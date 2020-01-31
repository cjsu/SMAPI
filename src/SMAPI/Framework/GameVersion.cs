using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework
{
    /// <summary>An implementation of <see cref="ISemanticVersion"/> that correctly handles the non-semantic versions used by older Stardew Valley releases.</summary>
    internal class GameVersion : SemanticVersion
    {
        /*********
        ** Private methods
        *********/
        /// <summary>A mapping of game to semantic versions.</summary>
        private static readonly IDictionary<string, string> VersionMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["1.0"] = "1.0.0",
            ["1.01"] = "1.0.1",
            ["1.02"] = "1.0.2",
            ["1.03"] = "1.0.3",
            ["1.04"] = "1.0.4",
            ["1.05"] = "1.0.5",
            ["1.051"] = "1.0.6-prerelease1", // not a very good mapping, but good enough for SMAPI's purposes.
            ["1.051b"] = "1.0.6-prerelease2",
            ["1.06"] = "1.0.6",
            ["1.07"] = "1.0.7",
            ["1.07a"] = "1.0.8-prerelease1",
            ["1.08"] = "1.0.8",
            ["1.1"] = "1.1.0",
            ["1.2"] = "1.2.0",
            ["1.11"] = "1.1.1"
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="version">The game version string.</param>
        public GameVersion(string version)
            : base(GameVersion.GetSemanticVersionString(version)) { }

        /// <summary>Get a string representation of the version.</summary>
        public override string ToString()
        {
            return GameVersion.GetGameVersionString(base.ToString());
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Convert a game version string to a semantic version string.</summary>
        /// <param name="gameVersion">The game version string.</param>
        private static string GetSemanticVersionString(string gameVersion)
        {
            // mapped version
            if (GameVersion.VersionMap.TryGetValue(gameVersion, out string semanticVersion))
                return semanticVersion;

            // special case: four-part versions
            string[] parts = gameVersion.Split('.');
            if (parts.Length == 4)
                return $"{parts[0]}.{parts[1]}.{parts[2]}+{parts[3]}";

            return gameVersion;
        }

        /// <summary>Convert a semantic version string to the equivalent game version string.</summary>
        /// <param name="semanticVersion">The semantic version string.</param>
        private static string GetGameVersionString(string semanticVersion)
        {
            // mapped versions
            foreach (var mapping in GameVersion.VersionMap)
            {
                if (mapping.Value.Equals(semanticVersion, StringComparison.InvariantCultureIgnoreCase))
                    return mapping.Key;
            }

            // special case: four-part versions
            string[] parts = semanticVersion.Split('.', '+');
            if (parts.Length == 4)
                return $"{parts[0]}.{parts[1]}.{parts[2]}.{parts[3]}";

            return semanticVersion;
        }
    }
}
