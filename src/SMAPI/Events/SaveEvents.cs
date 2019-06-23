#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised before and after the player saves/loads the game.</summary>
    [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.Events) + " instead. See https://smapi.io/3.0 for more info.")]
    public static class SaveEvents
    {
        /*********
        ** Fields
        *********/
        /// <summary>The core event manager.</summary>
        private static EventManager EventManager;


        /*********
        ** Events
        *********/
        /// <summary>Raised before the game creates the save file.</summary>
        public static event EventHandler BeforeCreate
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                SaveEvents.EventManager.Legacy_BeforeCreateSave.Add(value);
            }
            remove => SaveEvents.EventManager.Legacy_BeforeCreateSave.Remove(value);
        }

        /// <summary>Raised after the game finishes creating the save file.</summary>
        public static event EventHandler AfterCreate
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                SaveEvents.EventManager.Legacy_AfterCreateSave.Add(value);
            }
            remove => SaveEvents.EventManager.Legacy_AfterCreateSave.Remove(value);
        }

        /// <summary>Raised before the game begins writes data to the save file.</summary>
        public static event EventHandler BeforeSave
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                SaveEvents.EventManager.Legacy_BeforeSave.Add(value);
            }
            remove => SaveEvents.EventManager.Legacy_BeforeSave.Remove(value);
        }

        /// <summary>Raised after the game finishes writing data to the save file.</summary>
        public static event EventHandler AfterSave
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                SaveEvents.EventManager.Legacy_AfterSave.Add(value);
            }
            remove => SaveEvents.EventManager.Legacy_AfterSave.Remove(value);
        }

        /// <summary>Raised after the player loads a save slot.</summary>
        public static event EventHandler AfterLoad
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                SaveEvents.EventManager.Legacy_AfterLoad.Add(value);
            }
            remove => SaveEvents.EventManager.Legacy_AfterLoad.Remove(value);
        }

        /// <summary>Raised after the game returns to the title screen.</summary>
        public static event EventHandler AfterReturnToTitle
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                SaveEvents.EventManager.Legacy_AfterReturnToTitle.Add(value);
            }
            remove => SaveEvents.EventManager.Legacy_AfterReturnToTitle.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the events.</summary>
        /// <param name="eventManager">The core event manager.</param>
        internal static void Init(EventManager eventManager)
        {
            SaveEvents.EventManager = eventManager;
        }
    }
}
#endif
