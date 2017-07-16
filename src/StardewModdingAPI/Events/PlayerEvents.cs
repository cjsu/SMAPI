using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using StardewModdingAPI.Framework;
using StardewValley;
using SFarmer = StardewValley.Farmer;

#pragma warning disable 618 // Suppress obsolete-symbol errors in this file. Since several events are marked obsolete, this produces unnecessary warnings.
namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the player data changes.</summary>
    public static class PlayerEvents
    {
        /*********
        ** Properties
        *********/
#if SMAPI_1_x
        /// <summary>Manages deprecation warnings.</summary>
        private static DeprecationManager DeprecationManager;

        /// <summary>The backing field for <see cref="LoadedGame"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler<EventArgsLoadedGameChanged> _LoadedGame;

        /// <summary>The backing field for <see cref="FarmerChanged"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler<EventArgsFarmerChanged> _FarmerChanged;
#endif


        /*********
        ** Events
        *********/
#if SMAPI_1_x
        /// <summary>Raised after the player loads a saved game.</summary>
        [Obsolete("Use " + nameof(SaveEvents) + "." + nameof(SaveEvents.AfterLoad) + " instead")]
        public static event EventHandler<EventArgsLoadedGameChanged> LoadedGame
        {
            add
            {
                PlayerEvents.DeprecationManager.Warn($"{nameof(PlayerEvents)}.{nameof(PlayerEvents.LoadedGame)}", "1.6", DeprecationLevel.PendingRemoval);
                PlayerEvents._LoadedGame += value;
            }
            remove => PlayerEvents._LoadedGame -= value;
        }

        /// <summary>Raised after the game assigns a new player character. This happens just before <see cref="LoadedGame"/>; it's unclear how this would happen any other time.</summary>
        [Obsolete("should no longer be used")]
        public static event EventHandler<EventArgsFarmerChanged> FarmerChanged
        {
            add
            {
                PlayerEvents.DeprecationManager.Warn($"{nameof(PlayerEvents)}.{nameof(PlayerEvents.FarmerChanged)}", "1.6", DeprecationLevel.PendingRemoval);
                PlayerEvents._FarmerChanged += value;
            }
            remove => PlayerEvents._FarmerChanged -= value;
        }
#endif

        /// <summary>Raised after the player's inventory changes in any way (added or removed item, sorted, etc).</summary>
        public static event EventHandler<EventArgsInventoryChanged> InventoryChanged;

        /// <summary> Raised after the player levels up a skill. This happens as soon as they level up, not when the game notifies the player after their character goes to bed.</summary>
        public static event EventHandler<EventArgsLevelUp> LeveledUp;


        /*********
        ** Internal methods
        *********/
#if SMAPI_1_x
        /// <summary>Injects types required for backwards compatibility.</summary>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        internal static void Shim(DeprecationManager deprecationManager)
        {
            PlayerEvents.DeprecationManager = deprecationManager;
        }

        /// <summary>Raise a <see cref="LoadedGame"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="loaded">Whether the save has been loaded. This is always true.</param>
        internal static void InvokeLoadedGame(IMonitor monitor, EventArgsLoadedGameChanged loaded)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(PlayerEvents)}.{nameof(PlayerEvents.LoadedGame)}", PlayerEvents._LoadedGame?.GetInvocationList(), null, loaded);
        }

        /// <summary>Raise a <see cref="FarmerChanged"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="priorFarmer">The previous player character.</param>
        /// <param name="newFarmer">The new player character.</param>
        internal static void InvokeFarmerChanged(IMonitor monitor, SFarmer priorFarmer, SFarmer newFarmer)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(PlayerEvents)}.{nameof(PlayerEvents.FarmerChanged)}", PlayerEvents._FarmerChanged?.GetInvocationList(), null, new EventArgsFarmerChanged(priorFarmer, newFarmer));
        }
#endif

        /// <summary>Raise an <see cref="InventoryChanged"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="inventory">The player's inventory.</param>
        /// <param name="changedItems">The inventory changes.</param>
        internal static void InvokeInventoryChanged(IMonitor monitor, List<Item> inventory, IEnumerable<ItemStackChange> changedItems)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(PlayerEvents)}.{nameof(PlayerEvents.InventoryChanged)}", PlayerEvents.InventoryChanged?.GetInvocationList(), null, new EventArgsInventoryChanged(inventory, changedItems.ToList()));
        }

        /// <summary>Rase a <see cref="LeveledUp"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="type">The player skill that leveled up.</param>
        /// <param name="newLevel">The new skill level.</param>
        internal static void InvokeLeveledUp(IMonitor monitor, EventArgsLevelUp.LevelType type, int newLevel)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(PlayerEvents)}.{nameof(PlayerEvents.LeveledUp)}", PlayerEvents.LeveledUp?.GetInvocationList(), null, new EventArgsLevelUp(type, newLevel));
        }
    }
}
