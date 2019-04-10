using StardewModdingAPI;

namespace Pathoschild.Stardew.Common.Integrations.BetterJunimos
{
    /// <summary>Handles the logic for integrating with the Better Junimos mod.</summary>
    internal class BetterJunimosIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's public API.</summary>
        private readonly IBetterJunimosApi ModApi;


        /*********
        ** Accessors
        *********/
        /// <summary>The Junimo Hut coverage radius.</summary>
        public int MaxRadius { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public BetterJunimosIntegration(IModRegistry modRegistry, IMonitor monitor)
            : base("Better Junimos", "hawkfalcon.BetterJunimos", "0.5.0", modRegistry, monitor)
        {
            if (!this.IsLoaded)
                return;

            // get mod API
            this.ModApi = this.GetValidatedApi<IBetterJunimosApi>();
            this.IsLoaded = this.ModApi != null;
            this.MaxRadius = this.ModApi?.GetJunimoHutMaxRadius() ?? 0;
        }
    }
}
