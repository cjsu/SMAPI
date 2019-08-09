using System;
using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Toolkit.Serialization;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>The static state to use while <see cref="Game1"/> is initializing, which happens before the <see cref="SGame"/> constructor runs.</summary>
    internal class SGameConstructorHack
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        public IMonitor Monitor { get; }

        /// <summary>Simplifies access to private game code.</summary>
        public Reflector Reflection { get; }

        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        public JsonHelper JsonHelper { get; }

        /// <summary>A callback to invoke the first time *any* game content manager loads an asset.</summary>
        public Action OnLoadingFirstAsset { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        /// <param name="onLoadingFirstAsset">A callback to invoke the first time *any* game content manager loads an asset.</param>
        public SGameConstructorHack(IMonitor monitor, Reflector reflection, JsonHelper jsonHelper, Action onLoadingFirstAsset)
        {
            this.Monitor = monitor;
            this.Reflection = reflection;
            this.JsonHelper = jsonHelper;
            this.OnLoadingFirstAsset = onLoadingFirstAsset;
        }
    }
}
