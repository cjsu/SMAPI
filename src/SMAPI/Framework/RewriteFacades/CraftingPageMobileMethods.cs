using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class CraftingPageMobileMethods : CraftingPageMobile
    {
        public CraftingPageMobileMethods(int x, int y, int width, int height, bool cooking = false, bool standalone_menu = false, List<Chest> material_containers = null)
            : base(x, y, width, height, cooking, 300, material_containers)
        {
        }
    }
}
