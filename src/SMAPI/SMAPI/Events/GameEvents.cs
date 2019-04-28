#if !SMAPI_3_0_STRICT
using System;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the game changes state.</summary>
    [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.Events) + " instead. See https://smapi.io/3.0 for more info.")]
    public static class GameEvents
    {
        /*********
        ** Fields
        *********/
        /// <summary>The core event manager.</summary>
        private static EventManager EventManager;


        /*********
        ** Events
        *********/
        /// <summary>Raised when the game updates its state (≈60 times per second).</summary>
        public static event EventHandler UpdateTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_UpdateTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_UpdateTick.Remove(value);
        }

        /// <summary>Raised every other tick (≈30 times per second).</summary>
        public static event EventHandler SecondUpdateTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_SecondUpdateTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_SecondUpdateTick.Remove(value);
        }

        /// <summary>Raised every fourth tick (≈15 times per second).</summary>
        public static event EventHandler FourthUpdateTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_FourthUpdateTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_FourthUpdateTick.Remove(value);
        }

        /// <summary>Raised every eighth tick (≈8 times per second).</summary>
        public static event EventHandler EighthUpdateTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_EighthUpdateTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_EighthUpdateTick.Remove(value);
        }

        /// <summary>Raised every 15th tick (≈4 times per second).</summary>
        public static event EventHandler QuarterSecondTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_QuarterSecondTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_QuarterSecondTick.Remove(value);
        }

        /// <summary>Raised every 30th tick (≈twice per second).</summary>
        public static event EventHandler HalfSecondTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_HalfSecondTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_HalfSecondTick.Remove(value);
        }

        /// <summary>Raised every 60th tick (≈once per second).</summary>
        public static event EventHandler OneSecondTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_OneSecondTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_OneSecondTick.Remove(value);
        }

        /// <summary>Raised once after the game initialises and all <see cref="IMod.Entry"/> methods have been called.</summary>
        public static event EventHandler FirstUpdateTick
        {
            add
            {
                SCore.DeprecationManager.WarnForOldEvents();
                GameEvents.EventManager.Legacy_FirstUpdateTick.Add(value);
            }
            remove => GameEvents.EventManager.Legacy_FirstUpdateTick.Remove(value);
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the events.</summary>
        /// <param name="eventManager">The core event manager.</param>
        internal static void Init(EventManager eventManager)
        {
            GameEvents.EventManager = eventManager;
        }
    }
}
#endif
