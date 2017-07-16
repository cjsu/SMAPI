using System;
using StardewModdingAPI.Framework;

namespace StardewModdingAPI.Events
{
    /// <summary>Events raised when the game loads content.</summary>
    public static class ContentEvents
    {

        /*********
        ** Events
        *********/
        /// <summary>Raised after the content language changes.</summary>
        public static event EventHandler<EventArgsValueChanged<string>> AfterLocaleChanged;


        /*********
        ** Internal methods
        *********/
        /// <summary>Raise an <see cref="AfterLocaleChanged"/> event.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="oldLocale">The previous locale.</param>
        /// <param name="newLocale">The current locale.</param>
        internal static void InvokeAfterLocaleChanged(IMonitor monitor, string oldLocale, string newLocale)
        {
            monitor.SafelyRaiseGenericEvent($"{nameof(ContentEvents)}.{nameof(ContentEvents.AfterLocaleChanged)}", ContentEvents.AfterLocaleChanged?.GetInvocationList(), null, new EventArgsValueChanged<string>(oldLocale, newLocale));
        }
    }
}
