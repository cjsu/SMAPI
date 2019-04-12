using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;

namespace Pathoschild.Stardew.LookupAnything.Framework.Subjects
{
    /// <summary>Provides metadata about something in the game.</summary>
    internal interface ISubject
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The display name.</summary>
        string Name { get; }

        /// <summary>The item description (if applicable).</summary>
        string Description { get; }

        /// <summary>The item type (if applicable).</summary>
        string Type { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        IEnumerable<ICustomField> GetData(Metadata metadata);

        /// <summary>Get raw debug data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        IEnumerable<IDebugField> GetDebugFields(Metadata metadata);

        /// <summary>Draw the subject portrait (if available).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="size">The size of the portrait to draw.</param>
        /// <returns>Returns <c>true</c> if a portrait was drawn, else <c>false</c>.</returns>
        bool DrawPortrait(SpriteBatch spriteBatch, Vector2 position, Vector2 size);
    }
}
