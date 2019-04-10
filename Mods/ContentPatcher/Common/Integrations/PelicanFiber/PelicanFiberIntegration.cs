using StardewModdingAPI;
using StardewValley;

namespace Pathoschild.Stardew.Common.Integrations.PelicanFiber
{
    /// <summary>Handles the logic for integrating with the Pelican Fiber mod.</summary>
    internal class PelicanFiberIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The full type name of the Pelican Fiber mod's build menu.</summary>
        private readonly string MenuTypeName = "PelicanFiber.Framework.ConstructionMenu";

        /// <summary>An API for accessing private code.</summary>
        private readonly IReflectionHelper Reflection;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="reflection">An API for accessing private code.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public PelicanFiberIntegration(IModRegistry modRegistry, IReflectionHelper reflection, IMonitor monitor)
            : base("Pelican Fiber", "jwdred.PelicanFiber", "3.0.2", modRegistry, monitor)
        {
            this.Reflection = reflection;
        }

        /// <summary>Get whether the Pelican Fiber build menu is open.</summary>
        public bool IsBuildMenuOpen()
        {
            this.AssertLoaded();
            return Game1.activeClickableMenu?.GetType().FullName == this.MenuTypeName;
        }

        /// <summary>Get the selected blueprint from the Pelican Fiber build menu, if it's open.</summary>
        public BluePrint GetBuildMenuBlueprint()
        {
            this.AssertLoaded();
            if (!this.IsBuildMenuOpen())
                return null;

            return this.Reflection.GetProperty<BluePrint>(Game1.activeClickableMenu, "CurrentBlueprint").GetValue();
        }
    }
}
