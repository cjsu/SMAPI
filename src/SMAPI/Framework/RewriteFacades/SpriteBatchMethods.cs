using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

#pragma warning disable 1591 // missing documentation
namespace StardewModdingAPI.Framework.RewriteFacades
{
    /// <summary>Provides <see cref="SpriteBatch"/> method signatures that can be injected into mod code for compatibility between Linux/Mac or Windows.</summary>
    /// <remarks>This is public to support SMAPI rewriting and should not be referenced directly by mods.</remarks>
    public class SpriteBatchMethods : SpriteBatch
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SpriteBatchMethods(GraphicsDevice graphicsDevice) : base(graphicsDevice) { }


        /****
        ** MonoGame signatures
        ****/
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Linux/Mac.")]
        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix? matrix)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, matrix ?? Matrix.Identity);
        }

        /****
        ** XNA signatures
        ****/
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void Begin()
        {
            base.Begin();
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void Begin(SpriteSortMode sortMode, BlendState blendState)
        {
            base.Begin(sortMode, blendState);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void Begin(SpriteSortMode sortMode, BlendState blendState, SamplerState samplerState, DepthStencilState depthStencilState, RasterizerState rasterizerState, Effect effect, Matrix transformMatrix)
        {
            base.Begin(sortMode, blendState, samplerState, depthStencilState, rasterizerState, effect, transformMatrix);
        }
    }
}
