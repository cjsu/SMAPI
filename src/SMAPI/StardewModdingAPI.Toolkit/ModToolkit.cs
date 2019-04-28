using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
//using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.ModScanning;
using StardewModdingAPI.Toolkit.Serialisation;

namespace StardewModdingAPI.Toolkit
{
    /// <summary>A convenience wrapper for the various tools.</summary>
    public class ModToolkit
    {
        /*********
        ** Fields
        *********/
        /// <summary>The default HTTP user agent for the toolkit.</summary>
        private readonly string UserAgent;

        /// <summary>Maps vendor keys (like <c>Nexus</c>) to their mod URL template (where <c>{0}</c> is the mod ID). This doesn't affect update checks, which defer to the remote web API.</summary>
        private readonly IDictionary<string, string> VendorModUrls = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
        {
            ["Chucklefish"] = "https://community.playstarbound.com/resources/{0}",
            ["GitHub"] = "https://github.com/{0}/releases",
            ["Nexus"] = "https://www.nexusmods.com/stardewvalley/mods/{0}"
        };


        /*********
        ** Accessors
        *********/
        /// <summary>Encapsulates SMAPI's JSON parsing.</summary>
        public JsonHelper JsonHelper { get; } = new JsonHelper();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public ModToolkit()
        {
            ISemanticVersion version = new SemanticVersion(this.GetType().Assembly.GetName().Version);
            this.UserAgent = $"SMAPI Mod Handler Toolkit/{version}";
        }

        /// <summary>Extract mod metadata from the wiki compatibility list.</summary>
        //public async Task<WikiModList> GetWikiCompatibilityListAsync()
        //{
        //    var client = new WikiClient(this.UserAgent);
        //    return await client.FetchModsAsync();
        //}

        /// <summary>Get SMAPI's internal mod database.</summary>
        /// <param name="metadataPath">The file path for the SMAPI metadata file.</param>
        public ModDatabase GetModDatabase(string metadataPath)
        {
            MetadataModel metadata = JsonConvert.DeserializeObject<MetadataModel>(File.ReadAllText(metadataPath));
            ModDataRecord[] records = metadata.ModData.Select(pair => new ModDataRecord(pair.Key, pair.Value)).ToArray();
            return new ModDatabase(records, this.GetUpdateUrl);
        }

        /// <summary>Extract information about all mods in the given folder.</summary>
        /// <param name="rootPath">The root folder containing mods.</param>
        public IEnumerable<ModFolder> GetModFolders(string rootPath)
        {
            return new ModScanner(this.JsonHelper).GetModFolders(rootPath);
        }

        /// <summary>Get an update URL for an update key (if valid).</summary>
        /// <param name="updateKey">The update key.</param>
        public string GetUpdateUrl(string updateKey)
        {
            string[] parts = updateKey.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
                return null;

            string vendorKey = parts[0].Trim();
            string modID = parts[1].Trim();

            if (this.VendorModUrls.TryGetValue(vendorKey, out string urlTemplate))
                return string.Format(urlTemplate, modID);

            return null;
        }
    }
}
