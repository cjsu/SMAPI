using System;
using System.Reflection;

namespace StardewModdingAPI.Framework.Content
{
    /// <summary>A wrapper for <see cref="IAssetEditor"/> and <see cref="IAssetLoader"/> for internal cache invalidation.</summary>
    internal class AssetInterceptorChange
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The mod which registered the interceptor.</summary>
        public IModMetadata Mod { get; }

        /// <summary>The interceptor instance.</summary>
        public object Instance { get; }

        /// <summary>Whether the asset interceptor was added since the last tick. Mutually exclusive with <see cref="WasRemoved"/>.</summary>
        public bool WasAdded { get; }

        /// <summary>Whether the asset interceptor was removed since the last tick. Mutually exclusive with <see cref="WasRemoved"/>.</summary>
        public bool WasRemoved => this.WasAdded;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="mod">The mod registering the interceptor.</param>
        /// <param name="instance">The interceptor. This must be an <see cref="IAssetEditor"/> or <see cref="IAssetLoader"/> instance.</param>
        /// <param name="wasAdded">Whether the asset interceptor was added since the last tick; else removed.</param>
        public AssetInterceptorChange(IModMetadata mod, object instance, bool wasAdded)
        {
            this.Mod = mod ?? throw new ArgumentNullException(nameof(mod));
            this.Instance = instance ?? throw new ArgumentNullException(nameof(instance));
            this.WasAdded = wasAdded;

            if (!(instance is IAssetEditor) && !(instance is IAssetLoader))
                throw new InvalidCastException($"The provided {nameof(instance)} value must be an {nameof(IAssetEditor)} or {nameof(IAssetLoader)} instance.");
        }

        /// <summary>Get whether this instance can intercept the given asset.</summary>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        public bool CanIntercept(IAssetInfo asset)
        {
            MethodInfo canIntercept = this.GetType().GetMethod(nameof(this.CanInterceptImpl), BindingFlags.Instance | BindingFlags.NonPublic);
            if (canIntercept == null)
                throw new InvalidOperationException($"SMAPI couldn't access the {nameof(AssetInterceptorChange)}.{nameof(this.CanInterceptImpl)} implementation.");

            return (bool)canIntercept.MakeGenericMethod(asset.DataType).Invoke(this, new object[] { asset });
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether this instance can intercept the given asset.</summary>
        /// <typeparam name="TAsset">The asset type.</typeparam>
        /// <param name="asset">Basic metadata about the asset being loaded.</param>
        private bool CanInterceptImpl<TAsset>(IAssetInfo asset)
        {
            // check edit
            if (this.Instance is IAssetEditor editor)
            {
                try
                {
                    if (editor.CanEdit<TAsset>(asset))
                        return true;
                }
                catch (Exception ex)
                {
                    this.Mod.LogAsMod($"Mod failed when checking whether it could edit asset '{asset.AssetName}'. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }
            }

            // check load
            if (this.Instance is IAssetLoader loader)
            {
                try
                {
                    if (loader.CanLoad<TAsset>(asset))
                        return true;
                }
                catch (Exception ex)
                {
                    this.Mod.LogAsMod($"Mod failed when checking whether it could load asset '{asset.AssetName}'. Error details:\n{ex.GetLogSummary()}", LogLevel.Error);
                }
            }

            return false;
        }
    }
}
