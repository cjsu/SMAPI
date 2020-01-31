namespace StardewModdingAPI.Web.Framework.Storage
{
    /// <summary>The result of an attempt to upload a file.</summary>
    internal class UploadResult
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Whether the file upload succeeded.</summary>
        public bool Succeeded { get; }

        /// <summary>The file ID, if applicable.</summary>
        public string ID { get; }

        /// <summary>The upload error, if any.</summary>
        public string UploadError { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="succeeded">Whether the file upload succeeded.</param>
        /// <param name="id">The file ID, if applicable.</param>
        /// <param name="uploadError">The upload error, if any.</param>
        public UploadResult(bool succeeded, string id, string uploadError)
        {
            this.Succeeded = succeeded;
            this.ID = id;
            this.UploadError = uploadError;
        }
    }
}
