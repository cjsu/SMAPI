using System;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Framework;

#pragma warning disable 618 // Suppress obsolete-symbol errors in this file. Since several events are marked obsolete, this produces unnecessary warnings.
namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the in-game date or time changes.</summary>
    public static class TimeEvents
    {
        /*********
        ** Properties
        *********/
#if SMAPI_1_x
        /// <summary>Manages deprecation warnings.</summary>
        private static DeprecationManager DeprecationManager;

        /// <summary>The backing field for <see cref="OnNewDay"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler<EventArgsNewDay> _OnNewDay;

        /// <summary>The backing field for <see cref="DayOfMonthChanged"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler<EventArgsIntChanged> _DayOfMonthChanged;

        /// <summary>The backing field for <see cref="SeasonOfYearChanged"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler<EventArgsStringChanged> _SeasonOfYearChanged;

        /// <summary>The backing field for <see cref="YearOfGameChanged"/>.</summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static event EventHandler<EventArgsIntChanged> _YearOfGameChanged;
#endif


        /*********
        ** Events
        *********/
        /// <summary>Raised after the game begins a new day, including when loading a save.</summary>
        public static event EventHandler AfterDayStarted;

        /// <summary>Raised after the in-game clock changes.</summary>
        public static event EventHandler<EventArgsIntChanged> TimeOfDayChanged;

#if SMAPI_1_x
        /// <summary>Raised after the day-of-month value changes, including when loading a save. This may happen before save; in most cases you should use <see cref="AfterDayStarted"/> instead.</summary>
        [Obsolete("Use " + nameof(TimeEvents) + "." + nameof(TimeEvents.AfterDayStarted) + " or " + nameof(SaveEvents) + " instead")]
        public static event EventHandler<EventArgsIntChanged> DayOfMonthChanged
        {
            add
            {
                TimeEvents.DeprecationManager.Warn($"{nameof(TimeEvents)}.{nameof(TimeEvents.DayOfMonthChanged)}", "1.14", DeprecationLevel.PendingRemoval);
                TimeEvents._DayOfMonthChanged += value;
            }
            remove => TimeEvents._DayOfMonthChanged -= value;
        }

        /// <summary>Raised after the year value changes.</summary>
        [Obsolete("Use " + nameof(TimeEvents) + "." + nameof(TimeEvents.AfterDayStarted) + " or " + nameof(SaveEvents) + " instead")]
        public static event EventHandler<EventArgsIntChanged> YearOfGameChanged
        {
            add
            {
                TimeEvents.DeprecationManager.Warn($"{nameof(TimeEvents)}.{nameof(TimeEvents.YearOfGameChanged)}", "1.14", DeprecationLevel.PendingRemoval);
                TimeEvents._YearOfGameChanged += value;
            }
            remove => TimeEvents._YearOfGameChanged -= value;
        }

        /// <summary>Raised after the season value changes.</summary>
        [Obsolete("Use " + nameof(TimeEvents) + "." + nameof(TimeEvents.AfterDayStarted) + " or " + nameof(SaveEvents) + " instead")]
        public static event EventHandler<EventArgsStringChanged> SeasonOfYearChanged
        {
            add
            {
                TimeEvents.DeprecationManager.Warn($"{nameof(TimeEvents)}.{nameof(TimeEvents.SeasonOfYearChanged)}", "1.14", DeprecationLevel.PendingRemoval);
                TimeEvents._SeasonOfYearChanged += value;
            }
            remove => TimeEvents._SeasonOfYearChanged -= value;
        }

        /// <summary>Raised when the player is transitioning to a new day and the game is performing its day update logic. This event is triggered twice: once after the game starts transitioning, and again after it finishes.</summary>
        [Obsolete("Use " + nameof(TimeEvents) + "." + nameof(TimeEvents.AfterDayStarted) + " or " + nameof(SaveEvents) + " instead")]
        public static event EventHandler<EventArgsNewDay> OnNewDay
        {
            add
            {
                TimeEvents.DeprecationManager.Warn($"{nameof(TimeEvents)}.{nameof(TimeEvents.OnNewDay)}", "1.6", DeprecationLevel.PendingRemoval);
                TimeEvents._OnNewDay += value;
            }
            remove => TimeEvents._OnNewDay -= value;
        }
#endif


        /*********
        ** Internal methods
        *********/
#if SMAPI_1_x
        /// <summary>Injects types required for backwards compatibility.</summary>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        internal static void Shim(DeprecationManager deprecationManager)
        {
            TimeEvents.DeprecationManager = deprecationManager;
        }
#endif

        /// <summary>Raise an <see cref="AfterDayStarted"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        internal static void InvokeAfterDayStarted(IMonitor monitor)
        {
            monitor.SafelyRaisePlainEvent($"{nameof(TimeEvents)}.{nameof(TimeEvents.AfterDayStarted)}", TimeEvents.AfterDayStarted?.GetInvocationList(), null, EventArgs.Empty);
        }

        /// <summary>Raise a <see cref="TimeOfDayChanged"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="priorTime">The previous time in military time format (e.g. 6:00pm is 1800).</param>
        /// <param name="newTime">The current time in military time format (e.g. 6:10pm is 1810).</param>
        internal static void InvokeTimeOfDayChanged(IMonitor monitor, int priorTime, int newTime)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(TimeEvents)}.{nameof(TimeEvents.TimeOfDayChanged)}", TimeEvents.TimeOfDayChanged?.GetInvocationList(), null, new EventArgsIntChanged(priorTime, newTime));
        }

#if SMAPI_1_x
        /// <summary>Raise a <see cref="DayOfMonthChanged"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="priorDay">The previous day value.</param>
        /// <param name="newDay">The current day value.</param>
        internal static void InvokeDayOfMonthChanged(IMonitor monitor, int priorDay, int newDay)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(TimeEvents)}.{nameof(TimeEvents.DayOfMonthChanged)}", TimeEvents._DayOfMonthChanged?.GetInvocationList(), null, new EventArgsIntChanged(priorDay, newDay));
        }

        /// <summary>Raise a <see cref="YearOfGameChanged"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="priorYear">The previous year value.</param>
        /// <param name="newYear">The current year value.</param>
        internal static void InvokeYearOfGameChanged(IMonitor monitor, int priorYear, int newYear)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(TimeEvents)}.{nameof(TimeEvents.YearOfGameChanged)}", TimeEvents._YearOfGameChanged?.GetInvocationList(), null, new EventArgsIntChanged(priorYear, newYear));
        }

        /// <summary>Raise a <see cref="SeasonOfYearChanged"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="priorSeason">The previous season name.</param>
        /// <param name="newSeason">The current season name.</param>
        internal static void InvokeSeasonOfYearChanged(IMonitor monitor, string priorSeason, string newSeason)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(TimeEvents)}.{nameof(TimeEvents.SeasonOfYearChanged)}", TimeEvents._SeasonOfYearChanged?.GetInvocationList(), null, new EventArgsStringChanged(priorSeason, newSeason));
        }

        /// <summary>Raise a <see cref="OnNewDay"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="priorDay">The previous day value.</param>
        /// <param name="newDay">The current day value.</param>
        /// <param name="isTransitioning">Whether the game just started the transition (<c>true</c>) or finished it (<c>false</c>).</param>
        internal static void InvokeOnNewDay(IMonitor monitor, int priorDay, int newDay, bool isTransitioning)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(TimeEvents)}.{nameof(TimeEvents.OnNewDay)}", TimeEvents._OnNewDay?.GetInvocationList(), null, new EventArgsNewDay(priorDay, newDay, isTransitioning));
        }
#endif
    }
}
