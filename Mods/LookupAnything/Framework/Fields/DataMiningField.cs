using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;

namespace Pathoschild.Stardew.LookupAnything.Framework.Fields
{
    /// <summary>Shows a collection of debug fields.</summary>
    internal class DataMiningField : GenericField
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="label">A short field label.</param>
        /// <param name="fields">The debug fields to display.</param>
        public DataMiningField(GameHelper gameHelper, string label, IEnumerable<IDebugField> fields)
            : base(gameHelper, label)
        {
            IDebugField[] fieldArray = fields?.ToArray() ?? new IDebugField[0];
            this.HasValue = fieldArray.Any();
            if (this.HasValue)
                this.Value = this.GetFormattedText(fieldArray).ToArray();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a formatted representation of a set of debug fields.</summary>
        /// <param name="fields">The debug fields to display.</param>
        private IEnumerable<IFormattedText> GetFormattedText(IDebugField[] fields)
        {
            for (int i = 0, last = fields.Length - 1; i <= last; i++)
            {
                IDebugField field = fields[i];
                yield return new FormattedText("*", Color.Red, bold: true);
                yield return new FormattedText($"{field.Label}:");
                yield return i != last
                    ? new FormattedText($"{field.Value}{Environment.NewLine}")
                    : new FormattedText(field.Value);
            }
        }
    }
}
