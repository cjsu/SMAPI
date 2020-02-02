using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Framework.PerformanceMonitoring;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>An event wrapper which intercepts and logs errors in handler code.</summary>
    /// <typeparam name="TEventArgs">The event arguments type.</typeparam>
    internal class ManagedEvent<TEventArgs> : IManagedEvent
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying event.</summary>
        private event EventHandler<TEventArgs> Event;

        /// <summary>Writes messages to the log.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The mod registry with which to identify mods.</summary>
        protected readonly ModRegistry ModRegistry;

        /// <summary>The display names for the mods which added each delegate.</summary>
        private readonly IDictionary<EventHandler<TEventArgs>, IModMetadata> SourceMods = new Dictionary<EventHandler<TEventArgs>, IModMetadata>();

        /// <summary>The cached invocation list.</summary>
        private EventHandler<TEventArgs>[] CachedInvocationList;

        /// <summary>Tracks performance metrics.</summary>
        private readonly PerformanceMonitor PerformanceMonitor;


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
        /// <param name="monitor">Writes messages to the log.</param>
        /// <param name="modRegistry">The mod registry with which to identify mods.</param>
        /// <param name="performanceMonitor">Tracks performance metrics.</param>
        /// <param name="isPerformanceCritical">Whether the event is typically called at least once per second.</param>
        public ManagedEvent(string eventName, IMonitor monitor, ModRegistry modRegistry, PerformanceMonitor performanceMonitor, bool isPerformanceCritical = false)
        {
            this.EventName = eventName;
            this.Monitor = monitor;
            this.ModRegistry = modRegistry;
            this.PerformanceMonitor = performanceMonitor;
            this.IsPerformanceCritical = isPerformanceCritical;
        }

        /// <summary>Get whether anything is listening to the event.</summary>
        public bool HasListeners()
        {
            return this.CachedInvocationList?.Length > 0;
        }

        /// <summary>Add an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        public void Add(EventHandler<TEventArgs> handler)
        {
            this.Add(handler, this.ModRegistry.GetFromStack());
        }

        /// <summary>Add an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        /// <param name="mod">The mod which added the event handler.</param>
        public void Add(EventHandler<TEventArgs> handler, IModMetadata mod)
        {
            this.Event += handler;
            this.AddTracking(mod, handler, this.Event?.GetInvocationList().Cast<EventHandler<TEventArgs>>());
        }

        /// <summary>Remove an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        public void Remove(EventHandler<TEventArgs> handler)
        {
            this.Event -= handler;
            this.RemoveTracking(handler, this.Event?.GetInvocationList().Cast<EventHandler<TEventArgs>>());
        }

        /// <summary>Raise the event and notify all handlers.</summary>
        /// <param name="args">The event arguments to pass.</param>
        public void Raise(TEventArgs args)
        {
            if (this.Event == null)
                return;


            this.PerformanceMonitor.Track(this.EventName, () =>
            {
                foreach (EventHandler<TEventArgs> handler in this.CachedInvocationList)
                {
                    try
                    {
                        this.PerformanceMonitor.Track(this.EventName, this.GetModNameForPerformanceCounters(handler), () => handler.Invoke(null, args));
                    }
                    catch (Exception ex)
                    {
                        this.LogError(handler, ex);
                    }
                }
            });
        }

        /// <summary>Raise the event and notify all handlers.</summary>
        /// <param name="args">The event arguments to pass.</param>
        /// <param name="match">A lambda which returns true if the event should be raised for the given mod.</param>
        public void RaiseForMods(TEventArgs args, Func<IModMetadata, bool> match)
        {
            if (this.Event == null)
                return;

            foreach (EventHandler<TEventArgs> handler in this.CachedInvocationList)
            {
                if (match(this.GetSourceMod(handler)))
                {
                    try
                    {
                        handler.Invoke(null, args);
                    }
                    catch (Exception ex)
                    {
                        this.LogError(handler, ex);
                    }
                }
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the mod name for a given event handler to display in performance monitoring reports.</summary>
        /// <param name="handler">The event handler.</param>
        private string GetModNameForPerformanceCounters(EventHandler<TEventArgs> handler)
        {
            IModMetadata mod = this.GetSourceMod(handler);
            if (mod == null)
                return Constants.GamePerformanceCounterName;

            return mod.HasManifest()
                ? mod.Manifest.UniqueID
                : mod.DisplayName;
        }

        /// <summary>Track an event handler.</summary>
        /// <param name="mod">The mod which added the handler.</param>
        /// <param name="handler">The event handler.</param>
        /// <param name="invocationList">The updated event invocation list.</param>
        protected void AddTracking(IModMetadata mod, EventHandler<TEventArgs> handler, IEnumerable<EventHandler<TEventArgs>> invocationList)
        {
            this.SourceMods[handler] = mod;
            this.CachedInvocationList = invocationList?.ToArray() ?? new EventHandler<TEventArgs>[0];
        }

        /// <summary>Remove tracking for an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        /// <param name="invocationList">The updated event invocation list.</param>
        protected void RemoveTracking(EventHandler<TEventArgs> handler, IEnumerable<EventHandler<TEventArgs>> invocationList)
        {
            this.CachedInvocationList = invocationList?.ToArray() ?? new EventHandler<TEventArgs>[0];
            if (!this.CachedInvocationList.Contains(handler)) // don't remove if there's still a reference to the removed handler (e.g. it was added twice and removed once)
                this.SourceMods.Remove(handler);
        }

        /// <summary>Get the mod which registered the given event handler, if available.</summary>
        /// <param name="handler">The event handler.</param>
        protected IModMetadata GetSourceMod(EventHandler<TEventArgs> handler)
        {
            return this.SourceMods.TryGetValue(handler, out IModMetadata mod)
                ? mod
                : null;
        }

        /// <summary>Log an exception from an event handler.</summary>
        /// <param name="handler">The event handler instance.</param>
        /// <param name="ex">The exception that was raised.</param>
        protected void LogError(EventHandler<TEventArgs> handler, Exception ex)
        {
            IModMetadata mod = this.GetSourceMod(handler);
            if (mod != null)
                mod.LogAsMod($"This mod failed in the {this.EventName} event. Technical details: \n{ex.GetLogSummary()}", LogLevel.Error);
            else
                this.Monitor.Log($"A mod failed in the {this.EventName} event. Technical details: \n{ex.GetLogSummary()}", LogLevel.Error);
        }
    }
}
