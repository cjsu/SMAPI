using Newtonsoft.Json;
using Pathoschild.Stardew.Common;
using StardewModdingAPI;

namespace Pathoschild.Stardew.Automate.Framework.Models
{
    /// <summary>The raw mod configuration.</summary>
    internal class ModConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether to treat the shipping bin as a machine that can be automated.</summary>
        public bool AutomateShippingBin { get; set; } = true;

        /// <summary>The number of ticks between each automation process (60 = once per second).</summary>
        public int AutomationInterval { get; set; } = 60;

        /// <summary>The control bindings.</summary>
        public ModConfigControls Controls { get; set; } = new ModConfigControls();

        /// <summary>The in-game objects through which machines can connect.</summary>
        public ModConfigObject[] Connectors { get; set; } = new ModConfigObject[0];


        /*********
        ** Nested models
        *********/
        /// <summary>A set of control bindings.</summary>
        internal class ModConfigControls
        {
            /// <summary>The button which toggles the automation overlay.</summary>
            [JsonConverter(typeof(StringEnumArrayConverter))]
            public SButton[] ToggleOverlay { get; set; } = { SButton.U };
        }
    }
}
