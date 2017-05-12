using System.Collections.Generic;

namespace StardewModdingAPI
{
    /// <summary>A manifest which describes a mod for SMAPI.</summary>
    public interface IManifest
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod name.</summary>
        string Name { get; }

        /// <summary>A brief description of the mod.</summary>
        string Description { get; }

        /// <summary>The mod author's name.</summary>
        string Author { get; }

        /// <summary>The mod version.</summary>
        ISemanticVersion Version { get; }

        /// <summary>The minimum SMAPI version required by this mod, if any.</summary>
        string MinimumApiVersion { get; }

        /// <summary>The unique mod ID.</summary>
        string UniqueID { get; }

        /// <summary>The name of the DLL in the directory that has the <see cref="Mod.Entry"/> method.</summary>
        string EntryDll { get; }

        /// <summary>The other mods that must be loaded before this mod.</summary>
        IManifestDependency[] Dependencies { get; }

        /// <summary>Any manifest fields which didn't match a valid field.</summary>
        IDictionary<string, object> ExtraFields { get; }
    }
}