using System;
using System.IO;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Models;

namespace StardewModdingAPI
{
    /// <summary>The base class for a mod.</summary>
    public class Mod : IMod, IDisposable
    {
        /*********
        ** Properties
        *********/
#if SMAPI_1_x
        /// <summary>Manages deprecation warnings.</summary>
        private static DeprecationManager DeprecationManager;


        /// <summary>The backing field for <see cref="Mod.PathOnDisk"/>.</summary>
        private string _pathOnDisk;
#endif


        /*********
        ** Accessors
        *********/
        /// <summary>Provides simplified APIs for writing mods.</summary>
        public IModHelper Helper { get; internal set; }

        /// <summary>Writes messages to the console and log file.</summary>
        public IMonitor Monitor { get; internal set; }

        /// <summary>The mod's manifest.</summary>
        public IManifest ModManifest { get; internal set; }

#if SMAPI_1_x
        /// <summary>The full path to the mod's directory on the disk.</summary>
        [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.DirectoryPath) + " instead")]
        public string PathOnDisk
        {
            get
            {
                Mod.DeprecationManager.Warn($"{nameof(Mod)}.{nameof(Mod.PathOnDisk)}", "1.0", DeprecationLevel.PendingRemoval);
                return this._pathOnDisk;
            }
            internal set { this._pathOnDisk = value; }
        }

        /// <summary>The full path to the mod's <c>config.json</c> file on the disk.</summary>
        [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.ReadConfig) + " instead")]
        public string BaseConfigPath
        {
            get
            {
                Mod.DeprecationManager.Warn($"{nameof(Mod)}.{nameof(Mod.BaseConfigPath)}", "1.0", DeprecationLevel.PendingRemoval);
                Mod.DeprecationManager.MarkWarned($"{nameof(Mod)}.{nameof(Mod.PathOnDisk)}", "1.0"); // avoid redundant warnings
                return Path.Combine(this.PathOnDisk, "config.json");
            }
        }

        /// <summary>The full path to the per-save configs folder (if <see cref="Manifest.PerSaveConfigs"/> is <c>true</c>).</summary>
        [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.ReadJsonFile) + " instead")]
        public string PerSaveConfigFolder => this.GetPerSaveConfigFolder();

        /// <summary>The full path to the per-save configuration file for the current save (if <see cref="Manifest.PerSaveConfigs"/> is <c>true</c>).</summary>
        [Obsolete("Use " + nameof(Mod.Helper) + "." + nameof(IModHelper.ReadJsonFile) + " instead")]
        public string PerSaveConfigPath
        {
            get
            {
                Mod.DeprecationManager.Warn($"{nameof(Mod)}.{nameof(Mod.PerSaveConfigPath)}", "1.0", DeprecationLevel.PendingRemoval);
                Mod.DeprecationManager.MarkWarned($"{nameof(Mod)}.{nameof(Mod.PerSaveConfigFolder)}", "1.0"); // avoid redundant warnings
                return Context.IsSaveLoaded ? Path.Combine(this.PerSaveConfigFolder, $"{Constants.SaveFolderName}.json") : "";
            }
        }
#endif


        /*********
        ** Public methods
        *********/
#if SMAPI_1_x
        /// <summary>Injects types required for backwards compatibility.</summary>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        internal static void Shim(DeprecationManager deprecationManager)
        {
            Mod.DeprecationManager = deprecationManager;
        }

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        [Obsolete("This overload is obsolete since SMAPI 1.0.")]
        public virtual void Entry(params object[] objects) { }
#endif

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public virtual void Entry(IModHelper helper) { }

        /// <summary>Release or reset unmanaged resources.</summary>
        public void Dispose()
        {
            (this.Helper as IDisposable)?.Dispose(); // deliberate do this outside overridable dispose method so mods don't accidentally suppress it
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }


        /*********
        ** Private methods
        *********/
#if SMAPI_1_x
        /// <summary>Get the full path to the per-save configuration file for the current save (if <see cref="Manifest.PerSaveConfigs"/> is <c>true</c>).</summary>
        [Obsolete]
        private string GetPerSaveConfigFolder()
        {
            Mod.DeprecationManager.Warn($"{nameof(Mod)}.{nameof(Mod.PerSaveConfigFolder)}", "1.0", DeprecationLevel.PendingRemoval);
            Mod.DeprecationManager.MarkWarned($"{nameof(Mod)}.{nameof(Mod.PathOnDisk)}", "1.0"); // avoid redundant warnings

            if (!((Manifest)this.ModManifest).PerSaveConfigs)
            {
                this.Monitor.Log("Tried to fetch the per-save config folder, but this mod isn't configured to use per-save config files.", LogLevel.Error);
                return "";
            }
            return Path.Combine(this.PathOnDisk, "psconfigs");
        }
#endif

        /// <summary>Release or reset unmanaged resources when the game exits. There's no guarantee this will be called on every exit.</summary>
        /// <param name="disposing">Whether the instance is being disposed explicitly rather than finalised. If this is false, the instance shouldn't dispose other objects since they may already be finalised.</param>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>Destruct the instance.</summary>
        ~Mod()
        {
            this.Dispose(false);
        }
    }
}
