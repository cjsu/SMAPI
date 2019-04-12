using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.Common.Integrations.SimpleSprinkler
{
    /// <summary>The API provided by the Simple Sprinkler mod.</summary>
    public interface ISimplerSprinklerApi
    {
        /// <summary>Get the relative tile coverage for supported sprinkler IDs (additive to the game's default coverage).</summary>
        IDictionary<int, Vector2[]> GetNewSprinklerCoverage();
    }
}
