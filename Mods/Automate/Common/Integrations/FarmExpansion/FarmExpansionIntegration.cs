using StardewModdingAPI;
using StardewValley;

namespace Pathoschild.Stardew.Common.Integrations.FarmExpansion
{
    /// <summary>Handles the logic for integrating with the Farm Expansion mod.</summary>
    internal class FarmExpansionIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's public API.</summary>
        private readonly IFarmExpansionApi ModApi;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public FarmExpansionIntegration(IModRegistry modRegistry, IMonitor monitor)
            : base("Farm Expansion", "Advize.FarmExpansion", "3.3", modRegistry, monitor)
        {
            if (!this.IsLoaded)
                return;

            // get mod API
            this.ModApi = this.GetValidatedApi<IFarmExpansionApi>();
            this.IsLoaded = this.ModApi != null;
        }

        /// <summary>Add a blueprint to all future carpenter menus for the farm area.</summary>
        /// <param name="blueprint">The blueprint to add.</param>
        public void AddFarmBluePrint(BluePrint blueprint)
        {
            this.AssertLoaded();
            this.ModApi.AddFarmBluePrint(blueprint);
        }

        /// <summary>Add a blueprint to all future carpenter menus for the expansion area.</summary>
        /// <param name="blueprint">The blueprint to add.</param>
        public void AddExpansionBluePrint(BluePrint blueprint)
        {
            this.AssertLoaded();
            this.ModApi.AddExpansionBluePrint(blueprint);
        }
    }
}
