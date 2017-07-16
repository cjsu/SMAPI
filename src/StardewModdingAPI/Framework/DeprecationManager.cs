using System;
using System.Collections.Generic;
using System.Reflection;

namespace StardewModdingAPI.Framework
{
    /// <summary>Manages deprecation warnings.</summary>
    internal class DeprecationManager
    {
        /*********
        ** Properties
        *********/
        /// <summary>The deprecations which have already been logged (as 'mod name::noun phrase::version').</summary>
        private readonly HashSet<string> LoggedDeprecations = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>Encapsulates monitoring and logging for a given module.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Tracks the installed mods.</summary>
        private readonly ModRegistry ModRegistry;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging for a given module.</param>
        /// <param name="modRegistry">Tracks the installed mods.</param>
        public DeprecationManager(IMonitor monitor, ModRegistry modRegistry)
        {
            this.Monitor = monitor;
            this.ModRegistry = modRegistry;
        }

        /// <summary>Log a deprecation warning.</summary>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated.</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <param name="severity">How deprecated the code is.</param>
        public void Warn(string nounPhrase, string version, DeprecationLevel severity)
        {
            this.Warn(this.ModRegistry.GetModFromStack(), nounPhrase, version, severity);
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

            // show SMAPI 2.0 meta-warning
            if(this.MarkWarned("SMAPI", "SMAPI 2.0 meta-warning", "2.0"))
                this.Monitor.Log("Some mods may stop working in SMAPI 2.0 (but they'll work fine for now). Try updating mods with 'deprecated code' warnings; if that doesn't remove the warnings, let the mod authors know about this message or see http://stardewvalleywiki.com/Modding:SMAPI_2.0 for details.", LogLevel.Warn);

            // build message
            string message = $"{source ?? "An unknown mod"} uses deprecated code ({nounPhrase}).";
            if (source == null)
                message += $"{Environment.NewLine}{Environment.StackTrace}";

            // log message
            switch (severity)
            {
                case DeprecationLevel.Notice:
                    this.Monitor.Log(message, LogLevel.Trace);
                    break;

                case DeprecationLevel.Info:
                    this.Monitor.Log(message, LogLevel.Debug);
                    break;

                case DeprecationLevel.PendingRemoval:
                    this.Monitor.Log(message, LogLevel.Warn);
                    break;

                default:
                    throw new NotSupportedException($"Unknown deprecation level '{severity}'");
            }
        }

        /// <summary>Mark a deprecation warning as already logged.</summary>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated (e.g. "the Extensions.AsInt32 method").</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <returns>Returns whether the deprecation was successfully marked as warned. Returns <c>false</c> if it was already marked.</returns>
        public bool MarkWarned(string nounPhrase, string version)
        {
            return this.MarkWarned(this.ModRegistry.GetModFromStack(), nounPhrase, version);
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

        /// <summary>Get whether a type implements the given virtual method.</summary>
        /// <param name="subtype">The type to check.</param>
        /// <param name="baseType">The base type which declares the virtual method.</param>
        /// <param name="name">The method name.</param>
        /// <param name="argumentTypes">The expected argument types.</param>
        internal bool IsVirtualMethodImplemented(Type subtype, Type baseType, string name, Type[] argumentTypes)
        {
            MethodInfo method = subtype.GetMethod(nameof(Mod.Entry), argumentTypes);
            return method.DeclaringType != baseType;
        }
    }
}
