using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class InventoryMenuMethods : InventoryMenu
    {
        public InventoryMenuMethods(int xPosition, int yPosition, bool playerInventory, IList<Item> actualInventory = null, highlightThisItem highlightMethod = null,
            int capacity = -1, int rows = 3, int horizontalGap = 0, int verticalGap = 0, bool drawSlots = true)
            : base(xPosition, yPosition, playerInventory, actualInventory, highlightMethod, capacity, rows, horizontalGap, verticalGap, drawSlots)
        {
        }
        public Item rightClick(int x, int y, Item toAddTo, bool playSound = true, bool onlyCheckToolAttachments = false)
        {
            return base.rightClick(x, y, toAddTo, playSound);
        }

        public List<ClickableComponent> GetBorder(BorderSide side)
        {
            List<ClickableComponent> clickableComponentList = new List<ClickableComponent>();
            int num = this.capacity / this.rows;
            switch (side)
            {
                case BorderSide.Top:
                    for (int index = 0; index < this.inventory.Count; ++index)
                    {
                        if (index < num)
                            clickableComponentList.Add(this.inventory[index]);
                    }
                    break;
                case BorderSide.Left:
                    for (int index = 0; index < this.inventory.Count; ++index)
                    {
                        if (index % num == 0)
                            clickableComponentList.Add(this.inventory[index]);
                    }
                    break;
                case BorderSide.Right:
                    for (int index = 0; index < this.inventory.Count; ++index)
                    {
                        if (index % num == num - 1)
                            clickableComponentList.Add(this.inventory[index]);
                    }
                    break;
                case BorderSide.Bottom:
                    for (int index = 0; index < this.inventory.Count; ++index)
                    {
                        if (index >= this.actualInventory.Count - num)
                            clickableComponentList.Add(this.inventory[index]);
                    }
                    break;
            }
            return clickableComponentList;
        }

        public enum BorderSide
        {
            Top,
            Left,
            Right,
            Bottom,
        }
    }
}
