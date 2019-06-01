using System.Collections.Generic;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class ItemGrabMenuMethods : ItemGrabMenu
    {
        public ItemGrabMenuMethods(IList<Item> inventory, bool reverseGrab, bool showReceivingMenu, InventoryMenu.highlightThisItem highlightFunction, behaviorOnItemSelect behaviorOnItemSelectFunction, string message, behaviorOnItemSelect behaviorOnItemGrab = null, bool snapToBottom = false, bool canBeExitedWithKey = false, bool playRightClickSound = true, bool allowRightClick = true, bool showOrganizeButton = false, int source = 0, Item sourceItem = null, int whichSpecialButton = -1, object specialObject = null)
            : base(inventory, reverseGrab, showReceivingMenu, highlightFunction, behaviorOnItemSelectFunction, message, behaviorOnItemGrab, snapToBottom,canBeExitedWithKey, playRightClickSound, allowRightClick, showOrganizeButton, source, null, -1, null, -1, 3, null, true, null, false, null)
        { }

        public ItemGrabMenuMethods(IList<Item> inventory, object context = null)
            : base(inventory) { }
        
    }
}
