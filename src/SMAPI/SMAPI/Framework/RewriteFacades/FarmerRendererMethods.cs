using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace StardewModdingAPI.SMAPI.Framework.RewriteFacades
{
    public class FarmerRendererMethods : FarmerRenderer
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public new void drawMiniPortrat(SpriteBatch b, Vector2 position, float layerDepth, float scale, int facingDirection, Farmer who)
        {
            base.drawMiniPortrat(b, position, layerDepth, scale, facingDirection, who);
        }
    }
}
