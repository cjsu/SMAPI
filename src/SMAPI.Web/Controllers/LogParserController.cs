using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.Clients.Pastebin;
using StardewModdingAPI.Web.Framework.Compression;
using StardewModdingAPI.Web.Framework.ConfigModels;
using StardewModdingAPI.Web.Framework.LogParsing;
using StardewModdingAPI.Web.Framework.LogParsing.Models;
using StardewModdingAPI.Web.ViewModels;

namespace StardewModdingAPI.Web.Controllers
{
    /// <summary>Provides a web UI and API for parsing SMAPI log files.</summary>
    internal class LogParserController : Controller
    {
        /*********
        ** Fields
        *********/
        /// <summary>The site config settings.</summary>
        private readonly SiteConfig Config;

        /// <summary>The underlying Pastebin client.</summary>
        private readonly IPastebinClient Pastebin;

        /// <summary>The underlying text compression helper.</summary>
        private readonly IGzipHelper GzipHelper;


        /*********
        ** Public methods
        *********/
        /***
        ** Constructor
        ***/
        /// <summary>Construct an instance.</summary>
        /// <param name="siteConfig">The context config settings.</param>
        /// <param name="pastebin">The Pastebin API client.</param>
        /// <param name="gzipHelper">The underlying text compression helper.</param>
        public LogParserController(IOptions<SiteConfig> siteConfig, IPastebinClient pastebin, IGzipHelper gzipHelper)
        {
            this.Config = siteConfig.Value;
            this.Pastebin = pastebin;
            this.GzipHelper = gzipHelper;
        }

        /***
        ** Web UI
        ***/
        /// <summary>Render the log parser UI.</summary>
        /// <param name="id">The paste ID.</param>
        /// <param name="raw">Whether to display the raw unparsed log.</param>
        [HttpGet]
        [Route("log")]
        [Route("log/{id}")]
        public async Task<ViewResult> Index(string id = null, bool raw = false)
        {
            // fresh page
            if (string.IsNullOrWhiteSpace(id))
                return this.View("Index", new LogParserModel(this.Config.LogParserUrl, id));

            // log page
            PasteInfo paste = await this.GetAsync(id);
            ParsedLog log = paste.Success
                ? new LogParser().Parse(paste.Content)
                : new ParsedLog { IsValid = false, Error = "Pastebin error: " + paste.Error };
            return this.View("Index", new LogParserModel(this.Config.LogParserUrl, id, log, raw));
        }

        /***
        ** JSON
        ***/
        /// <summary>Save raw log data.</summary>
        [HttpPost, AllowLargePosts]
        [Route("log")]
        public async Task<ActionResult> PostAsync()
        {
            // get raw log text
            string input = this.Request.Form["input"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(input))
                return this.View("Index", new LogParserModel(this.Config.LogParserUrl, null) { UploadError = "The log file seems to be empty." });

            // upload log
            input = this.GzipHelper.CompressString(input);
            SavePasteResult result = await this.Pastebin.PostAsync(input);

            // handle errors
            if (!result.Success)
                return this.View("Index", new LogParserModel(this.Config.LogParserUrl, result.ID) { UploadError = $"Pastebin error: {result.Error ?? "unknown error"}" });

            // redirect to view
            UriBuilder uri = new UriBuilder(new Uri(this.Config.LogParserUrl));
            uri.Path = uri.Path.TrimEnd('/') + '/' + result.ID;
            return this.Redirect(uri.Uri.ToString());
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Fetch raw text from Pastebin.</summary>
        /// <param name="id">The Pastebin paste ID.</param>
        private async Task<PasteInfo> GetAsync(string id)
        {
            PasteInfo response = await this.Pastebin.GetAsync(id);
            response.Content = this.GzipHelper.DecompressString(response.Content);
            return response;
        }


    }
}
