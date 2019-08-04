using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Pathoschild.Http.Client;

namespace StardewModdingAPI.Web.Framework.Clients.Pastebin
{
    /// <summary>An API client for Pastebin.</summary>
    internal class PastebinClient : IPastebinClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying HTTP client.</summary>
        private readonly IClient Client;

        /// <summary>The user key used to authenticate with the Pastebin API.</summary>
        private readonly string UserKey;

        /// <summary>The developer key used to authenticate with the Pastebin API.</summary>
        private readonly string DevKey;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="baseUrl">The base URL for the Pastebin API.</param>
        /// <param name="userAgent">The user agent for the API client.</param>
        /// <param name="userKey">The user key used to authenticate with the Pastebin API.</param>
        /// <param name="devKey">The developer key used to authenticate with the Pastebin API.</param>
        public PastebinClient(string baseUrl, string userAgent, string userKey, string devKey)
        {
            this.Client = new FluentClient(baseUrl).SetUserAgent(userAgent);
            this.UserKey = userKey;
            this.DevKey = devKey;
        }

        /// <summary>Fetch a saved paste.</summary>
        /// <param name="id">The paste ID.</param>
        public async Task<PasteInfo> GetAsync(string id)
        {
            try
            {
                // get from API
                string content = await this.Client
                    .GetAsync($"raw/{id}")
                    .AsString();

                // handle Pastebin errors
                if (string.IsNullOrWhiteSpace(content))
                    return new PasteInfo { Error = "Received an empty response from Pastebin." };
                if (content.StartsWith("<!DOCTYPE"))
                    return new PasteInfo { Error = $"Received a captcha challenge from Pastebin. Please visit https://pastebin.com/{id} in a new window to solve it." };
                return new PasteInfo { Success = true, Content = content };
            }
            catch (ApiException ex) when (ex.Status == HttpStatusCode.NotFound)
            {
                return new PasteInfo { Error = "There's no log with that ID." };
            }
            catch (Exception ex)
            {
                return new PasteInfo { Error = ex.ToString() };
            }
        }

        /// <summary>Save a paste to Pastebin.</summary>
        /// <param name="name">The paste name.</param>
        /// <param name="content">The paste content.</param>
        public async Task<SavePasteResult> PostAsync(string name, string content)
        {
            try
            {
                // validate
                if (string.IsNullOrWhiteSpace(content))
                    return new SavePasteResult { Error = "The log content can't be empty." };

                // post to API
                string response = await this.Client
                    .PostAsync("api/api_post.php")
                    .WithBody(p => p.FormUrlEncoded(new
                    {
                        api_option = "paste",
                        api_user_key = this.UserKey,
                        api_dev_key = this.DevKey,
                        api_paste_private = 1, // unlisted
                        api_paste_name = name,
                        api_paste_expire_date = "N", // never expire
                        api_paste_code = content
                    }))
                    .AsString();

                // handle Pastebin errors
                if (string.IsNullOrWhiteSpace(response))
                    return new SavePasteResult { Error = "Received an empty response from Pastebin." };
                if (response.StartsWith("Bad API request"))
                    return new SavePasteResult { Error = response };
                if (!response.Contains("/"))
                    return new SavePasteResult { Error = $"Received an unknown response: {response}" };

                // return paste ID
                string pastebinID = response.Split("/").Last();
                return new SavePasteResult { Success = true, ID = pastebinID };
            }
            catch (Exception ex)
            {
                return new SavePasteResult { Success = false, Error = ex.ToString() };
            }
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            this.Client.Dispose();
        }
    }
}
