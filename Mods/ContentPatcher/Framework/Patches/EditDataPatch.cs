using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.ConfigModels;
using ContentPatcher.Framework.Tokens;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Patches
{
    /// <summary>Metadata for a data to edit into a data file.</summary>
    internal class EditDataPatch : Patch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The data records to edit.</summary>
        private readonly EditDataPatchRecord[] Records;

        /// <summary>The data fields to edit.</summary>
        private readonly EditDataPatchField[] Fields;

        /// <summary>The token strings which contain mutable tokens.</summary>
        private readonly TokenString[] MutableTokenStrings;

        /// <summary>Whether the next context update is the first one.</summary>
        private bool IsFirstUpdate = true;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="logName">A unique name for this patch shown in log messages.</param>
        /// <param name="contentPack">The content pack which requested the patch.</param>
        /// <param name="assetName">The normalised asset name to intercept.</param>
        /// <param name="conditions">The conditions which determine whether this patch should be applied.</param>
        /// <param name="records">The data records to edit.</param>
        /// <param name="fields">The data fields to edit.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="normaliseAssetName">Normalise an asset name.</param>
        public EditDataPatch(string logName, ManagedContentPack contentPack, TokenString assetName, ConditionDictionary conditions, IEnumerable<EditDataPatchRecord> records, IEnumerable<EditDataPatchField> fields, IMonitor monitor, Func<string, string> normaliseAssetName)
            : base(logName, PatchType.EditData, contentPack, assetName, conditions, normaliseAssetName)
        {
            this.Records = records.ToArray();
            this.Fields = fields.ToArray();
            this.Monitor = monitor;
            this.MutableTokenStrings = this.GetTokenStrings(this.Records, this.Fields).Where(str => str.Tokens.Any()).ToArray();
        }

        /// <summary>Update the patch data when the context changes.</summary>
        /// <param name="context">Provides access to contextual tokens.</param>
        /// <returns>Returns whether the patch data changed.</returns>
        public override bool UpdateContext(IContext context)
        {
            bool changed = base.UpdateContext(context);

            // We need to update all token strings once. After this first time, we can skip
            // updating any immutable tokens.
            if (this.IsFirstUpdate)
            {
                this.IsFirstUpdate = false;
                foreach (TokenString str in this.GetTokenStrings(this.Records, this.Fields))
                    changed |= str.UpdateContext(context);
            }
            else
            {
                foreach (TokenString str in this.MutableTokenStrings)
                    changed |= str.UpdateContext(context);
            }

            return changed;
        }

        /// <summary>Get the tokens used by this patch in its fields.</summary>
        public override IEnumerable<TokenName> GetTokensUsed()
        {
            if (this.MutableTokenStrings.Length == 0)
                return base.GetTokensUsed();

            return base
                .GetTokensUsed()
                .Union(this.MutableTokenStrings.SelectMany(p => p.Tokens));
        }

        /// <summary>Apply the patch to a loaded asset.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="asset">The asset to edit.</param>
        /// <exception cref="NotSupportedException">The current patch type doesn't support editing assets.</exception>
        public override void Edit<T>(IAssetData asset)
        {
            // validate
            if (!typeof(T).IsGenericType || typeof(T).GetGenericTypeDefinition() != typeof(Dictionary<,>))
            {
                this.Monitor.Log($"Can't apply data patch \"{this.LogName}\" to {this.TargetAsset}: this file isn't a data file (found {(typeof(T) == typeof(Texture2D) ? "image" : typeof(T).Name)}).", LogLevel.Warn);
                return;
            }

            // get dictionary's key type
            Type keyType = typeof(T).GetGenericArguments().FirstOrDefault();
            if (keyType == null)
                throw new InvalidOperationException("Can't parse the asset's dictionary key type.");

            // get underlying apply method
            MethodInfo method = this.GetType().GetMethod(nameof(this.ApplyImpl), BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Can't fetch the internal apply method.");

            // invoke method
            method
                .MakeGenericMethod(keyType)
                .Invoke(this, new object[] { asset });
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get all token strings in the given data.</summary>
        /// <param name="records">The data records to edit.</param>
        /// <param name="fields">The data fields to edit.</param>
        private IEnumerable<TokenString> GetTokenStrings(IEnumerable<EditDataPatchRecord> records, IEnumerable<EditDataPatchField> fields)
        {
            foreach (TokenString tokenStr in records.SelectMany(p => p.GetTokenStrings()))
                yield return tokenStr;
            foreach (TokenString tokenStr in fields.SelectMany(p => p.GetTokenStrings()))
                yield return tokenStr;
        }

        /// <summary>Apply the patch to an asset.</summary>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <param name="asset">The asset to edit.</param>
        private void ApplyImpl<TKey>(IAssetData asset)
        {
            IDictionary<TKey, string> data = asset.AsDictionary<TKey, string>().Data;

            // apply records
            if (this.Records != null)
            {
                foreach (EditDataPatchRecord record in this.Records)
                {
                    TKey key = (TKey)Convert.ChangeType(record.Key.Value, typeof(TKey));
                    if (record.Value.Value != null)
                        data[key] = record.Value.Value;
                    else
                        data.Remove(key);
                }
            }

            // apply fields
            if (this.Fields != null)
            {
                foreach (var recordGroup in this.Fields.GroupByIgnoreCase(p => p.Key.Value))
                {
                    TKey key = (TKey)Convert.ChangeType(recordGroup.Key, typeof(TKey));
                    if (!data.ContainsKey(key))
                    {
                        this.Monitor.Log($"Can't apply data patch \"{this.LogName}\" to {this.TargetAsset}: there's no record matching key '{key}' under {nameof(PatchConfig.Fields)}.", LogLevel.Warn);
                        continue;
                    }

                    string[] actualFields = data[key].Split('/');
                    foreach (EditDataPatchField field in recordGroup)
                    {
                        if (field.FieldIndex < 0 || field.FieldIndex > actualFields.Length - 1)
                        {
                            this.Monitor.Log($"Can't apply data field \"{this.LogName}\" to {this.TargetAsset}: record '{key}' under {nameof(PatchConfig.Fields)} has no field with index {field.FieldIndex} (must be 0 to {actualFields.Length - 1}).", LogLevel.Warn);
                            continue;
                        }

                        actualFields[field.FieldIndex] = field.Value.Value;
                    }

                    data[key] = string.Join("/", actualFields);
                }
            }
        }
    }
}
