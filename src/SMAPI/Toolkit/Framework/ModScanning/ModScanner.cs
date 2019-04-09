using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewModdingAPI.Toolkit.Serialisation.Models;

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
        private readonly HashSet<string> IgnoreFilesystemEntries = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ".DS_Store",
            "mcs",
            "Thumbs.db"
        };

        /// <summary>The extensions for files which an XNB mod may contain. If a mod contains *only* these file extensions, it should be considered an XNB mod.</summary>
        private readonly HashSet<string> PotentialXnbModExtensions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ".md",
            ".png",
            ".txt",
            ".xnb"
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
                FileInfo[] files = searchFolder.GetFiles("*", SearchOption.AllDirectories).Where(this.IsRelevant).ToArray();
                if (!files.Any())
                    return new ModFolder(root, searchFolder, null, "it's an empty folder.");
                if (files.All(file => this.PotentialXnbModExtensions.Contains(file.Extension)))
                    return new ModFolder(root, searchFolder, null, "it's not a SMAPI mod (see https://smapi.io/xnb for info).");
                return new ModFolder(root, searchFolder, null, "it contains files, but none of them are manifest.json.");
            }

            // read mod info
            Manifest manifest = null;
            string manifestError = null;
            {
                try
                {
                    if (!this.JsonHelper.ReadJsonFileIfExists<Manifest>(manifestFile.FullName, out manifest) || manifest == null)
                        manifestError = "its manifest is invalid.";
                }
                catch (SParseException ex)
                {
                    manifestError = $"parsing its manifest failed: {ex.Message}";
                }
                catch (Exception ex)
                {
                    manifestError = $"parsing its manifest failed:\n{ex}";
                }
            }

            // normalise display fields
            if (manifest != null)
            {
                manifest.Name = this.StripNewlines(manifest.Name);
                manifest.Description = this.StripNewlines(manifest.Description);
                manifest.Author = this.StripNewlines(manifest.Author);
            }

            return new ModFolder(root, manifestFile.Directory, manifest, manifestError);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Recursively extract information about all mods in the given folder.</summary>
        /// <param name="root">The root mod folder.</param>
        /// <param name="folder">The folder to search for mods.</param>
        public IEnumerable<ModFolder> GetModFolders(DirectoryInfo root, DirectoryInfo folder)
        {
            // skip
            if (folder.FullName != root.FullName && folder.Name.StartsWith("."))
                yield return new ModFolder(root, folder, null, "ignored folder because its name starts with a dot.", shouldBeLoaded: false);

            // recurse into subfolders
            else if (this.IsModSearchFolder(root, folder))
            {
                foreach (DirectoryInfo subfolder in folder.EnumerateDirectories())
                {
                    foreach (ModFolder match in this.GetModFolders(root, subfolder))
                        yield return match;
                }
            }

            // treat as mod folder
            else
                yield return this.ReadFolder(root, folder);
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

        /// <summary>Get whether a file or folder is relevant when deciding how to process a mod folder.</summary>
        /// <param name="entry">The file or folder.</param>
        private bool IsRelevant(FileSystemInfo entry)
        {
            return !this.IgnoreFilesystemEntries.Contains(entry.Name);
        }

        /// <summary>Strip newlines from a string.</summary>
        /// <param name="input">The input to strip.</param>
        private string StripNewlines(string input)
        {
            return input?.Replace("\r", "").Replace("\n", "");
        }
    }
}
