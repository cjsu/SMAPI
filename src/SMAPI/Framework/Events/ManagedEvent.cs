using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>An event wrapper which intercepts and logs errors in handler code.</summary>
    /// <typeparam name="TEventArgs">The event arguments type.</typeparam>
    internal class ManagedEvent<TEventArgs> : IManagedEvent
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod registry with which to identify mods.</summary>
        protected readonly ModRegistry ModRegistry;

        /// <summary>The underlying event handlers.</summary>
        private readonly List<ManagedEventHandler<TEventArgs>> Handlers = new List<ManagedEventHandler<TEventArgs>>();

        /// <summary>A cached snapshot of <see cref="Handlers"/>, or <c>null</c> to rebuild it next raise.</summary>
        private ManagedEventHandler<TEventArgs>[] CachedHandlers = new ManagedEventHandler<TEventArgs>[0];

        /// <summary>The total number of event handlers registered for this events, regardless of whether they're still registered.</summary>
        private int RegistrationIndex;

        /// <summary>Whether new handlers were added since the last raise.</summary>
        private bool HasNewHandlers;


        /*********
        ** Accessors
        *********/
        /// <summary>A human-readable name for the event.</summary>
        public string EventName { get; }

        /// <summary>Whether the event is typically called at least once per second.</summary>
        public bool IsPerformanceCritical { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="eventName">A human-readable name for the event.</param>
        /// <param name="modRegistry">The mod registry with which to identify mods.</param>
        /// <param name="isPerformanceCritical">Whether the event is typically called at least once per second.</param>
        public ManagedEvent(string eventName, ModRegistry modRegistry, bool isPerformanceCritical = false)
        {
            this.EventName = eventName;
            this.ModRegistry = modRegistry;
            this.IsPerformanceCritical = isPerformanceCritical;
        }

        /// <summary>Get whether anything is listening to the event.</summary>
        public bool HasListeners()
        {
            return this.Handlers.Count > 0;
        }

        /// <summary>Add an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        /// <param name="mod">The mod which added the event handler.</param>
        public void Add(EventHandler<TEventArgs> handler, IModMetadata mod)
        {
            lock (this.Handlers)
            {
                EventPriority priority = handler.Method.GetCustomAttribute<EventPriorityAttribute>()?.Priority ?? EventPriority.Normal;
                var managedHandler = new ManagedEventHandler<TEventArgs>(handler, this.RegistrationIndex++, priority, mod);

                this.Handlers.Add(managedHandler);
                this.CachedHandlers = null;
                this.HasNewHandlers = true;
            }
        }

        /// <summary>Remove an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        public void Remove(EventHandler<TEventArgs> handler)
        {
            lock (this.Handlers)
            {
                // match C# events: if a handler is listed multiple times, remove the last one added
                for (int i = this.Handlers.Count - 1; i >= 0; i--)
                {
                    if (this.Handlers[i].Handler != handler)
                        continue;

                    this.Handlers.RemoveAt(i);
                    this.CachedHandlers = null;
                    break;
                }
            }
        }

        /// <summary>Raise the event and notify all handlers.</summary>
        /// <param name="args">The event arguments to pass.</param>
        /// <param name="match">A lambda which returns true if the event should be raised for the given mod.</param>
        public void Raise(TEventArgs args, Func<IModMetadata, bool> match = null)
        {
            // skip if no handlers
            if (this.Handlers.Count == 0)
                return;

            // update cached data
            // (This is debounced here to avoid repeatedly sorting when handlers are added/removed,
            // and keeping a separate cached list allows changes during enumeration.)
            var handlers = this.CachedHandlers; // iterate local copy in case a mod adds/removes a handler while handling the event, which will set this field to null
            if (handlers == null)
            {
                lock (this.Handlers)
                {
                    if (this.HasNewHandlers && this.Handlers.Any(p => p.Priority != EventPriority.Normal))
                        this.Handlers.Sort();

                    this.CachedHandlers = handlers = this.Handlers.ToArray();
                    this.HasNewHandlers = false;
                }
            }

            // raise event
            foreach (ManagedEventHandler<TEventArgs> handler in handlers)
            {
                if (match != null && !match(handler.SourceMod))
                    continue;

                try
                {
                    handler.Handler.Invoke(null, args);
                }
                catch (Exception ex)
                {
                    this.LogError(handler, ex);
                }
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Log an exception from an event handler.</summary>
        /// <param name="handler">The event handler instance.</param>
        /// <param name="ex">The exception that was raised.</param>
        protected void LogError(ManagedEventHandler<TEventArgs> handler, Exception ex)
        {
            handler.SourceMod.LogAsMod($"This mod failed in the {this.EventName} event. Technical details: \n{ex.GetLogSummary()}", LogLevel.Error);
        }
    }
}
