namespace TimeSpeed.Framework
{
    /// <summary>Represents a general location type relative to <see cref="StardewValley.GameLocation.IsOutdoors"/>.</summary>
    internal enum LocationType
    {
        /// <summary>The location is inside a building.</summary>
        Indoors,

        /// <summary>The location is outside.</summary>
        Outdoors,

        /// <summary>The mines or skull cavern.</summary>
        Mine
    }
}
