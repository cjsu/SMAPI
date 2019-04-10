using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace Pathoschild.Stardew.Common.Integrations.Cobalt
{
    /// <summary>Handles the logic for integrating with the Cobalt mod.</summary>
    internal class CobaltIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's public API.</summary>
        private readonly ICobaltApi ModApi;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public CobaltIntegration(IModRegistry modRegistry, IMonitor monitor)
            : base("Cobalt", "spacechase0.Cobalt", "1.1", modRegistry, monitor)
        {
            if (!this.IsLoaded)
                return;

            // get mod API
            this.ModApi = this.GetValidatedApi<ICobaltApi>();
            this.IsLoaded = this.ModApi != null;
        }

        /// <summary>Get the cobalt sprinkler's object ID.</summary>
        public int GetSprinklerId()
        {
            this.AssertLoaded();
            return this.ModApi.GetSprinklerId();
        }

        /// <summary>Get the configured Sprinkler tiles relative to (0, 0).</summary>
        public IEnumerable<Vector2> GetSprinklerTiles()
        {
            this.AssertLoaded();
            return this.ModApi.GetSprinklerCoverage(Vector2.Zero);
        }
    }
}
