using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.Common.Integrations.Cobalt
{
    /// <summary>The API provided by the Cobalt mod.</summary>
    public interface ICobaltApi
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get the cobalt sprinkler's object ID.</summary>
        int GetSprinklerId();

        /// <summary>Get the cobalt sprinkler coverage.</summary>
        /// <param name="origin">The tile position containing the sprinkler.</param>
        IEnumerable<Vector2> GetSprinklerCoverage(Vector2 origin);
    }
}
