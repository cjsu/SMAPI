using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using StardewModdingAPI.Web.Framework;
using StardewModdingAPI.Web.Framework.Clients.Pastebin;
using StardewModdingAPI.Web.Framework.Compression;
using StardewModdingAPI.Web.Framework.ConfigModels;
using StardewModdingAPI.Web.ViewModels.JsonValidator;

namespace StardewModdingAPI.Web.Controllers
{
    /// <summary>Provides a web UI for validating JSON schemas.</summary>
    internal class JsonValidatorController : Controller
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

        /// <summary>The section URL for the schema validator.</summary>
        private string SectionUrl => this.Config.JsonValidatorUrl;

        /// <summary>The supported JSON schemas (names indexed by ID).</summary>
        private readonly IDictionary<string, string> SchemaFormats = new Dictionary<string, string>
        {
            ["none"] = "None",
            ["manifest"] = "Manifest"
        };

        /// <summary>The schema ID to use if none was specified.</summary>
        private string DefaultSchemaID = "manifest";


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
        public JsonValidatorController(IOptions<SiteConfig> siteConfig, IPastebinClient pastebin, IGzipHelper gzipHelper)
        {
            this.Config = siteConfig.Value;
            this.Pastebin = pastebin;
            this.GzipHelper = gzipHelper;
        }

        /***
        ** Web UI
        ***/
        /// <summary>Render the schema validator UI.</summary>
        /// <param name="schemaName">The schema name with which to validate the JSON.</param>
        /// <param name="id">The paste ID.</param>
        [HttpGet]
        [Route("json")]
        [Route("json/{schemaName}")]
        [Route("json/{schemaName}/{id}")]
        public async Task<ViewResult> Index(string schemaName = null, string id = null)
        {
            schemaName = this.NormaliseSchemaName(schemaName);

            var result = new JsonValidatorModel(this.SectionUrl, id, schemaName, this.SchemaFormats);
            if (string.IsNullOrWhiteSpace(id))
                return this.View("Index", result);

            // fetch raw JSON
            PasteInfo paste = await this.GetAsync(id);
            if (string.IsNullOrWhiteSpace(paste.Content))
                return this.View("Index", result.SetUploadError("The JSON file seems to be empty."));
            result.SetContent(paste.Content);

            // parse JSON
            JToken parsed;
            try
            {
                parsed = JToken.Parse(paste.Content);
            }
            catch (JsonReaderException ex)
            {
                return this.View("Index", result.AddErrors(new JsonValidatorErrorModel(ex.LineNumber, ex.Path, ex.Message)));
            }

            // format JSON
            result.SetContent(parsed.ToString(Formatting.Indented));

            // skip if no schema selected
            if (schemaName == "none")
                return this.View("Index", result);

            // load schema
            JSchema schema;
            {
                FileInfo schemaFile = this.FindSchemaFile(schemaName);
                if (schemaFile == null)
                    return this.View("Index", result.SetParseError($"Invalid schema '{schemaName}'."));
                schema = JSchema.Parse(System.IO.File.ReadAllText(schemaFile.FullName));
            }

            // validate JSON
            parsed.IsValid(schema, out IList<ValidationError> rawErrors);
            var errors = rawErrors
                .Select(error => new JsonValidatorErrorModel(error.LineNumber, error.Path, this.GetFlattenedError(error)))
                .ToArray();
            return this.View("Index", result.AddErrors(errors));
        }

        /***
        ** JSON
        ***/
        /// <summary>Save raw JSON data.</summary>
        [HttpPost, AllowLargePosts]
        [Route("json")]
        public async Task<ActionResult> PostAsync(JsonValidatorRequestModel request)
        {
            if (request == null)
                return this.View("Index", new JsonValidatorModel(this.SectionUrl, null, null, this.SchemaFormats).SetUploadError("The request seems to be invalid."));

            // normalise schema name
            string schemaName = this.NormaliseSchemaName(request.SchemaName);

            // get raw log text
            string input = request.Content;
            if (string.IsNullOrWhiteSpace(input))
                return this.View("Index", new JsonValidatorModel(this.SectionUrl, null, schemaName, this.SchemaFormats).SetUploadError("The JSON file seems to be empty."));

            // upload log
            input = this.GzipHelper.CompressString(input);
            SavePasteResult result = await this.Pastebin.PostAsync($"JSON validator {DateTime.UtcNow:s}", input);

            // handle errors
            if (!result.Success)
                return this.View("Index", new JsonValidatorModel(this.SectionUrl, result.ID, schemaName, this.SchemaFormats).SetUploadError($"Pastebin error: {result.Error ?? "unknown error"}"));

            // redirect to view
            UriBuilder uri = new UriBuilder(new Uri(this.SectionUrl));
            uri.Path = $"{uri.Path.TrimEnd('/')}/{schemaName}/{result.ID}";
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

        /// <summary>Get a flattened, human-readable message representing a schema validation error.</summary>
        /// <param name="error">The error to represent.</param>
        /// <param name="indent">The indentation level to apply for inner errors.</param>
        private string GetFlattenedError(ValidationError error, int indent = 0)
        {
            // get friendly representation of main error
            string message = error.Message;
            switch (error.ErrorType)
            {
                case ErrorType.Enum:
                    message = $"Invalid value. Found '{error.Value}', but expected one of '{string.Join("', '", error.Schema.Enum)}'.";
                    break;
            }

            // add inner errors
            foreach (ValidationError childError in error.ChildErrors)
                message += "\n" + "".PadLeft(indent * 2, ' ') + $"==> {childError.Path}: " + this.GetFlattenedError(childError, indent + 1);
            return message;
        }

        /// <summary>Get a normalised schema name, or the <see cref="DefaultSchemaID"/> if blank.</summary>
        /// <param name="schemaName">The raw schema name to normalise.</param>
        private string NormaliseSchemaName(string schemaName)
        {
            schemaName = schemaName?.Trim().ToLower();
            return !string.IsNullOrWhiteSpace(schemaName)
                ? schemaName
                : this.DefaultSchemaID;
        }

        /// <summary>Get the schema file given its unique ID.</summary>
        /// <param name="id">The schema ID.</param>
        private FileInfo FindSchemaFile(string id)
        {
            // normalise ID
            id = id?.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(id))
                return null;

            // get matching file
            DirectoryInfo schemaDir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "schemas"));
            foreach (FileInfo file in schemaDir.EnumerateFiles("*.json"))
            {
                if (file.Name.Equals($"{id}.json"))
                    return file;
            }

            return null;
        }
    }
}
