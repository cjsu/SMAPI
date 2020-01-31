namespace StardewModdingAPI.Web.Framework.ModRepositories
{
    /// <summary>Generic metadata about a mod.</summary>
    internal class ModInfoModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod name.</summary>
        public string Name { get; set; }

        /// <summary>The mod's latest version.</summary>
        public string Version { get; set; }

        /// <summary>The mod's latest optional or prerelease version, if newer than <see cref="Version"/>.</summary>
        public string PreviewVersion { get; set; }

        /// <summary>The mod's web URL.</summary>
        public string Url { get; set; }

        /// <summary>The license URL, if available.</summary>
        public string LicenseUrl { get; set; }

        /// <summary>The license name, if available.</summary>
        public string LicenseName { get; set; }

        /// <summary>The mod availability status on the remote site.</summary>
        public RemoteModStatus Status { get; set; } = RemoteModStatus.Ok;

        /// <summary>The error message indicating why the mod is invalid (if applicable).</summary>
        public string Error { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an empty instance.</summary>
        public ModInfoModel() { }

        /// <summary>Construct an instance.</summary>
        /// <param name="name">The mod name.</param>
        /// <param name="version">The semantic version for the mod's latest release.</param>
        /// <param name="previewVersion">The semantic version for the mod's latest preview release, if available and different from <see cref="Version"/>.</param>
        /// <param name="url">The mod's web URL.</param>
        public ModInfoModel(string name, string version, string url, string previewVersion = null)
        {
            this
                .SetBasicInfo(name, url)
                .SetVersions(version, previewVersion);
        }

        /// <summary>Set the basic mod info.</summary>
        /// <param name="name">The mod name.</param>
        /// <param name="url">The mod's web URL.</param>
        public ModInfoModel SetBasicInfo(string name, string url)
        {
            this.Name = name;
            this.Url = url;

            return this;
        }

        /// <summary>Set the mod version info.</summary>
        /// <param name="version">The semantic version for the mod's latest release.</param>
        /// <param name="previewVersion">The semantic version for the mod's latest preview release, if available and different from <see cref="Version"/>.</param>
        public ModInfoModel SetVersions(string version, string previewVersion = null)
        {
            this.Version = version;
            this.PreviewVersion = previewVersion;

            return this;
        }

        /// <summary>Set the license info, if available.</summary>
        /// <param name="url">The license URL.</param>
        /// <param name="name">The license name.</param>
        public ModInfoModel SetLicense(string url, string name)
        {
            this.LicenseUrl = url;
            this.LicenseName = name;

            return this;
        }

        /// <summary>Set a mod error.</summary>
        /// <param name="status">The mod availability status on the remote site.</param>
        /// <param name="error">The error message indicating why the mod is invalid (if applicable).</param>
        public ModInfoModel SetError(RemoteModStatus status, string error)
        {
            this.Status = status;
            this.Error = error;

            return this;
        }
    }
}
