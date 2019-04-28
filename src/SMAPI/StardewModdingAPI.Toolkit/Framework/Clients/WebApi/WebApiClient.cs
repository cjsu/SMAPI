using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using StardewModdingAPI.Toolkit.Serialisation;

namespace StardewModdingAPI.Toolkit.Framework.Clients.WebApi
{
    /// <summary>Provides methods for interacting with the SMAPI web API.</summary>
    public class WebApiClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>The base URL for the web API.</summary>
        private readonly Uri BaseUrl;

        /// <summary>The API version number.</summary>
        private readonly ISemanticVersion Version;

        /// <summary>The JSON serializer settings to use.</summary>
        private readonly JsonSerializerSettings JsonSettings = new JsonHelper().JsonSettings;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="baseUrl">The base URL for the web API.</param>
        /// <param name="version">The web API version.</param>
        public WebApiClient(string baseUrl, ISemanticVersion version)
        {
            this.BaseUrl = new Uri(baseUrl);
            this.Version = version;
        }

        /// <summary>Get metadata about a set of mods from the web API.</summary>
        /// <param name="mods">The mod keys for which to fetch the latest version.</param>
        /// <param name="includeExtendedMetadata">Whether to include extended metadata for each mod.</param>
        public IDictionary<string, ModEntryModel> GetModInfo(ModSearchEntryModel[] mods, bool includeExtendedMetadata = false)
        {
            return this.Post<ModSearchModel, ModEntryModel[]>(
                $"v{this.Version}/mods",
                new ModSearchModel(mods, includeExtendedMetadata)
            ).ToDictionary(p => p.ID);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Fetch the response from the backend API.</summary>
        /// <typeparam name="TBody">The body content type.</typeparam>
        /// <typeparam name="TResult">The expected response type.</typeparam>
        /// <param name="url">The request URL, optionally excluding the base URL.</param>
        /// <param name="content">The body content to post.</param>
        private TResult Post<TBody, TResult>(string url, TBody content)
        {
            // note: avoid HttpClient for Mac compatibility
            using (WebClient client = new WebClient())
            {
                Uri fullUrl = new Uri(this.BaseUrl, url);
                string data = JsonConvert.SerializeObject(content);

                client.Headers["Content-Type"] = "application/json";
                client.Headers["User-Agent"] = $"SMAPI/{this.Version}";
                string response = client.UploadString(fullUrl, data);
                return JsonConvert.DeserializeObject<TResult>(response, this.JsonSettings);
            }
        }
    }
}
