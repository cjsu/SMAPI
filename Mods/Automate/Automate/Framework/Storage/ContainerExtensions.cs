using System;

namespace Pathoschild.Stardew.Automate.Framework.Storage
{
    /// <summary>Provides extensions for <see cref="IContainer"/> instances.</summary>
    internal static class ContainerExtensions
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get whether the container name contains a given tag.</summary>
        /// <param name="container">The container instance.</param>
        /// <param name="tag">The tag to check, excluding the '|' delimiters.</param>
        public static bool HasTag(this IContainer container, string tag)
        {
            return container.Name?.IndexOf($"|{tag}|", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        /// <summary>Get whether this container should be preferred for output when possible.</summary>
        /// <param name="container">The container instance.</param>
        public static bool ShouldIgnore(this IContainer container)
        {
            return container.HasTag("automate:ignore");
        }

        /// <summary>Get whether input is enabled for this container.</summary>
        /// <param name="container">The container instance.</param>
        public static bool AllowsInput(this IContainer container)
        {
            return !container.ShouldIgnore() && !container.HasTag("automate:noinput");
        }

        /// <summary>Get whether output is enabled for this container.</summary>
        /// <param name="container">The container instance.</param>
        public static bool AllowsOutput(this IContainer container)
        {
            return !container.ShouldIgnore() && !container.HasTag("automate:nooutput");
        }

        /// <summary>Get whether this container should be preferred for output when possible.</summary>
        /// <param name="container">The container instance.</param>
        public static bool PreferForOutput(this IContainer container)
        {
            return container.HasTag("automate:output");
        }
    }
}
