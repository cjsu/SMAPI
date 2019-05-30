using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    public class ModEntry : Mod
    {
        //private List<KeyButton> keyboard = new List<KeyButton>();
        //private ModConfig modConfig;
        public override void Entry(IModHelper helper)
        {
            VirtualToggle virtualToggle = new VirtualToggle(helper, this.Monitor);
            //this.modConfig = helper.ReadConfig<ModConfig>();
            //for (int i = 0; i < this.modConfig.buttons.Length; i++)
            //{
            //    this.keyboard.Add(new KeyButton(helper, this.modConfig.buttons[i], this.Monitor));
            //}
            //helper.WriteConfig(this.modConfig);
        }
    }
}
