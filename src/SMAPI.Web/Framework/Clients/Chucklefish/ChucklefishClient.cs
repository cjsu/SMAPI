using System;
using System.Net;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Pathoschild.Http.Client;

namespace StardewModdingAPI.Web.Framework.Clients.Chucklefish
{
    /// <summary>An HTTP client for fetching mod metadata from the Chucklefish mod site.</summary>
    internal class ChucklefishClient : IChucklefishClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>The URL for a mod page excluding the base URL, where {0} is the mod ID.</summary>
        private readonly string ModPageUrlFormat;

        /// <summary>The underlying HTTP client.</summary>
        private readonly IClient Client;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="userAgent">The user agent for the API client.</param>
        /// <param name="baseUrl">The base URL for the Chucklefish mod site.</param>
        /// <param name="modPageUrlFormat">The URL for a mod page excluding the <paramref name="baseUrl"/>, where {0} is the mod ID.</param>
        public ChucklefishClient(string userAgent, string baseUrl, string modPageUrlFormat)
        {
            this.ModPageUrlFormat = modPageUrlFormat;
            this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
        }

        /// <summary>Get metadata about a mod.</summary>
        /// <param name="id">The Chucklefish mod ID.</param>
        /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
        public async Task<ChucklefishMod> GetModAsync(uint id)
        {
            // fetch HTML
            string html;
            try
            {
                html = await this.Client
                    .GetAsync(string.Format(this.ModPageUrlFormat, id))
                    .AsString();
            }
            catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound || ex.Status == HttpStatusCode.Forbidden)
            {
                return null;
            }

            // parse HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // extract mod info
            string url = this.GetModUrl(id);
            string name = doc.DocumentNode.SelectSingleNode("//meta[@name='twitter:title']").Attributes["content"].Value;
            if (name.StartsWith("[SMAPI] "))
                name = name.Substring("[SMAPI] ".Length);
            string version = doc.DocumentNode.SelectSingleNode("//h1/span").InnerText;

            // create model
            return new ChucklefishMod
            {
                Name = name,
                Version = version,
                Url = url
            };
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.Client?.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the full mod page URL for a given ID.</summary>
        /// <param name="id">The mod ID.</param>
        private string GetModUrl(uint id)
        {
            UriBuilder builder = new UriBuilder(this.Client.BaseClient.BaseAddress);
            builder.Path += string.Format(this.ModPageUrlFormat, id);
            return builder.Uri.ToString();
        }
    }
}
