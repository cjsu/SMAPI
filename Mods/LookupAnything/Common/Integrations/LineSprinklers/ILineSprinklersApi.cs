using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.Common.Integrations.LineSprinklers
{
    /// <summary>The API provided by the Line Sprinklers mod.</summary>
    public interface ILineSprinklersApi
    {
        /// <summary>Get the maximum supported coverage width or height.</summary>
        int GetMaxGridSize();

        /// <summary>Get the relative tile coverage by supported sprinkler ID.</summary>
        IDictionary<int, Vector2[]> GetSprinklerCoverage();
    }
}
