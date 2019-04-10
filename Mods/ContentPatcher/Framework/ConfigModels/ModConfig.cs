using Newtonsoft.Json;
using Pathoschild.Stardew.Common;
using StardewModdingAPI;

namespace ContentPatcher.Framework.ConfigModels
{
    /// <summary>The mod configuration.</summary>
    internal class ModConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether to enable debug features.</summary>
        public bool EnableDebugFeatures { get; set; }

        /// <summary>The control bindings.</summary>
        public ModConfigControls Controls { get; set; } = new ModConfigControls();


        /*********
        ** Nested models
        *********/
        /// <summary>A set of control bindings.</summary>
        internal class ModConfigControls
        {
            /// <summary>Toggle the display of debug information.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] ToggleDebug { get; set; } = { SButton.F3 };

            /// <summary>Switch to the previous texture.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] DebugPrevTexture { get; set; } = { SButton.LeftControl };

            /// <summary>Switch to the next texture.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] DebugNextTexture { get; set; } = { SButton.RightControl };
        }
    }
}
