namespace Pathoschild.Stardew.LookupAnything.Framework.Models
{
    /// <summary>An object entry parsed from the game's data files.</summary>
    internal class ObjectModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The object's index in the object sprite sheet.</summary>
        public int ParentSpriteIndex { get; }

        /// <summary>The object name.</summary>
        public string Name { get; }

        /// <summary>The base description. This may be overridden by game logic (e.g. for the Gunther-can-tell-you-more messages).</summary>
        public string Description { get; }

        /// <summary>The base sale price.</summary>
        public int Price { get; }

        /// <summary>How edible the item is, where -300 is inedible.</summary>
        public int Edibility { get; }

        /// <summary>The type name.</summary>
        public string Type { get; }

        /// <summary>The category ID (or <c>0</c> if there is none).</summary>
        public int Category { get; }



        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="parentSpriteIndex">The object's index in the object sprite sheet.</param>
        /// <param name="name">The object name.</param>
        /// <param name="description">The base description.</param>
        /// <param name="price">The base sale price.</param>
        /// <param name="edibility">How edible the item is, where -300 is inedible.</param>
        /// <param name="type">The type name.</param>
        /// <param name="category">The category ID (or <c>0</c> if there is none).</param>
        public ObjectModel(int parentSpriteIndex, string name, string description, int price, int edibility, string type, int category)
        {
            this.ParentSpriteIndex = parentSpriteIndex;
            this.Name = name;
            this.Description = description;
            this.Price = price;
            this.Edibility = edibility;
            this.Type = type;
            this.Category = category;
        }
    }
}
