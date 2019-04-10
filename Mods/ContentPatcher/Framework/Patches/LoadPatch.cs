using System;
using System.Collections.Generic;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.Tokens;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace ContentPatcher.Framework.Patches
{
    /// <summary>Metadata for an asset that should be replaced with a content pack file.</summary>
    internal class LoadPatch : Patch
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="logName">A unique name for this patch shown in log messages.</param>
        /// <param name="contentPack">The content pack which requested the patch.</param>
        /// <param name="assetName">The normalised asset name to intercept.</param>
        /// <param name="conditions">The conditions which determine whether this patch should be applied.</param>
        /// <param name="localAsset">The asset key to load from the content pack instead.</param>
        /// <param name="normaliseAssetName">Normalise an asset name.</param>
        public LoadPatch(string logName, ManagedContentPack contentPack, TokenString assetName, ConditionDictionary conditions, TokenString localAsset, Func<string, string> normaliseAssetName)
            : base(logName, PatchType.Load, contentPack, assetName, conditions, normaliseAssetName)
        {
            this.FromLocalAsset = localAsset;
        }

        /// <summary>Load the initial version of the asset.</summary>
        /// <param name="asset">The asset to load.</param>
        public override T Load<T>(IAssetInfo asset)
        {
            T data = this.ContentPack.Load<T>(this.FromLocalAsset.Value);
            return (data as object) is Texture2D texture
                ? (T)(object)this.CloneTexture(texture)
                : data;
        }

        /// <summary>Get the tokens used by this patch in its fields.</summary>
        public override IEnumerable<TokenName> GetTokensUsed()
        {
            return base.GetTokensUsed().Union(this.FromLocalAsset.Tokens);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Clone a texture.</summary>
        /// <param name="source">The texture to clone.</param>
        /// <returns>Cloning a texture is necessary when loading to avoid having it shared between different content managers, which can lead to undesirable effects like two players having synchronised texture changes.</returns>
        private Texture2D CloneTexture(Texture2D source)
        {
            // get data
            int[] pixels = new int[source.Width * source.Height];
            source.GetData(pixels);

            // create clone
            Texture2D target = new Texture2D(source.GraphicsDevice, source.Width, source.Height);
            target.SetData(pixels);
            return target;
        }
    }
}
