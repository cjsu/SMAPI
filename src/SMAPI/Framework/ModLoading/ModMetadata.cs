using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>Metadata for a mod.</summary>
    internal class ModMetadata : IModMetadata
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod's display name.</summary>
        public string DisplayName { get; }

        /// <summary>The mod's full directory path.</summary>
        public string DirectoryPath { get; }

        /// <summary>The <see cref="IModMetadata.DirectoryPath"/> relative to the game's Mods folder.</summary>
        public string RelativeDirectoryPath { get; }

        /// <summary>The mod manifest.</summary>
        public IManifest Manifest { get; }

        /// <summary>Metadata about the mod from SMAPI's internal data (if any).</summary>
        public ModDataRecordVersionedFields DataRecord { get; }

        /// <summary>The metadata resolution status.</summary>
        public ModMetadataStatus Status { get; private set; }

        /// <summary>Indicates non-error issues with the mod.</summary>
        public ModWarning Warnings { get; private set; }

        /// <summary>The reason the metadata is invalid, if any.</summary>
        public string Error { get; private set; }

        /// <summary>Whether the mod folder should be ignored. This is <c>true</c> if it was found within a folder whose name starts with a dot.</summary>
        public bool IsIgnored { get; }

        /// <summary>The mod instance (if loaded and <see cref="IsContentPack"/> is false).</summary>
        public IMod Mod { get; private set; }

        /// <summary>The content pack instance (if loaded and <see cref="IsContentPack"/> is true).</summary>
        public IContentPack ContentPack { get; private set; }

        /// <summary>Writes messages to the console and log file as this mod.</summary>
        public IMonitor Monitor { get; private set; }

        /// <summary>The mod-provided API (if any).</summary>
        public object Api { get; private set; }

        /// <summary>The update-check metadata for this mod (if any).</summary>
        public ModEntryModel UpdateCheckData { get; private set; }

        /// <summary>Whether the mod is a content pack.</summary>
        public bool IsContentPack => this.Manifest?.ContentPackFor != null;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="displayName">The mod's display name.</param>
        /// <param name="directoryPath">The mod's full directory path.</param>
        /// <param name="relativeDirectoryPath">The <paramref name="directoryPath"/> relative to the game's Mods folder.</param>
        /// <param name="manifest">The mod manifest.</param>
        /// <param name="dataRecord">Metadata about the mod from SMAPI's internal data (if any).</param>
        /// <param name="isIgnored">Whether the mod folder should be ignored. This should be <c>true</c> if it was found within a folder whose name starts with a dot.</param>
        public ModMetadata(string displayName, string directoryPath, string relativeDirectoryPath, IManifest manifest, ModDataRecordVersionedFields dataRecord, bool isIgnored)
        {
            this.DisplayName = displayName;
            this.DirectoryPath = directoryPath;
            this.RelativeDirectoryPath = relativeDirectoryPath;
            this.Manifest = manifest;
            this.DataRecord = dataRecord;
            this.IsIgnored = isIgnored;
        }

        /// <summary>Set the mod status.</summary>
        /// <param name="status">The metadata resolution status.</param>
        /// <param name="error">The reason the metadata is invalid, if any.</param>
        /// <returns>Return the instance for chaining.</returns>
        public IModMetadata SetStatus(ModMetadataStatus status, string error = null)
        {
            this.Status = status;
            this.Error = error;
            return this;
        }

        /// <summary>Set a warning flag for the mod.</summary>
        /// <param name="warning">The warning to set.</param>
        public IModMetadata SetWarning(ModWarning warning)
        {
            this.Warnings |= warning;
            return this;
        }

        /// <summary>Set the mod instance.</summary>
        /// <param name="mod">The mod instance to set.</param>
        public IModMetadata SetMod(IMod mod)
        {
            if (this.ContentPack != null)
                throw new InvalidOperationException("A mod can't be both an assembly mod and content pack.");

            this.Mod = mod;
            this.Monitor = mod.Monitor;
            return this;
        }

        /// <summary>Set the mod instance.</summary>
        /// <param name="contentPack">The contentPack instance to set.</param>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        public IModMetadata SetMod(IContentPack contentPack, IMonitor monitor)
        {
            if (this.Mod != null)
                throw new InvalidOperationException("A mod can't be both an assembly mod and content pack.");

            this.ContentPack = contentPack;
            this.Monitor = monitor;
            return this;
        }

        /// <summary>Set the mod-provided API instance.</summary>
        /// <param name="api">The mod-provided API.</param>
        public IModMetadata SetApi(object api)
        {
            this.Api = api;
            return this;
        }

        /// <summary>Set the update-check metadata for this mod.</summary>
        /// <param name="data">The update-check metadata.</param>
        public IModMetadata SetUpdateData(ModEntryModel data)
        {
            this.UpdateCheckData = data;
            return this;
        }

        /// <summary>Whether the mod manifest was loaded (regardless of whether the mod itself was loaded).</summary>
        public bool HasManifest()
        {
            return this.Manifest != null;
        }

        /// <summary>Whether the mod has an ID (regardless of whether the ID is valid or the mod itself was loaded).</summary>
        public bool HasID()
        {
            return
                this.HasManifest()
                && !string.IsNullOrWhiteSpace(this.Manifest.UniqueID);
        }

        /// <summary>Whether the mod has the given ID.</summary>
        /// <param name="id">The mod ID to check.</param>
        public bool HasID(string id)
        {
            return
                this.HasID()
                && string.Equals(this.Manifest.UniqueID.Trim(), id?.Trim(), StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>Get the defined update keys.</summary>
        /// <param name="validOnly">Only return valid update keys.</param>
        public IEnumerable<UpdateKey> GetUpdateKeys(bool validOnly = false)
        {
            foreach (string rawKey in this.Manifest?.UpdateKeys ?? new string[0])
            {
                UpdateKey updateKey = UpdateKey.Parse(rawKey);
                if (updateKey.LooksValid || !validOnly)
                    yield return updateKey;
            }
        }

        /// <summary>Whether the mod has at least one valid update key set.</summary>
        public bool HasValidUpdateKeys()
        {
            return this.GetUpdateKeys(validOnly: true).Any();
        }

        /// <summary>Get whether the mod has a given warning and it hasn't been suppressed in the <see cref="DataRecord"/>.</summary>
        /// <param name="warning">The warning to check.</param>
        public bool HasUnsuppressWarning(ModWarning warning)
        {
            return
                this.Warnings.HasFlag(warning)
                && (this.DataRecord?.DataRecord == null || !this.DataRecord.DataRecord.SuppressWarnings.HasFlag(warning));
        }
    }
}
