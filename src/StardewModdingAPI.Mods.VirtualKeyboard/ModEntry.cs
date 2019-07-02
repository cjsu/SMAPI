using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            new VirtualToggle(helper, this.Monitor);
        }
    }
}
