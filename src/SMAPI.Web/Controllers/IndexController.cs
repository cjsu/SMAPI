using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Toolkit;
using StardewModdingAPI.Web.Framework.Clients.GitHub;
using StardewModdingAPI.Web.Framework.ConfigModels;
using StardewModdingAPI.Web.ViewModels;

namespace StardewModdingAPI.Web.Controllers
{
    /// <summary>Provides an info/download page about SMAPI.</summary>
    [Route("")]
    [Route("install")]
    internal class IndexController : Controller
    {
        /*********
        ** Fields
        *********/
        /// <summary>The site config settings.</summary>
        private readonly SiteConfig SiteConfig;

        /// <summary>The cache in which to store release data.</summary>
        private readonly IMemoryCache Cache;

        /// <summary>The GitHub API client.</summary>
        private readonly IGitHubClient GitHub;

        /// <summary>The cache time for release info.</summary>
        private readonly TimeSpan CacheTime = TimeSpan.FromMinutes(10);

        /// <summary>The GitHub repository name to check for update.</summary>
        private readonly string RepositoryName = "Pathoschild/SMAPI";


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="cache">The cache in which to store release data.</param>
        /// <param name="github">The GitHub API client.</param>
        /// <param name="siteConfig">The context config settings.</param>
        public IndexController(IMemoryCache cache, IGitHubClient github, IOptions<SiteConfig> siteConfig)
        {
            this.Cache = cache;
            this.GitHub = github;
            this.SiteConfig = siteConfig.Value;
        }

        /// <summary>Display the index page.</summary>
        [HttpGet]
        public async Task<ViewResult> Index()
        {
            // choose versions
            ReleaseVersion[] versions = await this.GetReleaseVersionsAsync();
            ReleaseVersion stableVersion = versions.LastOrDefault(version => !version.IsBeta && !version.IsForDevs);
            ReleaseVersion stableVersionForDevs = versions.LastOrDefault(version => !version.IsBeta && version.IsForDevs);
            ReleaseVersion betaVersion = versions.LastOrDefault(version => version.IsBeta && !version.IsForDevs);
            ReleaseVersion betaVersionForDevs = versions.LastOrDefault(version => version.IsBeta && version.IsForDevs);

            // render view
            IndexVersionModel stableVersionModel = stableVersion != null
                ? new IndexVersionModel(stableVersion.Version.ToString(), stableVersion.Release.Body, stableVersion.Asset.DownloadUrl, stableVersionForDevs?.Asset.DownloadUrl)
                : new IndexVersionModel("unknown", "", "https://github.com/Pathoschild/SMAPI/releases", null); // just in case something goes wrong)
            IndexVersionModel betaVersionModel = betaVersion != null && this.SiteConfig.BetaEnabled
                ? new IndexVersionModel(betaVersion.Version.ToString(), betaVersion.Release.Body, betaVersion.Asset.DownloadUrl, betaVersionForDevs?.Asset.DownloadUrl)
                : null;

            // render view
            var model = new IndexModel(stableVersionModel, betaVersionModel, this.SiteConfig.BetaBlurb);
            return this.View(model);
        }

        /// <summary>Display the index page.</summary>
        [HttpGet("/privacy")]
        public ViewResult Privacy()
        {
            return this.View();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a sorted, parsed list of SMAPI downloads for the latest releases.</summary>
        private async Task<ReleaseVersion[]> GetReleaseVersionsAsync()
        {
            return await this.Cache.GetOrCreateAsync("available-versions", async entry =>
            {
                entry.AbsoluteExpiration = DateTimeOffset.UtcNow.Add(this.CacheTime);

                // get latest release (whether preview or stable)
                GitRelease stableRelease = await this.GitHub.GetLatestReleaseAsync(this.RepositoryName, includePrerelease: true);

                // split stable/prerelease if applicable
                GitRelease betaRelease = null;
                if (stableRelease.IsPrerelease)
                {
                    GitRelease result = await this.GitHub.GetLatestReleaseAsync(this.RepositoryName, includePrerelease: false);
                    if (result != null)
                    {
                        betaRelease = stableRelease;
                        stableRelease = result;
                    }
                }

                // strip 'noinclude' blocks from release descriptions
                foreach (GitRelease release in new[] { stableRelease, betaRelease })
                {
                    if (release == null)
                        continue;

                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(release.Body);
                    foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//*[@class='noinclude']")?.ToArray() ?? new HtmlNode[0])
                        node.Remove();
                    release.Body = doc.DocumentNode.InnerHtml.Trim();
                }

                // get versions
                ReleaseVersion[] stableVersions = this.ParseReleaseVersions(stableRelease).ToArray();
                ReleaseVersion[] betaVersions = this.ParseReleaseVersions(betaRelease).ToArray();
                return stableVersions
                    .Concat(betaVersions)
                    .OrderBy(p => p.Version)
                    .ToArray();
            });
        }

        /// <summary>Get a parsed list of SMAPI downloads for a release.</summary>
        /// <param name="release">The GitHub release.</param>
        private IEnumerable<ReleaseVersion> ParseReleaseVersions(GitRelease release)
        {
            if (release?.Assets == null)
                yield break;

            foreach (GitAsset asset in release.Assets)
            {
                if (asset.FileName.StartsWith("Z_"))
                    continue;

                Match match = Regex.Match(asset.FileName, @"SMAPI-(?<version>[\d\.]+(?:-.+)?)-installer(?<forDevs>-for-developers)?.zip");
                if (!match.Success || !SemanticVersion.TryParse(match.Groups["version"].Value, out ISemanticVersion version))
                    continue;
                bool isBeta = version.IsPrerelease();
                bool isForDevs = match.Groups["forDevs"].Success;

                yield return new ReleaseVersion(release, asset, version, isBeta, isForDevs);
            }
        }

        /// <summary>A parsed release download.</summary>
        private class ReleaseVersion
        {
            /*********
            ** Accessors
            *********/
            /// <summary>The underlying GitHub release.</summary>
            public GitRelease Release { get; }

            /// <summary>The underlying download asset.</summary>
            public GitAsset Asset { get; }

            /// <summary>The SMAPI version.</summary>
            public ISemanticVersion Version { get; }

            /// <summary>Whether this is a beta download.</summary>
            public bool IsBeta { get; }

            /// <summary>Whether this is a 'for developers' download.</summary>
            public bool IsForDevs { get; }


            /*********
            ** Public methods
            *********/
            /// <summary>Construct an instance.</summary>
            /// <param name="release">The underlying GitHub release.</param>
            /// <param name="asset">The underlying download asset.</param>
            /// <param name="version">The SMAPI version.</param>
            /// <param name="isBeta">Whether this is a beta download.</param>
            /// <param name="isForDevs">Whether this is a 'for developers' download.</param>
            public ReleaseVersion(GitRelease release, GitAsset asset, ISemanticVersion version, bool isBeta, bool isForDevs)
            {
                this.Release = release;
                this.Asset = asset;
                this.Version = version;
                this.IsBeta = isBeta;
                this.IsForDevs = isForDevs;
            }
        }
    }
}
