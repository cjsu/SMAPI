using System.Linq;
using ContentPatcher.Framework.Tokens;
using Pathoschild.Stardew.Common.Utilities;

namespace ContentPatcher.Framework.Conditions
{
    /// <summary>A condition that can be checked against the token context.</summary>
    internal class Condition
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The token name in the context.</summary>
        public TokenName Name { get; }

        /// <summary>The token values for which this condition is valid.</summary>
        public InvariantHashSet Values { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="name">The token name in the context.</param>
        /// <param name="values">The token values for which this condition is valid.</param>
        public Condition(TokenName name, InvariantHashSet values)
        {
            this.Name = name;
            this.Values = values;
        }

        /// <summary>Whether the condition matches.</summary>
        /// <param name="context">The condition context.</param>
        public bool IsMatch(IContext context)
        {
            return context
                .GetValues(this.Name, enforceContext: true)
                .Any(value => this.Values.Contains(value));
        }
    }
}
