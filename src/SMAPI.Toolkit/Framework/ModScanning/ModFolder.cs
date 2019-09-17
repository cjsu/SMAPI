using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI.Toolkit.Serialisation.Models;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Toolkit.Framework.ModScanning
{
    /// <summary>The info about a mod read from its folder.</summary>
    public class ModFolder
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A suggested display name for the mod folder.</summary>
        public string DisplayName { get; }

        /// <summary>The folder containing the mod's manifest.json.</summary>
        public DirectoryInfo Directory { get; }

        /// <summary>The mod manifest.</summary>
        public Manifest Manifest { get; }

        /// <summary>The error which occurred parsing the manifest, if any.</summary>
        public string ManifestParseError { get; }

        /// <summary>Whether the mod should be loaded by default. This is <c>false</c> if it was found within a folder whose name starts with a dot.</summary>
        public bool ShouldBeLoaded { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="root">The root folder containing mods.</param>
        /// <param name="directory">The folder containing the mod's manifest.json.</param>
        /// <param name="manifest">The mod manifest.</param>
        /// <param name="manifestParseError">The error which occurred parsing the manifest, if any.</param>
        /// <param name="shouldBeLoaded">Whether the mod should be loaded by default. This should be <c>false</c> if it was found within a folder whose name starts with a dot.</param>
        public ModFolder(DirectoryInfo root, DirectoryInfo directory, Manifest manifest, string manifestParseError = null, bool shouldBeLoaded = true)
        {
            // save info
            this.Directory = directory;
            this.Manifest = manifest;
            this.ManifestParseError = manifestParseError;
            this.ShouldBeLoaded = shouldBeLoaded;

            // set display name
            this.DisplayName = manifest?.Name;
            if (string.IsNullOrWhiteSpace(this.DisplayName))
                this.DisplayName = PathUtilities.GetRelativePath(root.FullName, directory.FullName);
        }

        /// <summary>Get the update keys for a mod.</summary>
        /// <param name="manifest">The mod manifest.</param>
        public IEnumerable<string> GetUpdateKeys(Manifest manifest)
        {
            return
                (manifest.UpdateKeys ?? new string[0])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();
        }
    }
}
