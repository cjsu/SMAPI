using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class ItemGrabMenuMethods : ItemGrabMenu
    {
        public ClickableTextureComponent FillStacksButtonProp
        {
            get
            {
                ClickableTextureComponent textureComponent1 = new ClickableTextureComponent("", new Rectangle(this.xPositionOnScreen + this.width, this.yPositionOnScreen + this.height / 3 - 64 - 64 - 16, 64, 64), "", Game1.content.LoadString("Strings\\UI:ItemGrab_FillStacks"), Game1.mouseCursors, new Rectangle(103, 469, 16, 16), 4f, false);
                textureComponent1.myID = 12952;
                textureComponent1.upNeighborID = this.colorPickerToggleButton != null ? 27346 : (this.specialButton != null ? 12485 : -500);
                textureComponent1.downNeighborID = 106;
                textureComponent1.leftNeighborID = 53921;
                textureComponent1.region = 15923;
                return textureComponent1;
            }
            set
            {
            }
        }

        public ItemGrabMenuMethods(IList<Item> inventory, bool reverseGrab, bool showReceivingMenu, InventoryMenu.highlightThisItem highlightFunction, ItemGrabMenu.behaviorOnItemSelect behaviorOnItemSelectFunction, string message, ItemGrabMenu.behaviorOnItemSelect behaviorOnItemGrab = null, bool snapToBottom = false, bool canBeExitedWithKey = false, bool playRightClickSound = true, bool allowRightClick = true, bool showOrganizeButton = false, int source = 0, Item sourceItem = null, int whichSpecialButton = -1, object context = null)
            : base(inventory, reverseGrab, showReceivingMenu, highlightFunction, behaviorOnItemSelectFunction, message, behaviorOnItemGrab, snapToBottom, canBeExitedWithKey, playRightClickSound, allowRightClick, showOrganizeButton, source, sourceItem, whichSpecialButton, context, -1, 3, null, true, null, false, null)
        { }

        public ItemGrabMenuMethods(IList<Item> inventory, object context = null)
            : base(inventory) { }

        public void FillOutStacks()
        {
            for (int index1 = 0; index1 < this.ItemsToGrabMenu.actualInventory.Count; ++index1)
            {
                Item obj1 = this.ItemsToGrabMenu.actualInventory[index1];
                if (obj1 != null && obj1.maximumStackSize() > 1)
                {
                    for (int index2 = 0; index2 < this.inventory.actualInventory.Count; ++index2)
                    {
                        Item stack1 = this.inventory.actualInventory[index2];
                        if (stack1 != null && obj1.canStackWith((ISalable)stack1))
                        {
                            //this._transferredItemSprites.Add(new ItemGrabMenu.TransferredItemSprite(stack1.getOne(), this.inventory.inventory[index2].bounds.X, this.inventory.inventory[index2].bounds.Y));
                            int stack2 = stack1.Stack;
                            if (obj1.getRemainingStackSpace() > 0)
                            {
                                stack2 = obj1.addToStack(stack1);
                                //this.ItemsToGrabMenu.ShakeItem(obj1);
                            }
                            int stack3;
                            for (stack1.Stack = stack2; stack1.Stack > 0; stack1.Stack = stack3)
                            {
                                Item obj2 = (Item)null;
                                if (Utility.canItemBeAddedToThisInventoryList(obj1.getOne(), this.ItemsToGrabMenu.actualInventory, this.ItemsToGrabMenu.capacity))
                                {
                                    if (obj2 == null)
                                    {
                                        for (int index3 = 0; index3 < this.ItemsToGrabMenu.actualInventory.Count; ++index3)
                                        {
                                            if (this.ItemsToGrabMenu.actualInventory[index3] != null && this.ItemsToGrabMenu.actualInventory[index3].canStackWith((ISalable)obj1) && this.ItemsToGrabMenu.actualInventory[index3].getRemainingStackSpace() > 0)
                                            {
                                                obj2 = this.ItemsToGrabMenu.actualInventory[index3];
                                                break;
                                            }
                                        }
                                    }
                                    if (obj2 == null)
                                    {
                                        for (int index3 = 0; index3 < this.ItemsToGrabMenu.actualInventory.Count; ++index3)
                                        {
                                            if (this.ItemsToGrabMenu.actualInventory[index3] == null)
                                            {
                                                obj2 = this.ItemsToGrabMenu.actualInventory[index3] = obj1.getOne();
                                                obj2.Stack = 0;
                                                break;
                                            }
                                        }
                                    }
                                    if (obj2 == null && this.ItemsToGrabMenu.actualInventory.Count < this.ItemsToGrabMenu.capacity)
                                    {
                                        obj2 = obj1.getOne();
                                        this.ItemsToGrabMenu.actualInventory.Add(obj2);
                                    }
                                    if (obj2 != null)
                                    {
                                        stack3 = obj2.addToStack(stack1);
                                        //this.ItemsToGrabMenu.ShakeItem(obj2);
                                    }
                                    else
                                        break;
                                }
                                else
                                    break;
                            }
                            if (stack1.Stack == 0)
                                this.inventory.actualInventory[index2] = (Item)null;
                        }
                    }
                }
            }
        }
    }
}
