using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace StardewModdingAPI.SMAPI.Framework.RewriteFacades
{
    public class Game1Methods : Game1
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new string parseText(string text, SpriteFont whichFont, int width)
        {
            return parseText(text, whichFont, width, 1);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(LocationRequest locationRequest, int tileX, int tileY, int facingDirectionAfterWarp)
        {
            warpFarmer(locationRequest, tileX, tileY, facingDirectionAfterWarp, true, false);
        }
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(string locationName, int tileX, int tileY, bool flip)
        {
            warpFarmer(locationName, tileX, tileY, flip ? ((player.FacingDirection + 2) % 4) : player.FacingDirection);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(string locationName, int tileX, int tileY, int facingDirectionAfterWarp)
        {
            warpFarmer(locationName, tileX, tileY, facingDirectionAfterWarp, false, true, false);
        }
    }
}
