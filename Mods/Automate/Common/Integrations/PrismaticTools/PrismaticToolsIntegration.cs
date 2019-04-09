using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace Pathoschild.Stardew.Common.Integrations.PrismaticTools
{
    /// <summary>Handles the logic for integrating with the Prismatic Tools mod.</summary>
    internal class PrismaticToolsIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's public API.</summary>
        private readonly IPrismaticToolsApi ModApi;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public PrismaticToolsIntegration(IModRegistry modRegistry, IMonitor monitor)
            : base("Prismatic Tools", "stokastic.PrismaticTools", "1.3.0", modRegistry, monitor)
        {
            if (!this.IsLoaded)
                return;

            // get mod API
            this.ModApi = this.GetValidatedApi<IPrismaticToolsApi>();
            this.IsLoaded = this.ModApi != null;
        }

        /// <summary>Get whether prismatic sprinklers also act as scarecrows.</summary>
        public bool ArePrismaticSprinklersScarecrows()
        {
            this.AssertLoaded();
            return this.ModApi.ArePrismaticSprinklersScarecrows;
        }

        /// <summary>Get the prismatic sprinkler object ID.</summary>
        public int GetSprinklerID()
        {
            this.AssertLoaded();
            return this.ModApi.SprinklerIndex;
        }

        /// <summary>Get the relative tile coverage for a prismatic sprinkler.</summary>
        public IEnumerable<Vector2> GetSprinklerCoverage()
        {
            this.AssertLoaded();
            return this.ModApi.GetSprinklerCoverage(Vector2.Zero);
        }
    }
}
