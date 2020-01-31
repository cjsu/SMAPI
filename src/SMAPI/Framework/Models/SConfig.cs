using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Internal.ConsoleWriting;

namespace StardewModdingAPI.Framework.Models
{
    /// <summary>The SMAPI configuration settings.</summary>
    internal class SConfig
    {
        /********
        ** Fields
        ********/
        /// <summary>The default config values, for fields that should be logged if different.</summary>
        private static readonly IDictionary<string, object> DefaultValues = new Dictionary<string, object>
        {
            [nameof(CheckForUpdates)] = true,
            [nameof(ParanoidWarnings)] =
#if DEBUG
                true,
#else
                false,
#endif
            [nameof(UseBetaChannel)] = Constants.ApiVersion.IsPrerelease(),
            [nameof(GitHubProjectName)] = "Pathoschild/SMAPI",
            [nameof(WebApiBaseUrl)] = "https://smapi.io/api/",
            [nameof(VerboseLogging)] = false,
            [nameof(LogNetworkTraffic)] = false
        };

        /// <summary>The default values for <see cref="SuppressUpdateChecks"/>, to log changes if different.</summary>
        private static readonly HashSet<string> DefaultSuppressUpdateChecks = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
        {
            "SMAPI.ConsoleCommands",
            "SMAPI.SaveBackup"
        };


        /********
        ** Accessors
        ********/
        /// <summary>Whether to enable development features.</summary>
        public bool DeveloperMode { get; set; }

        /// <summary>Whether to check for newer versions of SMAPI and mods on startup.</summary>
        public bool CheckForUpdates { get; set; }

        /// <summary>Whether to add a section to the 'mod issues' list for mods which which directly use potentially sensitive .NET APIs like file or shell access.</summary>
        public bool ParanoidWarnings { get; set; } = (bool)SConfig.DefaultValues[nameof(SConfig.ParanoidWarnings)];

        /// <summary>Whether to show beta versions as valid updates.</summary>
        public bool UseBetaChannel { get; set; } = (bool)SConfig.DefaultValues[nameof(SConfig.UseBetaChannel)];

        /// <summary>SMAPI's GitHub project name, used to perform update checks.</summary>
        public string GitHubProjectName { get; set; }

        /// <summary>The base URL for SMAPI's web API, used to perform update checks.</summary>
        public string WebApiBaseUrl { get; set; }

        /// <summary>Whether SMAPI should log more information about the game context.</summary>
        public bool VerboseLogging { get; set; }

        /// <summary>Whether SMAPI should log network traffic. Best combined with <see cref="VerboseLogging"/>, which includes network metadata.</summary>
        public bool LogNetworkTraffic { get; set; }

        /// <summary>The colors to use for text written to the SMAPI console.</summary>
        public ColorSchemeConfig ConsoleColors { get; set; }

        /// <summary>The mod IDs SMAPI should ignore when performing update checks or validating update keys.</summary>
        public string[] SuppressUpdateChecks { get; set; }


        /********
        ** Public methods
        ********/
        /// <summary>Get the settings which have been customised by the player.</summary>
        public IDictionary<string, object> GetCustomSettings()
        {
            IDictionary<string, object> custom = new Dictionary<string, object>();

            foreach (var pair in SConfig.DefaultValues)
            {
                object value = typeof(SConfig).GetProperty(pair.Key)?.GetValue(this);
                if (!pair.Value.Equals(value))
                    custom[pair.Key] = value;
            }

            HashSet<string> curSuppressUpdateChecks = new HashSet<string>(this.SuppressUpdateChecks ?? new string[0], StringComparer.InvariantCultureIgnoreCase);
            if (SConfig.DefaultSuppressUpdateChecks.Count != curSuppressUpdateChecks.Count || SConfig.DefaultSuppressUpdateChecks.Any(p => !curSuppressUpdateChecks.Contains(p)))
                custom[nameof(this.SuppressUpdateChecks)] = "[" + string.Join(", ", this.SuppressUpdateChecks ?? new string[0]) + "]";

            return custom;
        }
    }
}
