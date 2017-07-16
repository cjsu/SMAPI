using System.Collections.Generic;

namespace StardewModdingAPI
{
    /// <summary>Provides an API for fetching metadata about loaded mods.</summary>
    public interface IModRegistry : IModLinked
    {
        /// <summary>Get metadata for all loaded mods.</summary>
        IEnumerable<IManifest> GetAll();

        /// <summary>Get metadata for a loaded mod.</summary>
        /// <param name="uniqueID">The mod's unique ID.</param>
        /// <returns>Returns the matching mod's metadata, or <c>null</c> if not found.</returns>
        IManifest Get(string uniqueID);

        /// <summary>Get whether a mod has been loaded.</summary>
        /// <param name="uniqueID">The mod's unique ID.</param>
        bool IsLoaded(string uniqueID);
    }
}