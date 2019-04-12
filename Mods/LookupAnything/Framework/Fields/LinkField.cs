using System;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.LookupAnything.Framework.Subjects;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>A metadata field containing clickable links.</summary>
    internal class LinkField : GenericField, ILinkField
    {
        /*********
        ** Fields
        *********/
        /// <summary>Gets the subject the link points to.</summary>
        private readonly Func<ISubject> Subject;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="text">The link text.</param>
        /// <param name="subject">Gets the subject the link points to.</param>
        public LinkField(GameHelper gameHelper, string label, string text, Func<ISubject> subject)
            : base(gameHelper, label, new FormattedText(text, Color.Blue))
        {
            this.Subject = subject;
        }

        /// <summary>Get the subject the link points to.</summary>
        public ISubject GetLinkSubject()
        {
            return this.Subject();
        }
    }
}
