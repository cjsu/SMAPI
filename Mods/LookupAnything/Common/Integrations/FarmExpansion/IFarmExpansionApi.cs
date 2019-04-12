using StardewValley;

namespace Pathoschild.Stardew.Common.Integrations.FarmExpansion
{
    /// <summary>The API provided by the Farm Expansion mod.</summary>
    public interface IFarmExpansionApi
    {
        /// <summary>Add a blueprint to all future carpenter menus for the farm area.</summary>
        /// <param name="blueprint">The blueprint to add.</param>
        void AddFarmBluePrint(BluePrint blueprint);

        /// <summary>Add a blueprint to all future carpenter menus for the expansion area.</summary>
        /// <param name="blueprint">The blueprint to add.</param>
        void AddExpansionBluePrint(BluePrint blueprint);
    }
}
