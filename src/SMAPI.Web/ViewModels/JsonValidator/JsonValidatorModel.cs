using System.Collections.Generic;
using System.Linq;

namespace StardewModdingAPI.Web.ViewModels.JsonValidator
{
    /// <summary>The view model for the JSON validator page.</summary>
    public class JsonValidatorModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The root URL for the log parser controller.</summary>
        public string SectionUrl { get; set; }

        /// <summary>The paste ID.</summary>
        public string PasteID { get; set; }

        /// <summary>The schema name with which the JSON was validated.</summary>
        public string SchemaName { get; set; }

        /// <summary>The supported JSON schemas (names indexed by ID).</summary>
        public readonly IDictionary<string, string> SchemaFormats;

        /// <summary>The validated content.</summary>
        public string Content { get; set; }

        /// <summary>The schema validation errors, if any.</summary>
        public JsonValidatorErrorModel[] Errors { get; set; } = new JsonValidatorErrorModel[0];

        /// <summary>An error which occurred while uploading the JSON to Pastebin.</summary>
        public string UploadError { get; set; }

        /// <summary>An error which occurred while parsing the JSON.</summary>
        public string ParseError { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public JsonValidatorModel() { }

        /// <summary>Construct an instance.</summary>
        /// <param name="sectionUrl">The root URL for the log parser controller.</param>
        /// <param name="pasteID">The paste ID.</param>
        /// <param name="schemaName">The schema name with which the JSON was validated.</param>
        /// <param name="schemaFormats">The supported JSON schemas (names indexed by ID).</param>
        public JsonValidatorModel(string sectionUrl, string pasteID, string schemaName, IDictionary<string, string> schemaFormats)
        {
            this.SectionUrl = sectionUrl;
            this.PasteID = pasteID;
            this.SchemaName = schemaName;
            this.SchemaFormats = schemaFormats;
        }

        /// <summary>Set the validated content.</summary>
        /// <param name="content">The validated content.</param>
        public JsonValidatorModel SetContent(string content)
        {
            this.Content = content;

            return this;
        }

        /// <summary>Set the error which occurred while uploading the log to Pastebin.</summary>
        /// <param name="error">The error message.</param>
        public JsonValidatorModel SetUploadError(string error)
        {
            this.UploadError = error;

            return this;
        }

        /// <summary>Set the error which occurred while parsing the JSON.</summary>
        /// <param name="error">The error message.</param>
        public JsonValidatorModel SetParseError(string error)
        {
            this.ParseError = error;

            return this;
        }

        /// <summary>Add validation errors to the response.</summary>
        /// <param name="errors">The schema validation errors.</param>
        public JsonValidatorModel AddErrors(params JsonValidatorErrorModel[] errors)
        {
            this.Errors = this.Errors.Concat(errors).ToArray();

            return this;
        }
    }
}
