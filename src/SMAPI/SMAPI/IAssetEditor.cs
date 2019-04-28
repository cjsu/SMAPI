namespace StardewModdingAPI
{
    /// <summary>Edits matching content assets.</summary>
    public interface IAssetEditor
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get whether this instance can edit the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        bool CanEdit<T>(IAssetInfo asset);

        /// <summary>Edit a matched asset.</summary>
        /// <param name="asset">A helper which encapsulates metadata about an asset and enables changes to it.</param>
        void Edit<T>(IAssetData asset);
    }
}
