using System;
using System.Linq;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>An event wrapper which intercepts and logs errors in handler code.</summary>
    /// <typeparam name="TEventArgs">The event arguments type.</typeparam>
    internal class ManagedEvent<TEventArgs> : ManagedEventBase<EventHandler<TEventArgs>>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying event.</summary>
        private event EventHandler<TEventArgs> Event;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="eventName">A human-readable name for the event.</param>
        /// <param name="monitor">Writes messages to the log.</param>
        /// <param name="modRegistry">The mod registry with which to identify mods.</param>
        public ManagedEvent(string eventName, IMonitor monitor, ModRegistry modRegistry)
            : base(eventName, monitor, modRegistry) { }

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

            foreach (EventHandler<TEventArgs> handler in this.CachedInvocationList)
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
    }

#if !SMAPI_3_0_STRICT
    /// <summary>An event wrapper which intercepts and logs errors in handler code.</summary>
    internal class ManagedEvent : ManagedEventBase<EventHandler>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying event.</summary>
        private event EventHandler Event;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="eventName">A human-readable name for the event.</param>
        /// <param name="monitor">Writes messages to the log.</param>
        /// <param name="modRegistry">The mod registry with which to identify mods.</param>
        public ManagedEvent(string eventName, IMonitor monitor, ModRegistry modRegistry)
            : base(eventName, monitor, modRegistry) { }

        /// <summary>Add an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        public void Add(EventHandler handler)
        {
            this.Add(handler, this.ModRegistry.GetFromStack());
        }

        /// <summary>Add an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        /// <param name="mod">The mod which added the event handler.</param>
        public void Add(EventHandler handler, IModMetadata mod)
        {
            this.Event += handler;
            this.AddTracking(mod, handler, this.Event?.GetInvocationList().Cast<EventHandler>());
        }

        /// <summary>Remove an event handler.</summary>
        /// <param name="handler">The event handler.</param>
        public void Remove(EventHandler handler)
        {
            this.Event -= handler;
            this.RemoveTracking(handler, this.Event?.GetInvocationList().Cast<EventHandler>());
        }

        /// <summary>Raise the event and notify all handlers.</summary>
        public void Raise()
        {
            if (this.Event == null)
                return;

            foreach (EventHandler handler in this.CachedInvocationList)
            {
                try
                {
                    handler.Invoke(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    this.LogError(handler, ex);
                }
            }
        }
    }
#endif
}
