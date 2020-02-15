using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class DayTimeMoneyBoxMethods : DayTimeMoneyBox
    {
        public void drawMoneyBox(SpriteBatch b, int overrideX = -1, int overrideY = -1)
        {
            base.drawMoneyBox(b, overrideX, overrideY);
        }

    }
}
