using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class DiscreteColorPickerMethods : DiscreteColorPicker
    {
        public DiscreteColorPickerMethods(int xPosition, int yPosition, int startingColor = 0, Item itemToDrawColored = null)
            :base(xPosition, yPosition, startingColor, itemToDrawColored)
        {
        }
    }
}
