using System;
using System.IO;

namespace StardewModdingAPI
{
    public class Mod
    {
        /// <summary>
        /// The mod's manifest
        /// </summary>
        public Manifest Manifest { get; internal set; }

        /// <summary>
        /// Where the mod is located on the disk.
        /// </summary>
        public string PathOnDisk { get; internal set; }

        /// <summary>
        /// A basic path to store your mod's config at.
        /// </summary>
        public string BaseConfigPath => PathOnDisk + "\\config.json";

        /// <summary>
        /// A basic path to where per-save configs are stored
        /// </summary>
        public string PerSaveConfigFolder => GetPerSaveConfigFolder();

        /// <summary>
        /// A basic path to store your mod's config at, dependent on the current save.
        /// The Manifest must allow for per-save configs. This is to keep from having an
        /// empty directory in every mod folder.
        /// </summary>
        public string PerSaveConfigPath => Constants.CurrentSavePathExists ? Path.Combine(PerSaveConfigFolder, Constants.SaveFolderName + ".json") : "";

        /// <summary>
        /// A basic method that is the entry-point of your mod. It will always be called once when the mod loads.
        /// </summary>
        public virtual void Entry(params object[] objects)
        {

        }

        private string GetPerSaveConfigFolder()
        {
            if (Manifest.PerSaveConfigs)
            {
                return Path.Combine(PathOnDisk, "psconfigs");
            }
            Log.Error("The mod [{0}] is not configured to use per-save configs.", Manifest.Name);
            return "";
        }
    }
}
