using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Common.Integrations.CustomFarmingRedux
{
    /// <summary>Handles the logic for integrating with the Custom Farming Redux mod.</summary>
    internal class CustomFarmingReduxIntegration : BaseIntegration
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod's public API.</summary>
        private readonly ICustomFarmingApi ModApi;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modRegistry">An API for fetching metadata about loaded mods.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public CustomFarmingReduxIntegration(IModRegistry modRegistry, IMonitor monitor)
            : base("Custom Farming Redux", "Platonymous.CustomFarming", "2.8.5", modRegistry, monitor)
        {
            if (!this.IsLoaded)
                return;

            // get mod API
            this.ModApi = this.GetValidatedApi<ICustomFarmingApi>();
            this.IsLoaded = this.ModApi != null;
        }

        /// <summary>Get the sprite info for a custom object, or <c>null</c> if the object isn't custom.</summary>
        /// <param name="obj">The custom object.</param>
        public SpriteInfo GetSprite(SObject obj)
        {
            this.AssertLoaded();

            Tuple<Item, Texture2D, Rectangle, Color> data = this.ModApi.getRealItemAndTexture(obj);
            return data != null
                ? new SpriteInfo(data.Item2, data.Item3)
                : null;
        }
    }
}
