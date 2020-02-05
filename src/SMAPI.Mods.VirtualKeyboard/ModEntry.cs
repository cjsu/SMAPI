using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewModdingAPI;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            new VirtualToggle(helper, this.Monitor);
        }
    }
}
