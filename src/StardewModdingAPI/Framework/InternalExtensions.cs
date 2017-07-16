using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>Provides extension methods for SMAPI's internal use.</summary>
    internal static class InternalExtensions
    {
        /****
        ** IMonitor
        ****/
        /// <summary>Safely raise an <see cref="EventHandler"/> event, and intercept any exceptions thrown by its handlers.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="name">The event name for error messages.</param>
        /// <param name="handlers">The event handlers.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event arguments (or <c>null</c> to pass <see cref="EventArgs.Empty"/>).</param>
        public static void SafelyRaisePlainEvent(this IMonitor monitor, string name, IEnumerable<Delegate> handlers, object sender = null, EventArgs args = null)
        {
            if (handlers == null)
                return;

            foreach (EventHandler handler in handlers.Cast<EventHandler>())
            {
                // handle SMAPI exiting
                if (monitor.IsExiting)
                {
                    monitor.Log($"SMAPI shutting down: aborting {name} event.", LogLevel.Warn);
                    return;
                }

                // raise event
                try
                {
                    handler.Invoke(sender, args ?? EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    monitor.Log($"A mod failed handling the {name} event:\n{ex.GetLogSummary()}", LogLevel.Error);
                }
            }
        }

        /// <summary>Safely raise an <see cref="EventHandler{TEventArgs}"/> event, and intercept any exceptions thrown by its handlers.</summary>
        /// <typeparam name="TEventArgs">The event argument object type.</typeparam>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="name">The event name for error messages.</param>
        /// <param name="handlers">The event handlers.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event arguments.</param>
        public static void SafelyRaiseGenericEvent<TEventArgs>(this IMonitor monitor, string name, IEnumerable<Delegate> handlers, object sender, TEventArgs args)
        {
            if (handlers == null)
                return;

            foreach (EventHandler<TEventArgs> handler in handlers.Cast<EventHandler<TEventArgs>>())
            {
                try
                {
                    handler.Invoke(sender, args);
                }
                catch (Exception ex)
                {
                    monitor.Log($"A mod failed handling the {name} event:\n{ex.GetLogSummary()}", LogLevel.Error);
                }
            }
        }

        /****
        ** Exceptions
        ****/
        /// <summary>Get a string representation of an exception suitable for writing to the error log.</summary>
        /// <param name="exception">The error to summarise.</param>
        public static string GetLogSummary(this Exception exception)
        {
            switch (exception)
            {
                case TypeLoadException ex:
                    return $"Failed loading type '{ex.TypeName}': {exception}";

                case ReflectionTypeLoadException ex:
                    string summary = exception.ToString();
                    foreach (Exception childEx in ex.LoaderExceptions)
                        summary += $"\n\n{childEx.GetLogSummary()}";
                    return summary;

                default:
                    return exception.ToString();
            }
        }

        /****
        ** Sprite batch
        ****/
        /// <summary>Get whether the sprite batch is between a begin and end pair.</summary>
        /// <param name="spriteBatch">The sprite batch to check.</param>
        /// <param name="reflection">The reflection helper with which to access private fields.</param>
        public static bool IsOpen(this SpriteBatch spriteBatch, Reflector reflection)
        {
            // get field name
            const string fieldName =
#if SMAPI_FOR_WINDOWS
            "inBeginEndPair";
#else
            "_beginCalled";
#endif

            // get result
            return reflection.GetPrivateField<bool>(Game1.spriteBatch, fieldName).GetValue();
        }
    }
}
