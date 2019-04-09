using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Pathoschild.Stardew.Automate.Framework.Models
{
    /// <summary>An object identifier.</summary>
    internal class ModConfigObject
    {
        /// <summary>The object type.</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ObjectType Type { get; set; }

        /// <summary>The object ID.</summary>
        public int ID { get; set; }
    }
}
