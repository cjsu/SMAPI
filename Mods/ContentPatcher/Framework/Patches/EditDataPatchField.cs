using System.Collections.Generic;
using ContentPatcher.Framework.Conditions;

namespace ContentPatcher.Framework.Patches
{
    /// <summary>An specific field in a data file to change.</summary>
    internal class EditDataPatchField
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique key for the entry in the data file.</summary>
        public TokenString Key { get; }

        /// <summary>The field index to change.</summary>
        public int FieldIndex { get; }

        /// <summary>The entry value to set.</summary>
        public TokenString Value { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="key">The unique key for the entry in the data file.</param>
        /// <param name="field">The field number to change.</param>
        /// <param name="value">The entry value to set.</param>
        public EditDataPatchField(TokenString key, int field, TokenString value)
        {
            this.Key = key;
            this.FieldIndex = field;
            this.Value = value;
        }

        /// <summary>Get all token strings used in the record.</summary>
        public IEnumerable<TokenString> GetTokenStrings()
        {
            yield return this.Key;
            yield return this.Value;
        }
    }
}
