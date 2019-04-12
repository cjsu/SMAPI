using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace Pathoschild.Stardew.Common.Integrations.SimpleSprinkler
{
    /// <summary>Handles the logic for integrating with the Simple Sprinkler mod.</summary>
    internal class SimpleSprinklerIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's public API.</summary>
        private readonly ISimplerSprinklerApi ModApi;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public SimpleSprinklerIntegration(IModRegistry modRegistry, IMonitor monitor)
            : base("Simple Sprinklers", "tZed.SimpleSprinkler", "1.6.0", modRegistry, monitor)
        {
            if (!this.IsLoaded)
                return;

            // get mod API
            this.ModApi = this.GetValidatedApi<ISimplerSprinklerApi>();
            this.IsLoaded = this.ModApi != null;
        }

        /// <summary>Get the Sprinkler tiles relative to (0, 0), additive to the game's default sprinkler coverage.</summary>
        public IDictionary<int, Vector2[]> GetNewSprinklerTiles()
        {
            this.AssertLoaded();
            return this.ModApi.GetNewSprinklerCoverage();
        }
    }
}
