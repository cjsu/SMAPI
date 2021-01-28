using System;
using StardewModdingAPI.Events;

namespace StardewModdingAPI.Framework.Events
{
    /// <summary>An event handler wrapper which tracks metadata about an event handler.</summary>
    /// <typeparam name="TEventArgs">The event arguments type.</typeparam>
    internal class ManagedEventHandler<TEventArgs> : IComparable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The event handler method.</summary>
        public EventHandler<TEventArgs> Handler { get; }

        /// <summary>The order in which the event handler was registered, relative to other handlers for this event.</summary>
        public int RegistrationOrder { get; }

        /// <summary>The event handler priority, relative to other handlers for this event.</summary>
        public EventPriority Priority { get; }

        /// <summary>The mod which registered the handler.</summary>
        public IModMetadata SourceMod { get; set; }


        /*********
        ** Accessors
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="handler">The event handler method.</param>
        /// <param name="registrationOrder">The order in which the event handler was registered, relative to other handlers for this event.</param>
        /// <param name="priority">The event handler priority, relative to other handlers for this event.</param>
        /// <param name="sourceMod">The mod which registered the handler.</param>
        public ManagedEventHandler(EventHandler<TEventArgs> handler, int registrationOrder, EventPriority priority, IModMetadata sourceMod)
        {
            this.Handler = handler;
            this.RegistrationOrder = registrationOrder;
            this.Priority = priority;
            this.SourceMod = sourceMod;
        }

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <exception cref="T:System.ArgumentException"><paramref name="obj" /> is not the same type as this instance.</exception>
        public int CompareTo(object obj)
        {
            if (!(obj is ManagedEventHandler<TEventArgs> other))
                throw new ArgumentException("Can't compare to an unrelated object type.");

            int priorityCompare = -this.Priority.CompareTo(other.Priority); // higher value = sort first
            return priorityCompare != 0
                ? priorityCompare
                : this.RegistrationOrder.CompareTo(other.RegistrationOrder);
        }
    }
}
