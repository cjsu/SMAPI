using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Pathoschild.Stardew.Common.Integrations.Automate
{
    /// <summary>Handles the logic for integrating with the Automate mod.</summary>
    internal class AutomateIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's public API.</summary>
        private readonly IAutomateApi ModApi;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public AutomateIntegration(IModRegistry modRegistry, IMonitor monitor)
            : base("Automate", "Pathoschild.Automate", "1.11.0", modRegistry, monitor)
        {
            if (!this.IsLoaded)
                return;

            // get mod API
            this.ModApi = this.GetValidatedApi<IAutomateApi>();
            this.IsLoaded = this.ModApi != null;
        }

        /// <summary>Get the status of machines in a tile area. This is a specialised API for Data Layers and similar mods.</summary>
        /// <param name="location">The location for which to display data.</param>
        /// <param name="tileArea">The tile area for which to display data.</param>
        public IDictionary<Vector2, int> GetMachineStates(GameLocation location, Rectangle tileArea)
        {
            this.AssertLoaded();
            return this.ModApi.GetMachineStates(location, tileArea);
        }
    }
}
