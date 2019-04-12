using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite
{
    class IconHandler
    {
        public static IconHandler Handler { get; private set; }

        static IconHandler()
        {
            if (Handler == null)
                Handler = new IconHandler();
        }

        private int _amountOfVisibleIcons;

        private IconHandler()
        {

        }

        public Point GetNewIconPosition()
        {
            int yPos = Game1.options.zoomButtons ? 320 : 290;
            int xPosition = (int)Tools.GetWidthInPlayArea() - 214 - 46 * this._amountOfVisibleIcons;
            ++this._amountOfVisibleIcons;
            return new Point(xPosition, yPos);
        }

        public void Reset(object sender, EventArgs e)
        {
            this._amountOfVisibleIcons = 0;
        }
        private int scaledViewportWidth
        {
            get
            {
                return (int)(((float)Game1.clientBounds.Width) / Game1.menuButtonScale);
            }
        }
    }
}
