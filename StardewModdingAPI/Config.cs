/*
    Copyright 2016 Zoey (Zoryn)
*/

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StardewModdingAPI
{
    public class Config
    {
        [JsonIgnore]
        public virtual string ConfigLocation { get; protected internal set; }

        [JsonIgnore]
        public virtual string ConfigDir => Path.GetDirectoryName(ConfigLocation);

        public virtual Config Instance<T>() where T : Config => Activator.CreateInstance<T>();

        /// <summary>
        /// Should never be used for anything.
        /// </summary>
        public Config()
        {

        }

        /// <summary>
        /// Loads the config from the json blob on disk, updating and re-writing to the disk if needed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T LoadConfig<T>() where T : Config
        {
            if (string.IsNullOrEmpty(ConfigLocation))
            {
                Log.Error("A config tried to load without specifying a location on the disk.");
                return null;
            }

            T ret = null;

            if (!File.Exists(ConfigLocation))
            {
                //no config exists, generate default values
                var c = this.GenerateDefaultConfig<T>();
                c.ConfigLocation = ConfigLocation;
                ret = c;
            }
            else
            {
                try
                {
                    //try to load the config from a json blob on disk
                    T c = JsonConvert.DeserializeObject<T>(File.ReadAllText(ConfigLocation));

                    c.ConfigLocation = ConfigLocation;

                    //update the config with default values if needed
                    ret = c.UpdateConfig<T>();

                    c = null;
                }
                catch (Exception ex)
                {
                    Log.Error("Invalid JSON ({0}): {1} \n{2}", GetType().Name, ConfigLocation, ex);
                    return GenerateDefaultConfig<T>();
                }
            }

            ret.WriteConfig();
            return ret;
        }

        /// <summary>
        /// MUST be implemented in inheriting class!
        /// </summary>
        public virtual T GenerateDefaultConfig<T>() where T : Config
        {
            return null;
        }

        /// <summary>
        /// Merges a default-value config with the user-config on disk.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual T UpdateConfig<T>() where T : Config
        {
            try
            {
                //default config
                var b = JObject.FromObject(Instance<T>().GenerateDefaultConfig<T>());

                //user config
                var u = JObject.FromObject(this);

                //overwrite default values with user values
                b.Merge(u, new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Replace});

                //cast json object to config
                T c = b.ToObject<T>();

                //re-write the location on disk to the object
                c.ConfigLocation = ConfigLocation;

                return c;
            }
            catch (Exception ex)
            {
                Log.Error("An error occured when updating a config: " + ex);
                return this as T;
            }
        }
    }

    public static class ConfigExtensions
    {
        /// <summary>
        /// Initializes an instance of any class that inherits from Config.
        /// This method performs the loading, saving, and merging of the config on the disk and in memory at a default state.
        /// This method should not be used to re-load or to re-save a config.
        /// NOTE: You MUST set your config EQUAL to the return of this method!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="baseConfig"></param>
        /// <param name="configLocation"></param>
        /// <returns></returns>
        public static T InitializeConfig<T>(this T baseConfig, string configLocation) where T : Config
        {
            if (baseConfig == null)
            {
                baseConfig = Activator.CreateInstance<T>();
                /*
                Log.Error("A config tried to initialize whilst being null.");
                return null;
                */
            }

            if (string.IsNullOrEmpty(configLocation))
            {
                Log.Error("A config tried to initialize without specifying a location on the disk.");
                return null;
            }

            baseConfig.ConfigLocation = configLocation;
            T c = baseConfig.LoadConfig<T>();

            return c;
        }

        /// <summary>
        /// Writes a config to a json blob on the disk specified in the config's properties.
        /// </summary>
        public static void WriteConfig<T>(this T baseConfig) where T : Config
        {
            if (string.IsNullOrEmpty(baseConfig?.ConfigLocation) || string.IsNullOrEmpty(baseConfig.ConfigDir))
            {
                Log.Error("A config attempted to save when it itself or it's location were null.");
                return;
            }

            string s = JsonConvert.SerializeObject(baseConfig, typeof (T), Formatting.Indented, new JsonSerializerSettings());

            if (!Directory.Exists(baseConfig.ConfigDir))
                Directory.CreateDirectory(baseConfig.ConfigDir);

            if (!File.Exists(baseConfig.ConfigLocation) || !File.ReadAllText(baseConfig.ConfigLocation).SequenceEqual(s))
                File.WriteAllText(baseConfig.ConfigLocation, s);
        }

        /// <summary>
        /// Re-reads the json blob on the disk and merges its values with a default config
        /// </summary>
        public static T ReloadConfig<T>(this T baseConfig) where T : Config
        {
            return baseConfig.UpdateConfig<T>();
        }
    }
}