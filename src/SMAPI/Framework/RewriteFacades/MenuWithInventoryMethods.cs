using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public MenuWithInventoryMethods(InventoryMenu.highlightThisItem highlighterMethod = null, bool okButton = false, bool trashCan = false, int inventoryXOffset = 0, int  inventoryYOffset = 0, int menuOffsetHack = 0) : base(highlighterMethod, okButton, trashCan, inventoryXOffset, inventoryYOffset)
        {
        }
        public virtual void draw(SpriteBatch b, bool drawUpperPortion = true, bool drawDescriptionArea = true, int red = -1, int green = -1, int blue = -1)
        {
            base.draw(b);
            base.draw(b, red, green, blue);
        }
    }
}
