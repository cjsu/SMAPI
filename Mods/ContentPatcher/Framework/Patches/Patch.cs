using System;
using System.Collections.Generic;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.Tokens;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Patches
{
    /// <summary>Metadata for a conditional patch.</summary>
    internal abstract class Patch : IPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Normalise an asset name.</summary>
        private readonly Func<string, string> NormaliseAssetName;


        /*********
        ** Accessors
        *********/
        /// <summary>The last context used to update this patch.</summary>
        protected IContext LastContext { get; private set; }

        /// <summary>A unique name for this patch shown in log messages.</summary>
        public string LogName { get; }

        /// <summary>The patch type.</summary>
        public PatchType Type { get; }

        /// <summary>The content pack which requested the patch.</summary>
        public ManagedContentPack ContentPack { get; }

        /// <summary>The raw asset key to intercept (if applicable), including tokens.</summary>
        public TokenString FromLocalAsset { get; protected set; }

        /// <summary>The normalised asset name to intercept.</summary>
        public string TargetAsset { get; private set; }

        /// <summary>The raw asset name to intercept, including tokens.</summary>
        public TokenString RawTargetAsset { get; }

        /// <summary>The conditions which determine whether this patch should be applied.</summary>
        public ConditionDictionary Conditions { get; }

        /// <summary>Whether this patch should be applied in the latest context.</summary>
        public bool MatchesContext { get; private set; }

        /// <summary>Whether this patch is valid if <see cref="MatchesContext"/> is true.</summary>
        public bool IsValidInContext { get; protected set; } = true;

        /// <summary>Whether the patch is currently applied to the target asset.</summary>
        public bool IsApplied { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Update the patch data when the context changes.</summary>
        /// <param name="context">Provides access to contextual tokens.</param>
        /// <returns>Returns whether the patch data changed.</returns>
        public virtual bool UpdateContext(IContext context)
        {
            this.LastContext = context;

            // update conditions
            bool conditionsChanged;
            {
                bool wasMatch = this.MatchesContext;
                this.MatchesContext =
                    (this.Conditions.Count == 0 || this.Conditions.Values.All(p => p.IsMatch(context)))
                    && this.GetTokensUsed().All(p => context.Contains(p, enforceContext: true));
                conditionsChanged = wasMatch != this.MatchesContext;
            }
            // update target asset
            bool targetChanged = this.RawTargetAsset.UpdateContext(context);
            this.TargetAsset = this.NormaliseAssetName(this.RawTargetAsset.Value);

            // update source asset
            bool sourceChanged = false;
            if (this.FromLocalAsset != null)
            {
                sourceChanged = this.FromLocalAsset.UpdateContext(context);
                this.IsValidInContext = this.FromLocalAsset.IsReady && this.ContentPack.HasFile(this.FromLocalAsset.Value);
            }
            
            return conditionsChanged || targetChanged || sourceChanged;
        }

        /// <summary>Load the initial version of the asset.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="asset">The asset to load.</param>
        /// <exception cref="NotSupportedException">The current patch type doesn't support loading assets.</exception>
        public virtual T Load<T>(IAssetInfo asset)
        {
            throw new NotSupportedException("This patch type doesn't support loading assets.");
        }

        /// <summary>Apply the patch to a loaded asset.</summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="asset">The asset to edit.</param>
        /// <exception cref="NotSupportedException">The current patch type doesn't support editing assets.</exception>
        public virtual void Edit<T>(IAssetData asset)
        {
            throw new NotSupportedException("This patch type doesn't support loading assets.");
        }

        /// <summary>Get the tokens used by this patch in its fields.</summary>
        public virtual IEnumerable<TokenName> GetTokensUsed()
        {
            return this.RawTargetAsset.Tokens;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="logName">A unique name for this patch shown in log messages.</param>
        /// <param name="type">The patch type.</param>
        /// <param name="contentPack">The content pack which requested the patch.</param>
        /// <param name="assetName">The normalised asset name to intercept.</param>
        /// <param name="conditions">The conditions which determine whether this patch should be applied.</param>
        /// <param name="normaliseAssetName">Normalise an asset name.</param>
        protected Patch(string logName, PatchType type, ManagedContentPack contentPack, TokenString assetName, ConditionDictionary conditions, Func<string, string> normaliseAssetName)
        {
            this.LogName = logName;
            this.Type = type;
            this.ContentPack = contentPack;
            this.RawTargetAsset = assetName;
            this.Conditions = conditions;
            this.NormaliseAssetName = normaliseAssetName;
        }
    }
}
