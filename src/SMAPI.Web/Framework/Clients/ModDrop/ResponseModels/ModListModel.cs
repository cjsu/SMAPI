using System.Collections.Generic;

namespace StardewModdingAPI.Web.Framework.Clients.ModDrop.ResponseModels
{
    /// <summary>A list of mods from the ModDrop API.</summary>
    public class ModListModel
    {
        /// <summary>The mod data.</summary>
        public IDictionary<long, ModModel> Mods { get; set; }
    }
}
