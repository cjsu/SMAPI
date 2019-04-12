using System;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Events related to UI and drawing to the screen.</summary>
    public interface IHookEvents
    {
        /// <summary>Object.canBePlacedHere hook.</summary>
        event Func<ObjectCanBePlacedHereEventArgs, bool> ObjectCanBePlacedHere;

        /// <summary>Object.checkForAction hook.</summary>
        event Func<ObjectCheckForActionEventArgs, bool> ObjectCheckForAction;

        /// <summary>Object.isIndexOkForBasicShippedCategory hook.</summary>
        event Func<ObjectIsIndexOkForBasicShippedCategoryEventArgs, bool> ObjectIsIndexOkForBasicShippedCategory;
    }
}
