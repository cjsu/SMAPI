using System.Collections.Generic;
using ContentPatcher.Framework.Conditions;

namespace ContentPatcher.Framework.Patches
{
    /// <summary>An entry in a data file to change.</summary>
    internal class EditDataPatchRecord
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The unique key for the entry in the data file.</summary>
        public TokenString Key { get; }

        /// <summary>The entry value to set.</summary>
        public TokenString Value { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="key">The unique key for the entry in the data file.</param>
        /// <param name="value">The entry value to set.</param>
        public EditDataPatchRecord(TokenString key, TokenString value)
        {
            this.Key = key;
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
