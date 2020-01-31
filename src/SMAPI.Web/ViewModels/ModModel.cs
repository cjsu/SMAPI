using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Toolkit.Framework.Clients.Wiki;

namespace StardewModdingAPI.Web.ViewModels
{
    /// <summary>Metadata about a mod.</summary>
    public class ModModel
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod name.</summary>
        public string Name { get; set; }

        /// <summary>The mod's alternative names, if any.</summary>
        public string AlternateNames { get; set; }

        /// <summary>The mod author's name.</summary>
        public string Author { get; set; }

        /// <summary>The mod author's alternative names, if any.</summary>
        public string AlternateAuthors { get; set; }

        /// <summary>The URL to the mod's source code, if any.</summary>
        public string SourceUrl { get; set; }

        /// <summary>The compatibility status for the stable version of the game.</summary>
        public ModCompatibilityModel Compatibility { get; set; }

        /// <summary>The compatibility status for the beta version of the game.</summary>
        public ModCompatibilityModel BetaCompatibility { get; set; }

        /// <summary>Links to the available mod pages.</summary>
        public ModLinkModel[] ModPages { get; set; }

        /// <summary>The human-readable warnings for players about this mod.</summary>
        public string[] Warnings { get; set; }

        /// <summary>Extra metadata links (usually for open pull requests).</summary>
        public Tuple<Uri, string>[] MetadataLinks { get; set; }

        /// <summary>Special notes intended for developers who maintain unofficial updates or submit pull requests. </summary>
        public string DevNote { get; set; }

        /// <summary>A unique identifier for the mod that can be used in an anchor URL.</summary>
        public string Slug { get; set; }

        /// <summary>The sites where the mod can be downloaded.</summary>
        public string[] ModPageSites => this.ModPages.Select(p => p.Text).ToArray();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="entry">The mod metadata.</param>
        public ModModel(WikiModEntry entry)
        {
            // basic info
            this.Name = entry.Name.FirstOrDefault();
            this.AlternateNames = string.Join(", ", entry.Name.Skip(1).ToArray());
            this.Author = entry.Author.FirstOrDefault();
            this.AlternateAuthors = string.Join(", ", entry.Author.Skip(1).ToArray());
            this.SourceUrl = this.GetSourceUrl(entry);
            this.Compatibility = new ModCompatibilityModel(entry.Compatibility);
            this.BetaCompatibility = entry.BetaCompatibility != null ? new ModCompatibilityModel(entry.BetaCompatibility) : null;
            this.ModPages = this.GetModPageUrls(entry).ToArray();
            this.Warnings = entry.Warnings;
            this.MetadataLinks = entry.MetadataLinks;
            this.DevNote = entry.DevNote;
            this.Slug = entry.Anchor;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the web URL for the mod's source code repository, if any.</summary>
        /// <param name="entry">The mod metadata.</param>
        private string GetSourceUrl(WikiModEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.GitHubRepo))
                return $"https://github.com/{entry.GitHubRepo}";
            if (!string.IsNullOrWhiteSpace(entry.CustomSourceUrl))
                return entry.CustomSourceUrl;
            return null;
        }

        /// <summary>Get the web URLs for the mod pages, if any.</summary>
        /// <param name="entry">The mod metadata.</param>
        private IEnumerable<ModLinkModel> GetModPageUrls(WikiModEntry entry)
        {
            bool anyFound = false;

            // normal mod pages
            if (entry.NexusID.HasValue)
            {
                anyFound = true;
                yield return new ModLinkModel($"https://www.nexusmods.com/stardewvalley/mods/{entry.NexusID}", "Nexus");
            }
            if (entry.ModDropID.HasValue)
            {
                anyFound = true;
                yield return new ModLinkModel($"https://www.moddrop.com/sdv/mod/{entry.ModDropID}", "ModDrop");
            }
            if (!string.IsNullOrWhiteSpace(entry.CurseForgeKey))
            {
                anyFound = true;
                yield return new ModLinkModel($"https://www.curseforge.com/stardewvalley/mods/{entry.CurseForgeKey}", "CurseForge");
            }
            if (entry.ChucklefishID.HasValue)
            {
                anyFound = true;
                yield return new ModLinkModel($"https://community.playstarbound.com/resources/{entry.ChucklefishID}", "Chucklefish");
            }

            // fallback
            if (!anyFound && !string.IsNullOrWhiteSpace(entry.CustomUrl))
            {
                anyFound = true;
                yield return new ModLinkModel(entry.CustomUrl, "custom");
            }
            if (!anyFound && !string.IsNullOrWhiteSpace(entry.GitHubRepo))
                yield return new ModLinkModel($"https://github.com/{entry.GitHubRepo}/releases", "GitHub");
        }
    }
}
