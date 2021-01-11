namespace StardewModdingAPI.Events
{
    /// <summary>Indicates how an inventory item changed.</summary>
    public enum ChangeType
    {
        /// <summary>The entire stack was removed.</summary>
        Removed,

        /// <summary>The entire stack was added.</summary>
        Added,

        /// <summary>The stack size changed.</summary>
        StackChange
    }
}
