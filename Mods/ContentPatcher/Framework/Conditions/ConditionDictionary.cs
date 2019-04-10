using System.Collections.Generic;
using ContentPatcher.Framework.Tokens;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Conditions
{
    /// <summary>A set of conditions that can be checked against the context.</summary>
    internal class ConditionDictionary : Dictionary<TokenName, Condition>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Add an element with the given key and condition values.</summary>
        /// <param name="name">The token name to add.</param>
        /// <param name="values">The token values to match.</param>
        public void Add(TokenName name, IEnumerable<string> values)
        {
            this.Add(name, new Condition(name, new InvariantHashSet(values)));
        }
    }
}
