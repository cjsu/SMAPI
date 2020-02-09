using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class MenuWithInventoryMethods : MenuWithInventory
    {
        public ClickableTextureComponent TrashCanProp
        {
            get
            {
                return new ClickableTextureComponent(new Rectangle((base.xPositionOnScreen + base.width) + 4, ((((base.yPositionOnScreen + base.height) - 0xc0) - 0x20) - IClickableMenu.borderWidth) - 0x68, 0x40, 0x68), Game1.mouseCursors, new Rectangle(0x234 + (Game1.player.trashCanLevel * 0x12), 0x66, 0x12, 0x1a), 4f, false)
                {
                    myID = 0x173c,
                    downNeighborID = 0x12f9,
                    leftNeighborID = 12,
                    upNeighborID = 0x6a
                };
            }
            set
            {
            }
        }
    }
}
