using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Pathoschild.Stardew.Common.Integrations.Automate
{
    /// <summary>The API provided by the Automate mod.</summary>
    public interface IAutomateApi
    {
        /// <summary>Get the status of machines in a tile area. This is a specialised API for Data Layers and similar mods.</summary>
        /// <param name="location">The location for which to display data.</param>
        /// <param name="tileArea">The tile area for which to display data.</param>
        IDictionary<Vector2, int> GetMachineStates(GameLocation location, Rectangle tileArea);
    }
}
