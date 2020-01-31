namespace StardewModdingAPI.Web.Framework.Clients.Pastebin
{
    /// <summary>The response for a get-paste request.</summary>
    internal class PasteInfo
    {
        /// <summary>Whether the log was successfully fetched.</summary>
        public bool Success { get; set; }

        /// <summary>The fetched paste content (if <see cref="Success"/> is <c>true</c>).</summary>
        public string Content { get; set; }

        /// <summary>The error message if saving failed.</summary>
        public string Error { get; set; }
    }
}
