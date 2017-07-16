#if SMAPI_1_x
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Framework;

namespace StardewModdingAPI
{
    /// <summary>A dynamic configuration class for a mod.</summary>
    [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
    public abstract class Config
    {
        /*********
        ** Properties
        *********/
        /// <summary>Manages deprecation warnings.</summary>
        private static DeprecationManager DeprecationManager;


        /*********
        ** Accessors
        *********/
        /// <summary>The full path to the configuration file.</summary>
        [JsonIgnore]
        public virtual string ConfigLocation { get; protected internal set; }

        /// <summary>The directory path containing the configuration file.</summary>
        [JsonIgnore]
        public virtual string ConfigDir => Path.GetDirectoryName(this.ConfigLocation);


        /*********
        ** Public methods
        *********/
        /// <summary>Injects types required for backwards compatibility.</summary>
        /// <param name="deprecationManager">Manages deprecation warnings.</param>
        internal static void Shim(DeprecationManager deprecationManager)
        {
            Config.DeprecationManager = deprecationManager;
        }

        /// <summary>Construct an instance of the config class.</summary>
        /// <typeparam name="T">The config class type.</typeparam>
        [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
        public virtual Config Instance<T>() where T : Config => Activator.CreateInstance<T>();

        /// <summary>Load the config from the JSON file, saving it to disk if needed.</summary>
        /// <typeparam name="T">The config class type.</typeparam>
        [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
        public virtual T LoadConfig<T>() where T : Config
        {
            // validate
            if (string.IsNullOrEmpty(this.ConfigLocation))
            {
                Log.Error("A config tried to load without specifying a location on the disk.");
                return null;
            }

            // read or generate config
            T returnValue;
            if (!File.Exists(this.ConfigLocation))
            {
                T config = this.GenerateDefaultConfig<T>();
                config.ConfigLocation = this.ConfigLocation;
                returnValue = config;
            }
            else
            {
                try
                {
                    T config = JsonConvert.DeserializeObject<T>(File.ReadAllText(this.ConfigLocation));
                    config.ConfigLocation = this.ConfigLocation;
                    returnValue = config.UpdateConfig<T>();
                }
                catch (Exception ex)
                {
                    Log.Error($"Invalid JSON ({this.GetType().Name}): {this.ConfigLocation} \n{ex}");
                    return this.GenerateDefaultConfig<T>();
                }
            }

            returnValue.WriteConfig();
            return returnValue;
        }

        /// <summary>Get the default config values.</summary>
        [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
        public virtual T GenerateDefaultConfig<T>() where T : Config
        {
            return null;
        }

        /// <summary>Get the current configuration with missing values defaulted.</summary>
        /// <typeparam name="T">The config class type.</typeparam>
        [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
        public virtual T UpdateConfig<T>() where T : Config
        {
            try
            {
                // get default + user config
                JObject defaultConfig = JObject.FromObject(this.Instance<T>().GenerateDefaultConfig<T>());
                JObject currentConfig = JObject.FromObject(this);
                defaultConfig.Merge(currentConfig, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Replace });

                // cast json object to config
                T config = defaultConfig.ToObject<T>();

                // update location
                config.ConfigLocation = this.ConfigLocation;

                return config;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occured when updating a config: {ex}");
                return this as T;
            }
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        protected Config()
        {
            Config.DeprecationManager.Warn("the Config class", "1.0", DeprecationLevel.PendingRemoval);
            Config.DeprecationManager.MarkWarned($"{nameof(Mod)}.{nameof(Mod.BaseConfigPath)}", "1.0"); // typically used to construct config, avoid redundant warnings
        }
    }

    /// <summary>Provides extension methods for <see cref="Config"/> classes.</summary>
    [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
    public static class ConfigExtensions
    {
        /// <summary>Initialise the configuration. That includes loading, saving, and merging the config file and in memory at a default state. This method should not be used to reload or to resave a config. NOTE: You MUST set your config EQUAL to the return of this method!</summary>
        /// <typeparam name="T">The config class type.</typeparam>
        /// <param name="baseConfig">The base configuration to initialise.</param>
        /// <param name="configLocation">The base configuration file path.</param>
        [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
        public static T InitializeConfig<T>(this T baseConfig, string configLocation) where T : Config
        {
            if (baseConfig == null)
                baseConfig = Activator.CreateInstance<T>();

            if (string.IsNullOrEmpty(configLocation))
            {
                Log.Error("A config tried to initialize without specifying a location on the disk.");
                return null;
            }

            baseConfig.ConfigLocation = configLocation;
            return baseConfig.LoadConfig<T>();
        }

        /// <summary>Writes the configuration to the JSON file.</summary>
        /// <typeparam name="T">The config class type.</typeparam>
        /// <param name="baseConfig">The base configuration to initialise.</param>
        [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
        public static void WriteConfig<T>(this T baseConfig) where T : Config
        {
            if (string.IsNullOrEmpty(baseConfig?.ConfigLocation) || string.IsNullOrEmpty(baseConfig.ConfigDir))
            {
                Log.Error("A config attempted to save when it itself or it's location were null.");
                return;
            }

            string json = JsonConvert.SerializeObject(baseConfig, Formatting.Indented);
            if (!Directory.Exists(baseConfig.ConfigDir))
                Directory.CreateDirectory(baseConfig.ConfigDir);

            if (!File.Exists(baseConfig.ConfigLocation) || !File.ReadAllText(baseConfig.ConfigLocation).SequenceEqual(json))
                File.WriteAllText(baseConfig.ConfigLocation, json);
        }

        /// <summary>Rereads the JSON file and merges its values with a default config. NOTE: You MUST set your config EQUAL to the return of this method!</summary>
        /// <typeparam name="T">The config class type.</typeparam>
        /// <param name="baseConfig">The base configuration to initialise.</param>
        [Obsolete("This base class is obsolete since SMAPI 1.0. See the latest project README for details.")]
        public static T ReloadConfig<T>(this T baseConfig) where T : Config
        {
            return baseConfig.LoadConfig<T>();
        }
    }
}
#endif