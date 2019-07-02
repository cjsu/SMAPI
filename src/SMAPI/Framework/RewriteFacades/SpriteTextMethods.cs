using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class SpriteTextMethods : SpriteText
    {
        public static int getWidthOfString(string s, int widthConstraint = 999999)
        {
            return getWidthOfString(s);
        }

        public static void drawStringHorizontallyCenteredAt(SpriteBatch b, string s, int x, int y, int characterPosition = 999999, int width = -1, int height = 999999, float alpha = -1f, float layerDepth = 0.88f, bool junimoText = false, int color = -1, int maxWdith = 99999)
        {
            drawString(b, s, x - SpriteText.getWidthOfString(s) / 2, y, characterPosition, width, height, alpha, layerDepth, junimoText, -1, "", color);
        }

        public static void drawStringWithScrollBackground(SpriteBatch b, string s, int x, int y, string placeHolderWidthText, float alpha, int color)
        {
            drawStringWithScrollBackground(b, s, x, y, placeHolderWidthText, alpha, color, 0.088f);
        }
    }
}
