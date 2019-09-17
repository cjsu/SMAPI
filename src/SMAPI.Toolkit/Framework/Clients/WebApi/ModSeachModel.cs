using System.Linq;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi
{
    /// <summary>Specifies mods whose update-check info to fetch.</summary>
    public class ModSearchModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mods for which to find data.</summary>
        public ModSearchEntryModel[] Mods { get; set; }

        /// <summary>Whether to include extended metadata for each mod.</summary>
        public bool IncludeExtendedMetadata { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an empty instance.</summary>
        public ModSearchModel()
        {
            // needed for JSON deserialising
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="mods">The mods to search.</param>
        /// <param name="includeExtendedMetadata">Whether to include extended metadata for each mod.</param>
        public ModSearchModel(ModSearchEntryModel[] mods, bool includeExtendedMetadata)
        {
            this.Mods = mods.ToArray();
            this.IncludeExtendedMetadata = includeExtendedMetadata;
        }
    }
}
