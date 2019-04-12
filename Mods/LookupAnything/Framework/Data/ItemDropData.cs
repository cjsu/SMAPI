namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>A loot entry parsed from the game data.</summary>
    internal class ItemDropData
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The item's parent sprite index.</summary>
        public int ItemID { get; }

        /// <summary>The maximum number to drop.</summary>
        public int MaxDrop { get; }

        /// <summary>The probability that the item will be dropped.</summary>
        public float Probability { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="itemID">The item's parent sprite index.</param>
        /// <param name="maxDrop">The maximum number to drop.</param>
        /// <param name="probability">The probability that the item will be dropped.</param>
        public ItemDropData(int itemID, int maxDrop, float probability)
        {
            this.ItemID = itemID;
            this.MaxDrop = maxDrop;
            this.Probability = probability;
        }
    }
}
