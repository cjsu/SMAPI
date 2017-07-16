using System.Collections.Generic;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides metadata about installed mods.</summary>
    internal class ModRegistryHelper : BaseHelper, IModRegistry
    {
        /*********
        ** Properties
        *********/
        /// <summary>The underlying mod registry.</summary>
        private readonly ModRegistry Registry;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="registry">The underlying mod registry.</param>
        public ModRegistryHelper(string modID, ModRegistry registry)
            : base(modID)
        {
            this.Registry = registry;
        }

        /// <summary>Get metadata for all loaded mods.</summary>
        public IEnumerable<IManifest> GetAll()
        {
            return this.Registry.GetAll();
        }

        /// <summary>Get metadata for a loaded mod.</summary>
        /// <param name="uniqueID">The mod's unique ID.</param>
        /// <returns>Returns the matching mod's metadata, or <c>null</c> if not found.</returns>
        public IManifest Get(string uniqueID)
        {
            return this.Registry.Get(uniqueID);
        }

        /// <summary>Get whether a mod has been loaded.</summary>
        /// <param name="uniqueID">The mod's unique ID.</param>
        public bool IsLoaded(string uniqueID)
        {
            return this.Registry.IsLoaded(uniqueID);
        }
    }
}
