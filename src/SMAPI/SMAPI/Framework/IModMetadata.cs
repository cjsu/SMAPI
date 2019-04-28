using System.Collections.Generic;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Framework.UpdateData;

namespace StardewModdingAPI.Framework
{
    /// <summary>Metadata for a mod.</summary>
    internal interface IModMetadata : IModInfo
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod's display name.</summary>
        string DisplayName { get; }

        /// <summary>The mod's full directory path.</summary>
        string DirectoryPath { get; }

        /// <summary>The <see cref="DirectoryPath"/> relative to the game's Mods folder.</summary>
        string RelativeDirectoryPath { get; }

        /// <summary>Metadata about the mod from SMAPI's internal data (if any).</summary>
        ModDataRecordVersionedFields DataRecord { get; }

        /// <summary>The metadata resolution status.</summary>
        ModMetadataStatus Status { get; }

        /// <summary>Indicates non-error issues with the mod.</summary>
        ModWarning Warnings { get; }

        /// <summary>The reason the metadata is invalid, if any.</summary>
        string Error { get; }

        /// <summary>Whether the mod folder should be ignored. This is <c>true</c> if it was found within a folder whose name starts with a dot.</summary>
        bool IsIgnored { get; }

        /// <summary>The mod instance (if loaded and <see cref="IModInfo.IsContentPack"/> is false).</summary>
        IMod Mod { get; }

        /// <summary>The content pack instance (if loaded and <see cref="IModInfo.IsContentPack"/> is true).</summary>
        IContentPack ContentPack { get; }

        /// <summary>Writes messages to the console and log file as this mod.</summary>
        IMonitor Monitor { get; }

        /// <summary>The mod-provided API (if any).</summary>
        object Api { get; }

        /// <summary>The update-check metadata for this mod (if any).</summary>
        ModEntryModel UpdateCheckData { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Set the mod status.</summary>
        /// <param name="status">The metadata resolution status.</param>
        /// <param name="error">The reason the metadata is invalid, if any.</param>
        /// <returns>Return the instance for chaining.</returns>
        IModMetadata SetStatus(ModMetadataStatus status, string error = null);

        /// <summary>Set a warning flag for the mod.</summary>
        /// <param name="warning">The warning to set.</param>
        IModMetadata SetWarning(ModWarning warning);

        /// <summary>Set the mod instance.</summary>
        /// <param name="mod">The mod instance to set.</param>
        IModMetadata SetMod(IMod mod);

        /// <summary>Set the mod instance.</summary>
        /// <param name="contentPack">The contentPack instance to set.</param>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        IModMetadata SetMod(IContentPack contentPack, IMonitor monitor);

        /// <summary>Set the mod-provided API instance.</summary>
        /// <param name="api">The mod-provided API.</param>
        IModMetadata SetApi(object api);

        /// <summary>Set the update-check metadata for this mod.</summary>
        /// <param name="data">The update-check metadata.</param>
        IModMetadata SetUpdateData(ModEntryModel data);

        /// <summary>Whether the mod manifest was loaded (regardless of whether the mod itself was loaded).</summary>
        bool HasManifest();

        /// <summary>Whether the mod has an ID (regardless of whether the ID is valid or the mod itself was loaded).</summary>
        bool HasID();

        /// <summary>Whether the mod has the given ID.</summary>
        /// <param name="id">The mod ID to check.</param>
        bool HasID(string id);

        /// <summary>Get the defined update keys.</summary>
        /// <param name="validOnly">Only return valid update keys.</param>
        IEnumerable<UpdateKey> GetUpdateKeys(bool validOnly = true);

        /// <summary>Whether the mod has at least one valid update key set.</summary>
        bool HasValidUpdateKeys();

        /// <summary>Get whether the mod has a given warning and it hasn't been suppressed in the <see cref="DataRecord"/>.</summary>
        /// <param name="warning">The warning to check.</param>
        bool HasUnsuppressWarning(ModWarning warning);
    }
}
