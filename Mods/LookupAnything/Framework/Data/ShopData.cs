namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>Metadata for a shop that isn't available from the game data directly.</summary>
    internal class ShopData
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The internal name of the shop's indoor location.</summary>
        public string LocationName { get; set; }

        /// <summary>The translation key for the shop name.</summary>
        public string DisplayKey { get; set; }

        /// <summary>The categories of items that the player can sell to this shop.</summary>
        public int[] BuysCategories { get; set; }
    }
}
