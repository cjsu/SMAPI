namespace StardewModdingAPI.Framework
{
    /// <summary>A deprecation warning for a mod.</summary>
    internal class DeprecationWarning
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The affected mod's display name.</summary>
        public string ModName { get; }

        /// <summary>A noun phrase describing what is deprecated.</summary>
        public string NounPhrase { get; }

        /// <summary>The SMAPI version which deprecated it.</summary>
        public string Version { get; }

        /// <summary>The deprecation level for the affected code.</summary>
        public DeprecationLevel Level { get; }

        /// <summary>The stack trace when the deprecation warning was raised.</summary>
        public string StackTrace { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modName">The affected mod's display name.</param>
        /// <param name="nounPhrase">A noun phrase describing what is deprecated.</param>
        /// <param name="version">The SMAPI version which deprecated it.</param>
        /// <param name="level">The deprecation level for the affected code.</param>
        /// <param name="stackTrace">The stack trace when the deprecation warning was raised.</param>
        public DeprecationWarning(string modName, string nounPhrase, string version, DeprecationLevel level, string stackTrace)
        {
            this.ModName = modName;
            this.NounPhrase = nounPhrase;
            this.Version = version;
            this.Level = level;
            this.StackTrace = stackTrace;
        }
    }
}
