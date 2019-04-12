using Newtonsoft.Json;
using Pathoschild.Stardew.Common;
using StardewModdingAPI;

namespace Pathoschild.Stardew.LookupAnything.Framework
{
    /// <summary>The parsed mod configuration.</summary>
    internal class ModConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether to close the lookup UI when the lookup key is release.</summary>
        public bool HideOnKeyUp { get; set; }

        /// <summary>The amount to scroll long content on each up/down scroll.</summary>
        public int ScrollAmount { get; set; } = 160;

        /// <summary>Whether to show advanced data mining fields.</summary>
        public bool ShowDataMiningFields { get; set; }

        /// <summary>Whether to include map tiles as lookup targets.</summary>
        public bool EnableTileLookups { get; set; }

        /// <summary>The control bindings.</summary>
        public ModConfigControls Controls { get; set; } = new ModConfigControls();


        /*********
        ** Nested models
        *********/
        /// <summary>A set of control bindings.</summary>
        internal class ModConfigControls
        {
            /// <summary>The control which toggles the lookup UI for something under the cursor.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] ToggleLookup { get; set; } = { SButton.Help, SButton.VolumeDown };

            /// <summary>The control which toggles the lookup UI for something in front of the player.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] ToggleLookupInFrontOfPlayer { get; set; } = new SButton[0];

            /// <summary>The control which scrolls up long content.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] ScrollUp { get; set; } = { SButton.Up };

            /// <summary>The control which scrolls down long content.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] ScrollDown { get; set; } = { SButton.Down };

            /// <summary>Toggle the display of debug information.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] ToggleDebug { get; set; } = new SButton[0];
        }
    }
}
