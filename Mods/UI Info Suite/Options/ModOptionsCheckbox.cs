using UIInfoSuite.Extensions;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using StardewValley.Menus;

namespace UIInfoSuite.Options
{
    class ModOptionsCheckbox : ModOptionsElement
    {
        private const int PixelSize = 9;

        private readonly Action<bool> _toggleOptionsDelegate;
        private bool _isChecked;
        private readonly IDictionary<string, string> _options;
        private readonly string _optionKey;

        public ModOptionsCheckbox(
            string label, 
            int whichOption, 
            Action<bool> toggleOptionDelegate, 
            IDictionary<string, string> options,
            string optionKey, 
            bool defaultValue = true, 
            int x = -1, 
            int y = -1)
            : base(label, x, y, PixelSize * Game1.pixelZoom, PixelSize * Game1.pixelZoom, whichOption)
        {
            this._toggleOptionsDelegate = toggleOptionDelegate;
            this._options = options;
            this._optionKey = optionKey;

            if (!this._options.ContainsKey(this._optionKey))
                this._options[this._optionKey] = defaultValue.ToString();

            this._isChecked = this._options[this._optionKey].SafeParseBool();
            this._toggleOptionsDelegate(this._isChecked);
        }

        public override void ReceiveLeftClick(int x, int y)
        {
            if (this._canClick)
            {
                Game1.playSound("drumkit6");
                base.ReceiveLeftClick(x, y);
                this._isChecked = !this._isChecked;
                this._options[this._optionKey] = this._isChecked.ToString();
                this._toggleOptionsDelegate(this._isChecked);
            }
        }

        public override void Draw(SpriteBatch batch, int slotX, int slotY)
        {
            batch.Draw(Game1.mouseCursors, new Vector2(slotX + this.Bounds.X, slotY + this.Bounds.Y), new Rectangle?(this._isChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked), Color.White * (this._canClick ? 1f : 0.33f), 0.0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0.4f);
            base.Draw(batch, slotX, slotY);
        }
    }
}
