using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Framework
{
    /// <summary>Manages deprecation warnings.</summary>
    internal class DeprecationManager
    {
        /*********
        ** Fields
        *********/
        /// <summary>The deprecations which have already been logged (as 'mod name::noun phrase::version').</summary>
        private readonly HashSet<string> LoggedDeprecations = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>Encapsulates monitoring and logging for a given module.</summary>
#if !SMAPI_3_0_STRICT
        private readonly Monitor Monitor;
#else
        private readonly IMonitor Monitor;
#endif

        /// <summary>Tracks the installed mods.</summary>
        private readonly ModRegistry ModRegistry;

        /// <summary>The queued deprecation warnings to display.</summary>
        private readonly IList<DeprecationWarning> QueuedWarnings = new List<DeprecationWarning>();

#if !SMAPI_3_0_STRICT
        /// <summary>Whether the one-time deprecation message has been shown.</summary>
        private bool DeprecationHeaderShown = false;
#endif


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging for a given module.</param>
        /// <param name="modRegistry">Tracks the installed mods.</param>
#if !SMAPI_3_0_STRICT
        public DeprecationManager(Monitor monitor, ModRegistry modRegistry)
#else
        public DeprecationManager(IMonitor monitor, ModRegistry modRegistry)
#endif
        {
            this.Monitor = monitor;
            this.ModRegistry = modRegistry;
        }

        /// <summary>Log a deprecation warning for the old-style events.</summary>
        public void WarnForOldEvents()
        {
            this.Warn("legacy events", "2.9", DeprecationLevel.PendingRemoval);
        }

        /// <summary>Log a deprecation warning.</summary>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated.</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <param name="severity">How deprecated the code is.</param>
        public void Warn(string nounPhrase, string version, DeprecationLevel severity)
        {
            this.Warn(this.ModRegistry.GetFromStack()?.DisplayName, nounPhrase, version, severity);
        }

        /// <summary>Log a deprecation warning.</summary>
        /// <param name="source">The friendly mod name which used the deprecated code.</param>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated.</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <param name="severity">How deprecated the code is.</param>
        public void Warn(string source, string nounPhrase, string version, DeprecationLevel severity)
        {
            // ignore if already warned
            if (!this.MarkWarned(source ?? "<unknown>", nounPhrase, version))
                return;

            // queue warning
            this.QueuedWarnings.Add(new DeprecationWarning(source, nounPhrase, version, severity, Environment.StackTrace));
        }

        /// <summary>Print any queued messages.</summary>
        public void PrintQueued()
        {
#if !SMAPI_3_0_STRICT
            if (!this.DeprecationHeaderShown && this.QueuedWarnings.Any())
            {
                this.Monitor.Newline();
                this.Monitor.Log("Some of your mods will break in the upcoming SMAPI 3.0. Please update your mods now, or notify the author if no update is available. See https://mods.smapi.io for links to the latest versions.", LogLevel.Warn);
                this.Monitor.Newline();
                this.DeprecationHeaderShown = true;
            }
#endif

            foreach (DeprecationWarning warning in this.QueuedWarnings.OrderBy(p => p.ModName).ThenBy(p => p.NounPhrase))
            {
                // build message
#if SMAPI_3_0_STRICT
                string message = $"{warning.ModName} uses deprecated code ({warning.NounPhrase} is deprecated since SMAPI {warning.Version}).";
#else
                string message = warning.NounPhrase == "legacy events"
                    ? $"{warning.ModName ?? "An unknown mod"} will break in the upcoming SMAPI 3.0 (legacy events are deprecated since SMAPI {warning.Version})."
                    : $"{warning.ModName ?? "An unknown mod"} will break in the upcoming SMAPI 3.0 ({warning.NounPhrase} is deprecated since SMAPI {warning.Version}).";
#endif

                // get log level
                LogLevel level;
                switch (warning.Level)
                {
                    case DeprecationLevel.Notice:
                        level = LogLevel.Trace;
                        break;

                    case DeprecationLevel.Info:
                        level = LogLevel.Debug;
                        break;

                    case DeprecationLevel.PendingRemoval:
                        level = LogLevel.Warn;
                        break;

                    default:
                        throw new NotSupportedException($"Unknown deprecation level '{warning.Level}'.");
                }

                // log message
                if (warning.ModName != null)
                    this.Monitor.Log(message, level);
                else
                {
                    if (level == LogLevel.Trace)
                        this.Monitor.Log($"{message}\n{warning.StackTrace}", level);
                    else
                    {
                        this.Monitor.Log(message, level);
                        this.Monitor.Log(warning.StackTrace);
                    }
                }
            }
            this.QueuedWarnings.Clear();
        }

        /// <summary>Mark a deprecation warning as already logged.</summary>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated (e.g. "the Extensions.AsInt32 method").</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <returns>Returns whether the deprecation was successfully marked as warned. Returns <c>false</c> if it was already marked.</returns>
        public bool MarkWarned(string nounPhrase, string version)
        {
            return this.MarkWarned(this.ModRegistry.GetFromStack()?.DisplayName, nounPhrase, version);
        }

        /// <summary>Mark a deprecation warning as already logged.</summary>
        /// <param name="source">The friendly name of the assembly which used the deprecated code.</param>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated (e.g. "the Extensions.AsInt32 method").</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <returns>Returns whether the deprecation was successfully marked as warned. Returns <c>false</c> if it was already marked.</returns>
        public bool MarkWarned(string source, string nounPhrase, string version)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new InvalidOperationException("The deprecation source cannot be empty.");

            string key = $"{source}::{nounPhrase}::{version}";
            if (this.LoggedDeprecations.Contains(key))
                return false;
            this.LoggedDeprecations.Add(key);
            return true;
        }
    }
}
