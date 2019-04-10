using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace ContentPatcher.Framework
{
    /// <summary>Renders debug information to the screen.</summary>
    internal class DebugOverlay : BaseOverlay
    {
        /*********
        ** Fields
        *********/
        /// <summary>The size of the margin around the displayed legend.</summary>
        private readonly int Margin = 30;

        /// <summary>The padding between the border and content.</summary>
        private readonly int Padding = 5;

        /// <summary>The content helper from which to read textures.</summary>
        private readonly IContentHelper Content;

        /// <summary>The spritesheets to render.</summary>
        private readonly string[] TextureNames;

        /// <summary>The current spritesheet to display.</summary>
        private string CurrentName;

        /// <summary>The current texture to display.</summary>
        private Texture2D CurrentTexture;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="events">The SMAPI events available for mods.</param>
        /// <param name="inputHelper">An API for checking and changing input state.</param>
        /// <param name="contentHelper">The content helper from which to read textures.</param>
        public DebugOverlay(IModEvents events, IInputHelper inputHelper, IContentHelper contentHelper)
            : base(events, inputHelper)
        {
            this.Content = contentHelper;
            this.TextureNames = this.GetTextureNames(contentHelper).OrderByIgnoreCase(p => p).ToArray();
            this.NextTexture();
        }

        /// <summary>Switch to the next texture.</summary>
        public void NextTexture()
        {
            int index = Array.IndexOf(this.TextureNames, this.CurrentName) + 1;
            if (index >= this.TextureNames.Length)
                index = 0;
            this.CurrentName = this.TextureNames[index];
            this.CurrentTexture = this.Content.Load<Texture2D>(this.CurrentName, ContentSource.GameContent);
        }

        /// <summary>Switch to the previous data map.</summary>
        public void PrevTexture()
        {
            int index = Array.IndexOf(this.TextureNames, this.CurrentName) - 1;
            if (index < 0)
                index = this.TextureNames.Length - 1;
            this.CurrentName = this.TextureNames[index];
            this.CurrentTexture = this.Content.Load<Texture2D>(this.CurrentName, ContentSource.GameContent);
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Draw to the screen.</summary>
        /// <param name="spriteBatch">The sprite batch to which to draw.</param>
        protected override void Draw(SpriteBatch spriteBatch)
        {
            Vector2 labelSize = Game1.smallFont.MeasureString(this.CurrentName);
            int contentWidth = (int)Math.Max(labelSize.X, this.CurrentTexture?.Width ?? 0);

            CommonHelper.DrawScroll(spriteBatch, new Vector2(this.Margin), new Vector2(contentWidth, labelSize.Y + this.Padding + (this.CurrentTexture?.Height ?? (int)labelSize.Y)), out Vector2 contentPos, out Rectangle _, padding: this.Padding);
            spriteBatch.DrawString(Game1.smallFont, this.CurrentName, new Vector2(contentPos.X + ((contentWidth - labelSize.X) / 2), contentPos.Y), Color.Black);

            if (this.CurrentTexture != null)
                spriteBatch.Draw(this.CurrentTexture, contentPos + new Vector2(0, labelSize.Y + this.Padding), Color.White);
            else
                spriteBatch.DrawString(Game1.smallFont, "(null)", contentPos + new Vector2(0, labelSize.Y + this.Padding), Color.Black);
        }

        /// <summary>Get all texture asset names in the given content helper.</summary>
        /// <param name="contentHelper">The content helper to search.</param>
        private IEnumerable<string> GetTextureNames(IContentHelper contentHelper)
        {
            // get all texture keys from the content helper (this is such a hack)
            IList<string> textureKeys = new List<string>();
            contentHelper.InvalidateCache(asset =>
            {
                if (asset.DataType == typeof(Texture2D) && !asset.AssetName.Contains("..") && !asset.AssetName.StartsWith(StardewModdingAPI.Constants.ExecutionPath))
                    textureKeys.Add(asset.AssetName);
                return false;
            });
            return textureKeys;
        }
    }
}
