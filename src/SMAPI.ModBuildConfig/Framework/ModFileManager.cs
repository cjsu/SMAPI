using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewModdingAPI.Toolkit.Serialisation.Models;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.ModBuildConfig.Framework
{
    /// <summary>Manages the files that are part of a mod package.</summary>
    internal class ModFileManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The name of the manifest file.</summary>
        private readonly string ManifestFileName = "manifest.json";

        /// <summary>The files that are part of the package.</summary>
        private readonly IDictionary<string, FileInfo> Files;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="projectDir">The folder containing the project files.</param>
        /// <param name="targetDir">The folder containing the build output.</param>
        /// <param name="ignoreFilePatterns">Custom regex patterns matching files to ignore when deploying or zipping the mod.</param>
        /// <param name="validateRequiredModFiles">Whether to validate that required mod files like the manifest are present.</param>
        /// <exception cref="UserErrorException">The mod package isn't valid.</exception>
        public ModFileManager(string projectDir, string targetDir, Regex[] ignoreFilePatterns, bool validateRequiredModFiles)
        {
            this.Files = new Dictionary<string, FileInfo>(StringComparer.InvariantCultureIgnoreCase);

            // validate paths
            if (!Directory.Exists(projectDir))
                throw new UserErrorException("Could not create mod package because the project folder wasn't found.");
            if (!Directory.Exists(targetDir))
                throw new UserErrorException("Could not create mod package because no build output was found.");

            // project manifest
            bool hasProjectManifest = false;
            {
                FileInfo manifest = new FileInfo(Path.Combine(projectDir, "manifest.json"));
                if (manifest.Exists)
                {
                    this.Files[this.ManifestFileName] = manifest;
                    hasProjectManifest = true;
                }
            }

            // project i18n files
            bool hasProjectTranslations = false;
            DirectoryInfo translationsFolder = new DirectoryInfo(Path.Combine(projectDir, "i18n"));
            if (translationsFolder.Exists)
            {
                foreach (FileInfo file in translationsFolder.EnumerateFiles())
                    this.Files[Path.Combine("i18n", file.Name)] = file;
                hasProjectTranslations = true;
            }

            // project assets folder
            bool hasAssetsFolder = false;
            DirectoryInfo assetsFolder = new DirectoryInfo(Path.Combine(projectDir, "assets"));
            if (assetsFolder.Exists)
            {
                foreach (FileInfo file in assetsFolder.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    string relativePath = PathUtilities.GetRelativePath(projectDir, file.FullName);
                    this.Files[relativePath] = file;
                }
                hasAssetsFolder = true;
            }

            // build output
            DirectoryInfo buildFolder = new DirectoryInfo(targetDir);
            foreach (FileInfo file in buildFolder.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                // get path info
                string relativePath = PathUtilities.GetRelativePath(buildFolder.FullName, file.FullName);
                string[] segments = PathUtilities.GetSegments(relativePath);

                // prefer project manifest/i18n/assets files
                if (hasProjectManifest && this.EqualsInvariant(relativePath, this.ManifestFileName))
                    continue;
                if (hasProjectTranslations && this.EqualsInvariant(segments[0], "i18n"))
                    continue;
                if (hasAssetsFolder && this.EqualsInvariant(segments[0], "assets"))
                    continue;

                // handle ignored files
                if (this.ShouldIgnore(file, relativePath, ignoreFilePatterns))
                    continue;

                // add file
                this.Files[relativePath] = file;
            }

            // check for required files
            if (validateRequiredModFiles)
            {
                // manifest
                if (!this.Files.ContainsKey(this.ManifestFileName))
                    throw new UserErrorException($"Could not create mod package because no {this.ManifestFileName} was found in the project or build output.");

                // DLL
                // ReSharper disable once SimplifyLinqExpression
                if (!this.Files.Any(p => !p.Key.EndsWith(".dll")))
                    throw new UserErrorException("Could not create mod package because no .dll file was found in the project or build output.");
            }
        }

        /// <summary>Get the files in the mod package.</summary>
        public IDictionary<string, FileInfo> GetFiles()
        {
            return new Dictionary<string, FileInfo>(this.Files, StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>Get a semantic version from the mod manifest.</summary>
        /// <exception cref="UserErrorException">The manifest is missing or invalid.</exception>
        public string GetManifestVersion()
        {
            if (!this.Files.TryGetValue(this.ManifestFileName, out FileInfo manifestFile) || !new JsonHelper().ReadJsonFileIfExists(manifestFile.FullName, out Manifest manifest))
                throw new InvalidOperationException($"The mod does not have a {this.ManifestFileName} file."); // shouldn't happen since we validate in constructor

            return manifest.Version.ToString();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether a build output file should be ignored.</summary>
        /// <param name="file">The file to check.</param>
        /// <param name="relativePath">The file's relative path in the package.</param>
        /// <param name="ignoreFilePatterns">Custom regex patterns matching files to ignore when deploying or zipping the mod.</param>
        private bool ShouldIgnore(FileInfo file, string relativePath, Regex[] ignoreFilePatterns)
        {
            return
                // release zips
                this.EqualsInvariant(file.Extension, ".zip")

                // Json.NET (bundled into SMAPI)
                || this.EqualsInvariant(file.Name, "Newtonsoft.Json.dll")
                || this.EqualsInvariant(file.Name, "Newtonsoft.Json.pdb")
                || this.EqualsInvariant(file.Name, "Newtonsoft.Json.xml")

                // code analysis files
                || file.Name.EndsWith(".CodeAnalysisLog.xml", StringComparison.InvariantCultureIgnoreCase)
                || file.Name.EndsWith(".lastcodeanalysissucceeded", StringComparison.InvariantCultureIgnoreCase)

                // OS metadata files
                || this.EqualsInvariant(file.Name, ".DS_Store")
                || this.EqualsInvariant(file.Name, "Thumbs.db")

                // custom ignore patterns
                || ignoreFilePatterns.Any(p => p.IsMatch(relativePath));
        }

        /// <summary>Get whether a string is equal to another case-insensitively.</summary>
        /// <param name="str">The string value.</param>
        /// <param name="other">The string to compare with.</param>
        private bool EqualsInvariant(string str, string other)
        {
            if (str == null)
                return other == null;
            return str.Equals(other, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
