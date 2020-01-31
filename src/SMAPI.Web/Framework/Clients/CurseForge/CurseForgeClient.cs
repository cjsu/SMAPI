using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pathoschild.Http.Client;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Web.Framework.Clients.CurseForge.ResponseModels;

namespace StardewModdingAPI.Web.Framework.Clients.CurseForge
{
    /// <summary>An HTTP client for fetching mod metadata from the CurseForge API.</summary>
    internal class CurseForgeClient : ICurseForgeClient
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying HTTP client.</summary>
        private readonly IClient Client;

        /// <summary>A regex pattern which matches a version number in a CurseForge mod file name.</summary>
        private readonly Regex VersionInNamePattern = new Regex(@"^(?:.+? | *)v?(\d+\.\d+(?:\.\d+)?(?:-.+?)?) *(?:\.(?:zip|rar|7z))?$", RegexOptions.Compiled);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="userAgent">The user agent for the API client.</param>
        /// <param name="apiUrl">The base URL for the CurseForge API.</param>
        public CurseForgeClient(string userAgent, string apiUrl)
        {
            this.Client = new FluentClient(apiUrl).SetUserAgent(userAgent);
        }

        /// <summary>Get metadata about a mod.</summary>
        /// <param name="id">The CurseForge mod ID.</param>
        /// <returns>Returns the mod info if found, else <c>null</c>.</returns>
        public async Task<CurseForgeMod> GetModAsync(long id)
        {
            // get raw data
            ModModel mod = await this.Client
                .GetAsync($"addon/{id}")
                .As<ModModel>();
            if (mod == null)
                return null;

            // get latest versions
            string invalidVersion = null;
            ISemanticVersion latest = null;
            foreach (ModFileModel file in mod.LatestFiles)
            {
                // extract version
                ISemanticVersion version;
                {
                    string raw = this.GetRawVersion(file);
                    if (raw == null)
                        continue;

                    if (!SemanticVersion.TryParse(raw, out version))
                    {
                        if (invalidVersion == null)
                            invalidVersion = raw;
                        continue;
                    }
                }

                // track latest version
                if (latest == null || version.IsNewerThan(latest))
                    latest = version;
            }

            // get error
            string error = null;
            if (latest == null && invalidVersion == null)
            {
                error = mod.LatestFiles.Any()
                    ? $"CurseForge mod {id} has no downloads which specify the version in a recognised format."
                    : $"CurseForge mod {id} has no downloads.";
            }

            // generate result
            return new CurseForgeMod
            {
                Name = mod.Name,
                LatestVersion = latest?.ToString() ?? invalidVersion,
                Url = mod.WebsiteUrl,
                Error = error
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
        /// <summary>Get a raw version string for a mod file, if available.</summary>
        /// <param name="file">The file whose version to get.</param>
        private string GetRawVersion(ModFileModel file)
        {
            Match match = this.VersionInNamePattern.Match(file.DisplayName);
            if (!match.Success)
                match = this.VersionInNamePattern.Match(file.FileName);

            return match.Success
                ? match.Groups[1].Value
                : null;
        }
    }
}
