using System;

namespace StardewModdingAPI.Web.Framework.Storage
{
    /// <summary>The response for a get-file request.</summary>
    internal class StoredFileInfo
    {
        /// <summary>Whether the file was successfully fetched.</summary>
        public bool Success { get; set; }

        /// <summary>The fetched file content (if <see cref="Success"/> is <c>true</c>).</summary>
        public string Content { get; set; }

        /// <summary>When the file will no longer be available.</summary>
        public DateTime? Expiry { get; set; }

        /// <summary>The error message if saving succeeded, but a non-blocking issue was encountered.</summary>
        public string Warning { get; set; }

        /// <summary>The error message if saving failed.</summary>
        public string Error { get; set; }
    }
}
