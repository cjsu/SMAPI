using System;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>An exception raised when an incompatible instruction is found while loading a mod assembly.</summary>
    internal class IncompatibleInstructionException : Exception
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A brief noun phrase which describes the incompatible instruction that was found.</summary>
        public string NounPhrase { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="nounPhrase">A brief noun phrase which describes the incompatible instruction that was found.</param>
        public IncompatibleInstructionException(string nounPhrase)
            : base($"Found an incompatible CIL instruction ({nounPhrase}).")
        {
            this.NounPhrase = nounPhrase;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="nounPhrase">A brief noun phrase which describes the incompatible instruction that was found.</param>
        /// <param name="message">A message which describes the error.</param>
        public IncompatibleInstructionException(string nounPhrase, string message)
            : base(message)
        {
            this.NounPhrase = nounPhrase;
        }
    }
}
