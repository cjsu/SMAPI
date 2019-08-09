using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI.Toolkit.Serialization;
using StardewModdingAPI.Toolkit.Serialization.Models;

namespace StardewModdingAPI.Toolkit.Framework.ModScanning
{
    /// <summary>Scans folders for mod data.</summary>
    public class ModScanner
    {
        /*********
        ** Fields
        *********/
        /// <summary>The JSON helper with which to read manifests.</summary>
        private readonly JsonHelper JsonHelper;

        /// <summary>A list of filesystem entry names to ignore when checking whether a folder should be treated as a mod.</summary>
        private readonly HashSet<Regex> IgnoreFilesystemEntries = new HashSet<Regex>
        {
            // OS metadata files
            new Regex(@"^__folder_managed_by_vortex$", RegexOptions.Compiled | RegexOptions.IgnoreCase), // Vortex mod manager
            new Regex(@"^(?:__MACOSX|\._\.DS_Store|\.DS_Store|mcs)$", RegexOptions.Compiled | RegexOptions.IgnoreCase), // MacOS
            new Regex(@"^(?:desktop\.ini|Thumbs\.db)$", RegexOptions.Compiled | RegexOptions.IgnoreCase), // Windows
            new Regex(@"\.(?:url|lnk)$", RegexOptions.Compiled | RegexOptions.IgnoreCase), // Windows shortcut files

            // other
            new Regex(@"\.(?:bmp|gif|jpeg|jpg|png|psd|tif)$", RegexOptions.Compiled | RegexOptions.IgnoreCase), // image files
            new Regex(@"\.(?:md|rtf|txt)$", RegexOptions.Compiled | RegexOptions.IgnoreCase), // text files
            new Regex(@"\.(?:backup|bak|old)$", RegexOptions.Compiled | RegexOptions.IgnoreCase) // backup file
        };

        /// <summary>The extensions for files which an XNB mod may contain. If a mod doesn't have a <c>manifest.json</c> and contains *only* these file extensions, it should be considered an XNB mod.</summary>
        private readonly HashSet<string> PotentialXnbModExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            // XNB files
            ".xgs",
            ".xnb",
            ".xsb",
            ".xwb",

            // unpacking artifacts
            ".json",
            ".yaml"
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="jsonHelper">The JSON helper with which to read manifests.</param>
        public ModScanner(JsonHelper jsonHelper)
        {
            this.JsonHelper = jsonHelper;
        }

        /// <summary>Extract information about all mods in the given folder.</summary>
        /// <param name="rootPath">The root folder containing mods.</param>
        public IEnumerable<ModFolder> GetModFolders(string rootPath)
        {
            DirectoryInfo root = new DirectoryInfo(rootPath);
            return this.GetModFolders(root, root);
        }

        /// <summary>Extract information about all mods in the given folder.</summary>
        /// <param name="rootPath">The root folder containing mods. Only the <paramref name="modPath"/> will be searched, but this field allows it to be treated as a potential mod folder of its own.</param>
        /// <param name="modPath">The mod path to search.</param>
        // /// <param name="tryConsolidateMod">If the folder contains multiple XNB mods, treat them as subfolders of a single mod. This is useful when reading a single mod archive, as opposed to a mods folder.</param>
        public IEnumerable<ModFolder> GetModFolders(string rootPath, string modPath)
        {
            return this.GetModFolders(root: new DirectoryInfo(rootPath), folder: new DirectoryInfo(modPath));
        }

        /// <summary>Extract information from a mod folder.</summary>
        /// <param name="root">The root folder containing mods.</param>
        /// <param name="searchFolder">The folder to search for a mod.</param>
        public ModFolder ReadFolder(DirectoryInfo root, DirectoryInfo searchFolder)
        {
            // find manifest.json
            FileInfo manifestFile = this.FindManifest(searchFolder);

            // set appropriate invalid-mod error
            if (manifestFile == null)
            {
                FileInfo[] files = this.RecursivelyGetRelevantFiles(searchFolder).ToArray();
                if (!files.Any())
                    return new ModFolder(root, searchFolder, ModType.Invalid, null, ModParseError.EmptyFolder, "it's an empty folder.");
                if (files.All(this.IsPotentialXnbFile))
                    return new ModFolder(root, searchFolder, ModType.Xnb, null, ModParseError.XnbMod, "it's not a SMAPI mod (see https://smapi.io/xnb for info).");
                return new ModFolder(root, searchFolder, ModType.Invalid, null, ModParseError.ManifestMissing, "it contains files, but none of them are manifest.json.");
            }

            // read mod info
            Manifest manifest = null;
            ModParseError error = ModParseError.None;
            string errorText = null;
            {
                try
                {
                    if (!this.JsonHelper.ReadJsonFileIfExists<Manifest>(manifestFile.FullName, out manifest) || manifest == null)
                    {
                        error = ModParseError.ManifestInvalid;
                        errorText = "its manifest is invalid.";
                    }
                }
                catch (SParseException ex)
                {
                    error = ModParseError.ManifestInvalid;
                    errorText = $"parsing its manifest failed: {ex.Message}";
                }
                catch (Exception ex)
                {
                    error = ModParseError.ManifestInvalid;
                    errorText = $"parsing its manifest failed:\n{ex}";
                }
            }

            // normalize display fields
            if (manifest != null)
            {
                manifest.Name = this.StripNewlines(manifest.Name);
                manifest.Description = this.StripNewlines(manifest.Description);
                manifest.Author = this.StripNewlines(manifest.Author);
            }

            // get mod type
            ModType type = ModType.Invalid;
            if (manifest != null)
            {
                type = !string.IsNullOrWhiteSpace(manifest.ContentPackFor?.UniqueID)
                    ? ModType.ContentPack
                    : ModType.Smapi;
            }

            // build result
            return new ModFolder(root, manifestFile.Directory, type, manifest, error, errorText);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Recursively extract information about all mods in the given folder.</summary>
        /// <param name="root">The root mod folder.</param>
        /// <param name="folder">The folder to search for mods.</param>
        private IEnumerable<ModFolder> GetModFolders(DirectoryInfo root, DirectoryInfo folder)
        {
            bool isRoot = folder.FullName == root.FullName;

            // skip
            if (!isRoot)
            {
                if (folder.Name.StartsWith("."))
                {
                    yield return new ModFolder(root, folder, ModType.Ignored, null, ModParseError.IgnoredFolder, "ignored folder because its name starts with a dot.");
                    yield break;
                }
                if (!this.IsRelevant(folder))
                    yield break;
            }

            // find mods in subfolders
            if (this.IsModSearchFolder(root, folder))
            {
                IEnumerable<ModFolder> subfolders = folder.EnumerateDirectories().SelectMany(sub => this.GetModFolders(root, sub));
                if (!isRoot)
                    subfolders = this.TryConsolidate(root, folder, subfolders.ToArray());
                foreach (ModFolder subfolder in subfolders)
                    yield return subfolder;
            }

            // treat as mod folder
            else
                yield return this.ReadFolder(root, folder);
        }

        /// <summary>Consolidate adjacent folders into one mod folder, if possible.</summary>
        /// <param name="root">The folder containing both parent and subfolders.</param>
        /// <param name="parentFolder">The parent folder to consolidate, if possible.</param>
        /// <param name="subfolders">The subfolders to consolidate, if possible.</param>
        private IEnumerable<ModFolder> TryConsolidate(DirectoryInfo root, DirectoryInfo parentFolder, ModFolder[] subfolders)
        {
            if (subfolders.Length > 1)
            {
                // a collection of empty folders
                if (subfolders.All(p => p.ManifestParseError == ModParseError.EmptyFolder))
                    return new[] { new ModFolder(root, parentFolder, ModType.Invalid, null, ModParseError.EmptyFolder, subfolders[0].ManifestParseErrorText) };

                // an XNB mod
                if (subfolders.All(p => p.Type == ModType.Xnb || p.ManifestParseError == ModParseError.EmptyFolder))
                    return new[] { new ModFolder(root, parentFolder, ModType.Xnb, null, ModParseError.XnbMod, subfolders[0].ManifestParseErrorText) };
            }

            return subfolders;
        }

        /// <summary>Find the manifest for a mod folder.</summary>
        /// <param name="folder">The folder to search.</param>
        private FileInfo FindManifest(DirectoryInfo folder)
        {
            while (true)
            {
                // check for manifest in current folder
                FileInfo file = new FileInfo(Path.Combine(folder.FullName, "manifest.json"));
                if (file.Exists)
                    return file;

                // check for single subfolder
                FileSystemInfo[] entries = folder.EnumerateFileSystemInfos().Take(2).ToArray();
                if (entries.Length == 1 && entries[0] is DirectoryInfo subfolder)
                {
                    folder = subfolder;
                    continue;
                }

                // not found
                return null;
            }
        }

        /// <summary>Get whether a given folder should be treated as a search folder (i.e. look for subfolders containing mods).</summary>
        /// <param name="root">The root mod folder.</param>
        /// <param name="folder">The folder to search for mods.</param>
        private bool IsModSearchFolder(DirectoryInfo root, DirectoryInfo folder)
        {
            if (root.FullName == folder.FullName)
                return true;

            DirectoryInfo[] subfolders = folder.GetDirectories().Where(this.IsRelevant).ToArray();
            FileInfo[] files = folder.GetFiles().Where(this.IsRelevant).ToArray();
            return subfolders.Any() && !files.Any();
        }

        /// <summary>Recursively get all relevant files in a folder based on the result of <see cref="IsRelevant"/>.</summary>
        /// <param name="folder">The root folder to search.</param>
        private IEnumerable<FileInfo> RecursivelyGetRelevantFiles(DirectoryInfo folder)
        {
            foreach (FileSystemInfo entry in folder.GetFileSystemInfos())
            {
                if (!this.IsRelevant(entry))
                    continue;

                if (entry is FileInfo file)
                    yield return file;

                if (entry is DirectoryInfo subfolder)
                {
                    foreach (FileInfo subfolderFile in this.RecursivelyGetRelevantFiles(subfolder))
                        yield return subfolderFile;
                }
            }
        }

        /// <summary>Get whether a file or folder is relevant when deciding how to process a mod folder.</summary>
        /// <param name="entry">The file or folder.</param>
        private bool IsRelevant(FileSystemInfo entry)
        {
            return !this.IgnoreFilesystemEntries.Any(p => p.IsMatch(entry.Name));
        }

        /// <summary>Get whether a file is potentially part of an XNB mod.</summary>
        /// <param name="entry">The file.</param>
        private bool IsPotentialXnbFile(FileInfo entry)
        {
            if (!this.IsRelevant(entry))
                return true;

            return this.PotentialXnbModExtensions.Contains(entry.Extension); // use EndsWith to handle cases like image..png
        }

        /// <summary>Strip newlines from a string.</summary>
        /// <param name="input">The input to strip.</param>
        private string StripNewlines(string input)
        {
            return input?.Replace("\r", "").Replace("\n", "");
        }
    }
}
