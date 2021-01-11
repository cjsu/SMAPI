using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StardewModdingAPI.Framework.ModLoading.RewriteFacades
{
    /// <summary>Provides <see cref="SpriteBatch"/> method signatures that can be injected into mod code for compatibility between Linux/Mac or Windows.</summary>
    /// <remarks>This is public to support SMAPI rewriting and should not be referenced directly by mods.</remarks>
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Used via assembly rewriting")]
    [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Linux/Mac.")]
    [SuppressMessage("ReSharper", "CS1591", Justification = "Documentation not needed for facade classes.")]
    public class SpriteBatchFacade : SpriteBatch
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SpriteBatchFacade(GraphicsDevice graphicsDevice) : base(graphicsDevice) { }


        /****
        ** MonoGame signatures
        ****/
        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix? matrix)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, matrix ?? Matrix.Identity);
        }

        /****
        ** XNA signatures
        ****/
        public new void Begin()
        {
            base.Begin();
        }

        public new void Begin(SpriteSortMode sortMode, BlendState blendState)
        {
            base.Begin(sortMode, blendState);
        }

        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState);
        }

        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect);
        }

        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
        }
    }
}
