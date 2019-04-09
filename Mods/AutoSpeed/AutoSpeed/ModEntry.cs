using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using SMDroid.Options;

namespace AutoSpeed
{
    /// <summary>The mod entry point.</summary>
    class ModEntry : StardewModdingAPI.Mod
    {
        /// <summary>The mod configuration.</summary>
        private ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        }

        public override List<OptionsElement> GetConfigMenuItems()
        {
            List<OptionsElement> options = new List<OptionsElement>();
            ModOptionsSlider _optionsSliderSpeed = new ModOptionsSlider("移动加速", 0x8765, delegate (int value) {
                Config.Speed = value;
                Helper.WriteConfig<ModConfig>(Config);
            }, -1, -1);
            _optionsSliderSpeed.sliderMinValue = 0;
            _optionsSliderSpeed.sliderMaxValue = 10;
            _optionsSliderSpeed.value = Config.Speed;
            options.Add(_optionsSliderSpeed);
            return options;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (Context.IsPlayerFree)
                Game1.player.addedSpeed = Config.Speed;
        }
    }
}