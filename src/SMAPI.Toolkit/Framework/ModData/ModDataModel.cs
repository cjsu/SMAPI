using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StardewModdingAPI.Toolkit.Framework.ModData
{
    /// <summary>The raw mod metadata from SMAPI's internal mod list.</summary>
    internal class ModDataModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod's current unique ID.</summary>
        public string ID { get; set; }

        /// <summary>The former mod IDs (if any).</summary>
        /// <remarks>
        /// This uses a custom format which uniquely identifies a mod across multiple versions and
        /// supports matching other fields if no ID was specified. This doesn't include the latest
        /// ID, if any. If the mod's ID changed over time, multiple variants can be separated by the
        /// <c>|</c> character.
        /// </remarks>
        public string FormerIDs { get; set; }

        /// <summary>Maps local versions to a semantic version for update checks.</summary>
        public IDictionary<string, string> MapLocalVersions { get; set; } = new Dictionary<string, string>();

        /// <summary>Maps remote versions to a semantic version for update checks.</summary>
        public IDictionary<string, string> MapRemoteVersions { get; set; } = new Dictionary<string, string>();

        /// <summary>The mod warnings to suppress, even if they'd normally be shown.</summary>
        public ModWarning SuppressWarnings { get; set; }

        /// <summary>This field stores properties that aren't mapped to another field before they're parsed into <see cref="Fields"/>.</summary>
        [JsonExtensionData]
        public IDictionary<string, JToken> ExtensionData { get; set; }

        /// <summary>The versioned field data.</summary>
        /// <remarks>
        /// This maps field names to values. This should be accessed via <see cref="GetFields"/>.
        /// Format notes:
        ///   - Each key consists of a field name prefixed with any combination of version range
        ///     and <c>Default</c>, separated by pipes (whitespace trimmed). For example, <c>Name</c>
        ///     will always override the name, <c>Default | Name</c> will only override a blank
        ///     name, and <c>~1.1 | Default | Name</c> will override blank names up to version 1.1.
        ///   - The version format is <c>min~max</c> (where either side can be blank for unbounded), or
        ///     a single version number.
        ///   - The field name itself corresponds to a <see cref="ModDataFieldKey"/> value.
        /// </remarks>
        public IDictionary<string, string> Fields { get; set; } = new Dictionary<string, string>();


        /*********
        ** Public methods
        *********/
        /// <summary>Get a parsed representation of the <see cref="Fields"/>.</summary>
        public IEnumerable<ModDataField> GetFields()
        {
            foreach (KeyValuePair<string, string> pair in this.Fields)
            {
                // init fields
                string packedKey = pair.Key;
                string value = pair.Value;
                bool isDefault = false;
                ISemanticVersion lowerVersion = null;
                ISemanticVersion upperVersion = null;

                // parse
                string[] parts = packedKey.Split('|').Select(p => p.Trim()).ToArray();
                ModDataFieldKey fieldKey = (ModDataFieldKey)Enum.Parse(typeof(ModDataFieldKey), parts.Last(), ignoreCase: true);
                foreach (string part in parts.Take(parts.Length - 1))
                {
                    // 'default'
                    if (part.Equals("Default", StringComparison.InvariantCultureIgnoreCase))
                    {
                        isDefault = true;
                        continue;
                    }

                    // version range
                    if (part.Contains("~"))
                    {
                        string[] versionParts = part.Split(new[] { '~' }, 2);
                        lowerVersion = versionParts[0] != "" ? new SemanticVersion(versionParts[0]) : null;
                        upperVersion = versionParts[1] != "" ? new SemanticVersion(versionParts[1]) : null;
                        continue;
                    }

                    // single version
                    lowerVersion = new SemanticVersion(part);
                    upperVersion = new SemanticVersion(part);
                }

                yield return new ModDataField(fieldKey, value, isDefault, lowerVersion, upperVersion);
            }
        }

        /// <summary>Get the former mod IDs.</summary>
        public IEnumerable<string> GetFormerIDs()
        {
            if (this.FormerIDs != null)
            {
                foreach (string id in this.FormerIDs.Split('|'))
                    yield return id.Trim();
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method invoked after JSON deserialization.</summary>
        /// <param name="context">The deserialization context.</param>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (this.ExtensionData != null)
            {
                this.Fields = this.ExtensionData.ToDictionary(p => p.Key, p => p.Value.ToString());
                this.ExtensionData = null;
            }
        }
    }
}
