namespace StardewModdingAPI.Framework.Content
{
    /// <summary>Basic metadata about a vanilla tilesheet.</summary>
    internal class TilesheetReference
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The tilesheet's index in the list.</summary>
        public readonly int Index;

        /// <summary>The tilesheet's unique ID in the map.</summary>
        public readonly string Id;

        /// <summary>The asset path for the tilesheet texture.</summary>
        public readonly string ImageSource;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="index">The tilesheet's index in the list.</param>
        /// <param name="id">The tilesheet's unique ID in the map.</param>
        /// <param name="imageSource">The asset path for the tilesheet texture.</param>
        public TilesheetReference(int index, string id, string imageSource)
        {
            this.Index = index;
            this.Id = id;
            this.ImageSource = imageSource;
        }
    }
}
