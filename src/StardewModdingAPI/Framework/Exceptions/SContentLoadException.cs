using System;
using Microsoft.Xna.Framework.Content;

namespace StardewModdingAPI.Framework.Exceptions
{
    /// <summary>An implementation of <see cref="ContentLoadException"/> used by SMAPI to detect whether it was thrown by SMAPI or the underlying framework.</summary>
    internal class SContentLoadException : ContentLoadException
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="message">The error message.</param>
        /// <param name="ex">The underlying exception, if any.</param>
        public SContentLoadException(string message, Exception ex = null)
            : base(message, ex) { }
    }
}
