using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.ConfigModels;
using ContentPatcher.Framework.Tokens;
using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;

namespace ContentPatcher.Framework
{
    /// <summary>Handles the logic for reading, normalising, and saving the configuration for a content pack.</summary>
    internal class ConfigFileHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The name of the config file.</summary>
        private readonly string Filename;

        /// <summary>Parse a comma-delimited set of case-insensitive condition values.</summary>
        private readonly Func<string, InvariantHashSet> ParseCommaDelimitedField;

        /// <summary>A callback to invoke when a validation warning occurs. This is passed the content pack, label, and reason phrase respectively.</summary>
        private readonly Action<ManagedContentPack, string, string> LogWarning;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="filename">The name of the config file.</param>
        /// <param name="parseCommandDelimitedField">Parse a comma-delimited set of case-insensitive condition values.</param>
        /// <param name="logWarning">A callback to invoke when a validation warning occurs. This is passed the content pack, label, and reason phrase respectively.</param>
        public ConfigFileHandler(string filename, Func<string, InvariantHashSet> parseCommandDelimitedField, Action<ManagedContentPack, string, string> logWarning)
        {
            this.Filename = filename;
            this.ParseCommaDelimitedField = parseCommandDelimitedField;
            this.LogWarning = logWarning;
        }

        /// <summary>Read the configuration file for a content pack.</summary>
        /// <param name="contentPack">The content pack.</param>
        /// <param name="rawSchema">The raw config schema from the mod's <c>content.json</c>.</param>
        public InvariantDictionary<ConfigField> Read(ManagedContentPack contentPack, InvariantDictionary<ConfigSchemaFieldConfig> rawSchema)
        {
            InvariantDictionary<ConfigField> config = this.LoadConfigSchema(rawSchema, logWarning: (field, reason) => this.LogWarning(contentPack, $"{nameof(ContentConfig.ConfigSchema)} field '{field}'", reason));
            this.LoadConfigValues(contentPack, config, logWarning: (field, reason) => this.LogWarning(contentPack, $"{this.Filename} > {field}", reason));
            return config;
        }

        /// <summary>Save the configuration file for a content pack.</summary>
        /// <param name="contentPack">The content pack.</param>
        /// <param name="config">The configuration to save.</param>
        /// <param name="modHelper">The mod helper through which to save the file.</param>
        public void Save(ManagedContentPack contentPack, InvariantDictionary<ConfigField> config, IModHelper modHelper)
        {
            // save if settings valid
            if (config.Any())
            {
                InvariantDictionary<string> data = new InvariantDictionary<string>(config.ToDictionary(p => p.Key, p => string.Join(", ", p.Value.Value)));
                contentPack.WriteJsonFile(this.Filename, data);
            }

            // delete if no settings
            else
            {
                FileInfo file = new FileInfo(Path.Combine(contentPack.GetFullPath(this.Filename)));
                if (file.Exists)
                    file.Delete();
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Parse a raw config schema for a content pack.</summary>
        /// <param name="rawSchema">The raw config schema.</param>
        /// <param name="logWarning">The callback to invoke on each validation warning, passed the field name and reason respectively.</param>
        private InvariantDictionary<ConfigField> LoadConfigSchema(InvariantDictionary<ConfigSchemaFieldConfig> rawSchema, Action<string, string> logWarning)
        {
            InvariantDictionary<ConfigField> schema = new InvariantDictionary<ConfigField>();
            if (rawSchema == null || !rawSchema.Any())
                return schema;

            foreach (string rawKey in rawSchema.Keys)
            {
                ConfigSchemaFieldConfig field = rawSchema[rawKey];

                // validate format
                if (!TokenName.TryParse(rawKey, out TokenName name))
                {
                    logWarning(rawKey, $"the name '{rawKey}' is not in a valid format.");
                    continue;
                }
                if (name.HasSubkey())
                {
                    logWarning(rawKey, $"the name '{rawKey}' can't have a subkey (:).");
                    continue;
                }

                // validate reserved keys
                if (name.TryGetConditionType(out ConditionType _))
                {
                    logWarning(rawKey, $"can't use {name.Key} as a config field, because it's a reserved condition key.");
                    continue;
                }

                // read allowed values
                InvariantHashSet allowValues = this.ParseCommaDelimitedField(field.AllowValues);
                if (!allowValues.Any())
                {
                    logWarning(rawKey, $"no {nameof(ConfigSchemaFieldConfig.AllowValues)} specified.");
                    continue;
                }

                // read default values
                InvariantHashSet defaultValues = this.ParseCommaDelimitedField(field.Default);
                {
                    // inject default
                    if (!defaultValues.Any() && !field.AllowBlank)
                        defaultValues = new InvariantHashSet(allowValues.First());

                    // validate values
                    string[] invalidValues = defaultValues.ExceptIgnoreCase(allowValues).ToArray();
                    if (invalidValues.Any())
                    {
                        logWarning(rawKey, $"default values '{string.Join(", ", invalidValues)}' are not allowed according to {nameof(ConfigSchemaFieldConfig.AllowBlank)}.");
                        continue;
                    }

                    // validate allow multiple
                    if (!field.AllowMultiple && defaultValues.Count > 1)
                    {
                        logWarning(rawKey, $"can't have multiple default values because {nameof(ConfigSchemaFieldConfig.AllowMultiple)} is false.");
                        continue;
                    }
                }

                // add to schema
                schema[rawKey] = new ConfigField(allowValues, defaultValues, field.AllowBlank, field.AllowMultiple);
            }

            return schema;
        }

        /// <summary>Load config values from the content pack.</summary>
        /// <param name="contentPack">The content pack whose config file to read.</param>
        /// <param name="config">The config schema.</param>
        /// <param name="logWarning">The callback to invoke on each validation warning, passed the field name and reason respectively.</param>
        private void LoadConfigValues(ManagedContentPack contentPack, InvariantDictionary<ConfigField> config, Action<string, string> logWarning)
        {
            if (!config.Any())
                return;

            // read raw config
            InvariantDictionary<InvariantHashSet> configValues = new InvariantDictionary<InvariantHashSet>(
                from entry in (contentPack.ReadJsonFile<InvariantDictionary<string>>(this.Filename) ?? new InvariantDictionary<string>())
                let key = entry.Key.Trim()
                let value = this.ParseCommaDelimitedField(entry.Value)
                select new KeyValuePair<string, InvariantHashSet>(key, value)
            );

            // remove invalid values
            foreach (string key in configValues.Keys.ExceptIgnoreCase(config.Keys).ToArray())
            {
                logWarning(key, "no such field supported by this content pack.");
                configValues.Remove(key);
            }

            // inject default values
            foreach (string key in config.Keys)
            {
                ConfigField field = config[key];
                if (!configValues.TryGetValue(key, out InvariantHashSet values) || (!field.AllowBlank && !values.Any()))
                    configValues[key] = field.DefaultValues;
            }

            // parse each field
            foreach (string key in config.Keys)
            {
                // set value
                ConfigField field = config[key];
                field.Value = configValues[key];

                // validate allow-multiple
                if (!field.AllowMultiple && field.Value.Count > 1)
                {
                    logWarning(key, "field only allows a single value.");
                    field.Value = field.DefaultValues;
                    continue;
                }

                // validate allow-values
                string[] invalidValues = field.Value.ExceptIgnoreCase(field.AllowValues).ToArray();
                if (invalidValues.Any())
                {
                    logWarning(key, $"found invalid values ({string.Join(", ", invalidValues)}), expected: {string.Join(", ", field.AllowValues)}.");
                    field.Value = field.DefaultValues;
                }
            }
        }
    }
}
