using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StardewModdingAPI.Toolkit.Serialisation.Converters
{
    /// <summary>Handles deserialisation of <see cref="ISemanticVersion"/>.</summary>
    internal class SemanticVersionConverter : JsonConverter
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Get whether this converter can read JSON.</summary>
        public override bool CanRead => true;

        /// <summary>Get whether this converter can write JSON.</summary>
        public override bool CanWrite => true;


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether this instance can convert the specified object type.</summary>
        /// <param name="objectType">The object type.</param>
        public override bool CanConvert(Type objectType)
        {
            return typeof(ISemanticVersion).IsAssignableFrom(objectType);
        }

        /// <summary>Reads the JSON representation of the object.</summary>
        /// <param name="reader">The JSON reader.</param>
        /// <param name="objectType">The object type.</param>
        /// <param name="existingValue">The object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string path = reader.Path;
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return this.ReadObject(JObject.Load(reader));
                case JsonToken.String:
                    return this.ReadString(JToken.Load(reader).Value<string>(), path);
                default:
                    throw new SParseException($"Can't parse {nameof(ISemanticVersion)} from {reader.TokenType} node (path: {reader.Path}).");
            }
        }

        /// <summary>Writes the JSON representation of the object.</summary>
        /// <param name="writer">The JSON writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value?.ToString());
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Read a JSON object.</summary>
        /// <param name="obj">The JSON object to read.</param>
        private ISemanticVersion ReadObject(JObject obj)
        {
            int major = obj.ValueIgnoreCase<int>(nameof(ISemanticVersion.MajorVersion));
            int minor = obj.ValueIgnoreCase<int>(nameof(ISemanticVersion.MinorVersion));
            int patch = obj.ValueIgnoreCase<int>(nameof(ISemanticVersion.PatchVersion));
            string prereleaseTag = obj.ValueIgnoreCase<string>(nameof(ISemanticVersion.PrereleaseTag));
#if !SMAPI_3_0_STRICT
            if (string.IsNullOrWhiteSpace(prereleaseTag))
            {
                prereleaseTag = obj.ValueIgnoreCase<string>("Build");
                if (prereleaseTag == "0")
                    prereleaseTag = null; // '0' from incorrect examples in old SMAPI documentation
            }
#endif

            return new SemanticVersion(major, minor, patch, prereleaseTag
#if !SMAPI_3_0_STRICT
                , isLegacyFormat: true
#endif
            );
        }

        /// <summary>Read a JSON string.</summary>
        /// <param name="str">The JSON string value.</param>
        /// <param name="path">The path to the current JSON node.</param>
        private ISemanticVersion ReadString(string str, string path)
        {
            if (string.IsNullOrWhiteSpace(str))
                return null;
            if (!SemanticVersion.TryParse(str, out ISemanticVersion version))
                throw new SParseException($"Can't parse semantic version from invalid value '{str}', should be formatted like 1.2, 1.2.30, or 1.2.30-beta (path: {path}).");
            return version;
        }
    }
}
