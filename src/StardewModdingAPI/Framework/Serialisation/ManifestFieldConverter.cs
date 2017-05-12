using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Framework.Models;

namespace StardewModdingAPI.Framework.Serialisation
{
    /// <summary>Overrides how SMAPI reads and writes <see cref="ISemanticVersion"/> and <see cref="IManifestDependency"/> fields.</summary>
    internal class ManifestFieldConverter : JsonConverter
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether this converter can write JSON.</summary>
        public override bool CanWrite => false;


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">The object type.</param>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ISemanticVersion) || objectType == typeof(IManifestDependency[]);
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="existingValue">The object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // semantic version
            if (objectType == typeof(ISemanticVersion))
            {
                JObject obj = JObject.Load(reader);
                int major = obj.Value<int>(nameof(ISemanticVersion.MajorVersion));
                int minor = obj.Value<int>(nameof(ISemanticVersion.MinorVersion));
                int patch = obj.Value<int>(nameof(ISemanticVersion.PatchVersion));
                string build = obj.Value<string>(nameof(ISemanticVersion.Build));
                return new SemanticVersion(major, minor, patch, build);
            }

            // manifest dependency
            if (objectType == typeof(IManifestDependency[]))
            {
                List<IManifestDependency> result = new List<IManifestDependency>();
                foreach (JObject obj in JArray.Load(reader).Children<JObject>())
                {
                    string uniqueID = obj.Value<string>(nameof(IManifestDependency.UniqueID));
                    result.Add(new ManifestDependency(uniqueID));
                }
                return result.ToArray();
            }

            // unknown
            throw new NotSupportedException($"Unknown type '{objectType?.FullName}'.");
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new InvalidOperationException("This converter does not write JSON.");
        }
    }
}
