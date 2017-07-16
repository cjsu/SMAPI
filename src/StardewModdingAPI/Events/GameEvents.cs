using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Framework;

#pragma warning disable 618 // Suppress obsolete-symbol errors in this file. Since several events are marked obsolete, this produces unnecessary warnings.
namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the game changes state.</summary>
    public static class GameEvents
    {
        /*********
        ** Properties
        *********/
#if SMAPI_1_x
        /// <summary>Manages deprecation warnings.</summary>
        private static DeprecationManager DeprecationManager;

        /// <summary>The backing field for <see cref="Initialize"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler _Initialize;

        /// <summary>The backing field for <see cref="LoadContent"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler _LoadContent;

        /// <summary>The backing field for <see cref="GameLoaded"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler _GameLoaded;

        /// <summary>The backing field for <see cref="FirstUpdateTick"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler _FirstUpdateTick;
#endif


        /*********
        ** Events
        *********/
        /// <summary>Raised during launch after configuring XNA or MonoGame. The game window hasn't been opened by this point. Called after <see cref="Microsoft.Xna.Framework.Game.Initialize"/>.</summary>
        internal static event EventHandler InitializeInternal;

        /// <summary>Raised during launch after configuring Stardew Valley, loading it into memory, and opening the game window. The window is still blank by this point.</summary>
        internal static event EventHandler GameLoadedInternal;

#if SMAPI_1_x
        /// <summary>Raised during launch after configuring XNA or MonoGame. The game window hasn't been opened by this point. Called after <see cref="Microsoft.Xna.Framework.Game.Initialize"/>.</summary>
        [Obsolete("The " + nameof(Mod) + "." + nameof(Mod.Entry) + " method is now called after the " + nameof(GameEvents.Initialize) + " event, so any contained logic can be done directly in " + nameof(Mod.Entry) + ".")]
        public static event EventHandler Initialize
        {
            add
            {
                GameEvents.DeprecationManager.Warn($"{nameof(GameEvents)}.{nameof(GameEvents.Initialize)}", "1.10", DeprecationLevel.PendingRemoval);
                GameEvents._Initialize += value;
            }
            remove => GameEvents._Initialize -= value;
        }

        /// <summary>Raised before XNA loads or reloads graphics resources. Called during <see cref="Microsoft.Xna.Framework.Game.LoadContent"/>.</summary>
        [Obsolete("The " + nameof(Mod) + "." + nameof(Mod.Entry) + " method is now called after the " + nameof(GameEvents.LoadContent) + " event, so any contained logic can be done directly in " + nameof(Mod.Entry) + ".")]
        public static event EventHandler LoadContent
        {
            add
            {
                GameEvents.DeprecationManager.Warn($"{nameof(GameEvents)}.{nameof(GameEvents.LoadContent)}", "1.10", DeprecationLevel.PendingRemoval);
                GameEvents._LoadContent += value;
            }
            remove => GameEvents._LoadContent -= value;
        }

        /// <summary>Raised during launch after configuring Stardew Valley, loading it into memory, and opening the game window. The window is still blank by this point.</summary>
        [Obsolete("The " + nameof(Mod) + "." + nameof(Mod.Entry) + " method is now called after the game loads, so any contained logic can be done directly in " + nameof(Mod.Entry) + ".")]
        public static event EventHandler GameLoaded
        {
            add
            {
                GameEvents.DeprecationManager.Warn($"{nameof(GameEvents)}.{nameof(GameEvents.GameLoaded)}", "1.12", DeprecationLevel.PendingRemoval);
                GameEvents._GameLoaded += value;
            }
            remove => GameEvents._GameLoaded -= value;
        }

        /// <summary>Raised during the first game update tick.</summary>
        [Obsolete("The " + nameof(Mod) + "." + nameof(Mod.Entry) + " method is now called after the game loads, so any contained logic can be done directly in " + nameof(Mod.Entry) + ".")]
        public static event EventHandler FirstUpdateTick
        {
            add
            {
                GameEvents.DeprecationManager.Warn($"{nameof(GameEvents)}.{nameof(GameEvents.FirstUpdateTick)}", "1.12", DeprecationLevel.PendingRemoval);
                GameEvents._FirstUpdateTick += value;
            }
            remove => GameEvents._FirstUpdateTick -= value;
        }
#endif

        /// <summary>Raised when the game updates its state (≈60 times per second).</summary>
        public static event EventHandler UpdateTick;

        /// <summary>Raised every other tick (≈30 times per second).</summary>
        public static event EventHandler SecondUpdateTick;

        /// <summary>Raised every fourth tick (≈15 times per second).</summary>
        public static event EventHandler FourthUpdateTick;

        /// <summary>Raised every eighth tick (≈8 times per second).</summary>
        public static event EventHandler EighthUpdateTick;

        /// <summary>Raised every 15th tick (≈4 times per second).</summary>
        public static event EventHandler QuarterSecondTick;

        /// <summary>Raised every 30th tick (≈twice per second).</summary>
        public static event EventHandler HalfSecondTick;

        /// <summary>Raised every 60th tick (≈once per second).</summary>
        public static event EventHandler OneSecondTick;


        /*********
        ** Internal methods
        *********/
#if SMAPI_1_x
        /// <summary>Injects types required for backwards compatibility.</summary>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        internal static void Shim(DeprecationManager deprecationManager)
        {
            GameEvents.DeprecationManager = deprecationManager;
        }
#endif

        /// <summary>Raise an <see cref="InitializeInternal"/> event.</summary>
        /// <param name="monitor">Encapsulates logging and monitoring.</param>
        internal static void InvokeInitialize(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.InitializeInternal)}", GameEvents.InitializeInternal?.GetInvocationList());
#if SMAPI_1_x
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.Initialize)}", GameEvents._Initialize?.GetInvocationList());
#endif
        }

#if SMAPI_1_x
        /// <summary>Raise a <see cref="LoadContent"/> event.</summary>
        /// <param name="monitor">Encapsulates logging and monitoring.</param>
        internal static void InvokeLoadContent(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.LoadContent)}", GameEvents._LoadContent?.GetInvocationList());
        }
#endif

        /// <summary>Raise a <see cref="GameLoadedInternal"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeGameLoaded(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.GameLoadedInternal)}", GameEvents.GameLoadedInternal?.GetInvocationList());
#if SMAPI_1_x
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.GameLoaded)}", GameEvents._GameLoaded?.GetInvocationList());
#endif
        }

#if SMAPI_1_x
        /// <summary>Raise a <see cref="FirstUpdateTick"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeFirstUpdateTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.FirstUpdateTick)}", GameEvents._FirstUpdateTick?.GetInvocationList());
        }
#endif

        /// <summary>Raise an <see cref="UpdateTick"/> event.</summary>
        /// <param name="monitor">Encapsulates logging and monitoring.</param>
        internal static void InvokeUpdateTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.UpdateTick)}", GameEvents.UpdateTick?.GetInvocationList());
        }

        /// <summary>Raise a <see cref="SecondUpdateTick"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeSecondUpdateTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.SecondUpdateTick)}", GameEvents.SecondUpdateTick?.GetInvocationList());
        }

        /// <summary>Raise a <see cref="FourthUpdateTick"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeFourthUpdateTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.FourthUpdateTick)}", GameEvents.FourthUpdateTick?.GetInvocationList());
        }

        /// <summary>Raise a <see cref="EighthUpdateTick"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeEighthUpdateTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.EighthUpdateTick)}", GameEvents.EighthUpdateTick?.GetInvocationList());
        }

        /// <summary>Raise a <see cref="QuarterSecondTick"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeQuarterSecondTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.QuarterSecondTick)}", GameEvents.QuarterSecondTick?.GetInvocationList());
        }

        /// <summary>Raise a <see cref="HalfSecondTick"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeHalfSecondTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.HalfSecondTick)}", GameEvents.HalfSecondTick?.GetInvocationList());
        }

        /// <summary>Raise a <see cref="OneSecondTick"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeOneSecondTick(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(GameEvents)}.{nameof(GameEvents.OneSecondTick)}", GameEvents.OneSecondTick?.GetInvocationList());
        }
    }
}
