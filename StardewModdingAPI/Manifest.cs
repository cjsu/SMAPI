using System;

namespace StardewModdingAPI
{
    public class Manifest : Config
    {
        /// <summary>
        /// The name of your mod.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// The name of the mod's authour.
        /// </summary>
        public virtual string Authour { get; set; }

        /// <summary>
        /// The version of the mod.
        /// </summary>
        public virtual Version Version { get; set; }

        /// <summary>
        /// A description of the mod.
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// The unique ID of the mod. It doesn't *need* to be anything.
        /// </summary>
        public virtual string UniqueID { get; set; }

        /// <summary>
        /// Whether or not the mod uses per-save-config files.
        /// </summary>
        public virtual bool PerSaveConfigs { get; set; }

        /// <summary>
        /// The name of the DLL in the directory that has the Entry() method.
        /// </summary>
        public virtual string EntryDll { get; set; }

        public override T GenerateDefaultConfig<T>()
        {
            Name = "";
            Authour = "";
            Version = new Version(0, 0, 0, "");
            Description = "";
            UniqueID = Guid.NewGuid().ToString();
            PerSaveConfigs = false;
            EntryDll = "";
            return this as T;
        }
    }
}
