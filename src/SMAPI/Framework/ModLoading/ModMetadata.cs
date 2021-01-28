using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI.Framework.ModHelpers;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>Metadata for a mod.</summary>
    internal class ModMetadata : IModMetadata
    {
        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string DisplayName { get; }

        /// <inheritdoc />
        public string RootPath { get; }

        /// <inheritdoc />
        public string DirectoryPath { get; }

        /// <inheritdoc />
        public string RelativeDirectoryPath { get; }

        /// <inheritdoc />
        public IManifest Manifest { get; }

        /// <inheritdoc />
        public ModDataRecordVersionedFields DataRecord { get; }

        /// <inheritdoc />
        public ModMetadataStatus Status { get; private set; }

        /// <inheritdoc />
        public ModFailReason? FailReason { get; private set; }

        /// <inheritdoc />
        public ModWarning Warnings { get; private set; }

        /// <inheritdoc />
        public string Error { get; private set; }

        /// <inheritdoc />
        public string ErrorDetails { get; private set; }

        /// <inheritdoc />
        public bool IsIgnored { get; }

        /// <inheritdoc />
        public IMod Mod { get; private set; }

        /// <inheritdoc />
        public IContentPack ContentPack { get; private set; }

        /// <inheritdoc />
        public TranslationHelper Translations { get; private set; }

        /// <inheritdoc />
        public IMonitor Monitor { get; private set; }

        /// <inheritdoc />
        public object Api { get; private set; }

        /// <inheritdoc />
        public ModEntryModel UpdateCheckData { get; private set; }

        /// <inheritdoc />
        public bool IsContentPack => this.Manifest?.ContentPackFor != null;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="displayName">The mod's display name.</param>
        /// <param name="directoryPath">The mod's full directory path within the <paramref name="rootPath"/>.</param>
        /// <param name="rootPath">The root path containing mods.</param>
        /// <param name="manifest">The mod manifest.</param>
        /// <param name="dataRecord">Metadata about the mod from SMAPI's internal data (if any).</param>
        /// <param name="isIgnored">Whether the mod folder should be ignored. This should be <c>true</c> if it was found within a folder whose name starts with a dot.</param>
        public ModMetadata(string displayName, string directoryPath, string rootPath, IManifest manifest, ModDataRecordVersionedFields dataRecord, bool isIgnored)
        {
            this.DisplayName = displayName;
            this.DirectoryPath = directoryPath;
            this.RootPath = rootPath;
            this.RelativeDirectoryPath = PathUtilities.GetRelativePath(this.RootPath, this.DirectoryPath);
            this.Manifest = manifest;
            this.DataRecord = dataRecord;
            this.IsIgnored = isIgnored;
        }

        /// <inheritdoc />
        public IModMetadata SetStatusFound()
        {
            this.SetStatus(ModMetadataStatus.Found, ModFailReason.Incompatible, null);
            this.FailReason = null;
            return this;
        }

        /// <inheritdoc />
        public IModMetadata SetStatus(ModMetadataStatus status, ModFailReason reason, string error, string errorDetails = null)
        {
            this.Status = status;
            this.FailReason = reason;
            this.Error = error;
            this.ErrorDetails = errorDetails;
            return this;
        }

        /// <inheritdoc />
        public IModMetadata SetWarning(ModWarning warning)
        {
            this.Warnings |= warning;
            return this;
        }

        /// <inheritdoc />
        public IModMetadata SetMod(IMod mod, TranslationHelper translations)
        {
            if (this.ContentPack != null)
                throw new InvalidOperationException("A mod can't be both an assembly mod and content pack.");

            this.Mod = mod;
            this.Monitor = mod.Monitor;
            this.Translations = translations;
            return this;
        }

        /// <inheritdoc />
        public IModMetadata SetMod(IContentPack contentPack, IMonitor monitor, TranslationHelper translations)
        {
            if (this.Mod != null)
                throw new InvalidOperationException("A mod can't be both an assembly mod and content pack.");

            this.ContentPack = contentPack;
            this.Monitor = monitor;
            this.Translations = translations;
            return this;
        }

        /// <inheritdoc />
        public IModMetadata SetApi(object api)
        {
            this.Api = api;
            return this;
        }

        /// <inheritdoc />
        public IModMetadata SetUpdateData(ModEntryModel data)
        {
            this.UpdateCheckData = data;
            return this;
        }

        /// <inheritdoc />
        public bool HasManifest()
        {
            return this.Manifest != null;
        }

        /// <inheritdoc />
        public bool HasID()
        {
            return
                this.HasManifest()
                && !string.IsNullOrWhiteSpace(this.Manifest.UniqueID);
        }

        /// <inheritdoc />
        public bool HasID(string id)
        {
            return
                this.HasID()
                && string.Equals(this.Manifest.UniqueID.Trim(), id?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public IEnumerable<UpdateKey> GetUpdateKeys(bool validOnly = false)
        {
            foreach (string rawKey in this.Manifest?.UpdateKeys ?? new string[0])
            {
                UpdateKey updateKey = UpdateKey.Parse(rawKey);
                if (updateKey.LooksValid || !validOnly)
                    yield return updateKey;
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetRequiredModIds(bool includeOptional = false)
        {
            HashSet<string> required = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // yield dependencies
            if (this.Manifest?.Dependencies != null)
            {
                foreach (var entry in this.Manifest?.Dependencies)
                {
                    if ((entry.IsRequired || includeOptional) && required.Add(entry.UniqueID))
                        yield return entry.UniqueID;
                }
            }

            // yield content pack parent
            if (this.Manifest?.ContentPackFor?.UniqueID != null && required.Add(this.Manifest.ContentPackFor.UniqueID))
                yield return this.Manifest.ContentPackFor.UniqueID;
        }

        /// <inheritdoc />
        public bool HasValidUpdateKeys()
        {
            return this.GetUpdateKeys(validOnly: true).Any();
        }

        /// <inheritdoc />
        public bool HasUnsuppressedWarnings(params ModWarning[] warnings)
        {
            return warnings.Any(warning =>
                this.Warnings.HasFlag(warning)
                && (this.DataRecord?.DataRecord == null || !this.DataRecord.DataRecord.SuppressWarnings.HasFlag(warning))
            );
        }

        /// <inheritdoc />
        public string GetRelativePathWithRoot()
        {
            string rootFolderName = Path.GetFileName(this.RootPath) ?? "";
            return Path.Combine(rootFolderName, this.RelativeDirectoryPath);
        }
    }
}
