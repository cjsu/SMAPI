using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UIInfoSuite.Options
{
    public class ModOptionsElement
    {
        private const int DefaultX = 8;
        private const int DefaultY = 4;
        private const int DefaultPixelSize = 9;
        private Rectangle _bounds;
        private string _label;
        private int _whichOption;
        protected bool _canClick = true;

        public Rectangle Bounds { get { return this._bounds; } }

        public ModOptionsElement(string label)
            : this(label, -1, -1, DefaultPixelSize * Game1.pixelZoom, DefaultPixelSize * Game1.pixelZoom)
        {

        }

        public ModOptionsElement(string label, int x, int y, int width, int height, int whichOption = -1)
        {
            if (x < 0)
                x = DefaultX * Game1.pixelZoom;

            if (y < 0)
                y = DefaultY * Game1.pixelZoom;

            this._bounds = new Rectangle(x, y, width, height);
            this._label = label;
            this._whichOption = whichOption;
        }

        public virtual void ReceiveLeftClick(int x, int y)
        {

        }

        public virtual void LeftClickHeld(int x, int y)
        {

        }

        public virtual void LeftClickReleased(int x, int y)
        {

        }

        public virtual void ReceiveKeyPress(Keys key)
        {

        }

        public virtual void Draw(SpriteBatch batch, int slotX, int slotY)
        {
            if (this._whichOption < 0)
            {
                SpriteText.drawString(batch, this._label, slotX + this._bounds.X, slotY + this._bounds.Y + Game1.pixelZoom * 3, 999, -1, 999, 1, 0.1f);
            }
            else
            {
                Utility.drawTextWithShadow(batch,
                    this._label, 
                    Game1.dialogueFont, 
                    new Vector2(slotX + this._bounds.X + this._bounds.Width + Game1.pixelZoom * 2, slotY + this._bounds.Y),
                    this._canClick ? Game1.textColor : Game1.textColor * 0.33f, 
                    1f, 
                    0.1f);
            }
        }
    }
}
