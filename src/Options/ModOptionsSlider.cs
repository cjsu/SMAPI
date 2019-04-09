using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace SMDroid.Options
{
    public class ModOptionsSlider : OptionsSlider
    {
        Action<int> onChanged;
        public ModOptionsSlider(string label, int whichOption, Action<int> onChanged, int x = -1, int y = -1) : base(label, whichOption, x, y)
        {
            this.onChanged = onChanged;
        }
        public override void receiveLeftClick(int x, int y)
        {
            base.receiveLeftClick(x, y);
            this.onChanged(this.value);
        }
        public override void receiveKeyPress(Keys key)
        {
            base.receiveKeyPress(key);
            this.onChanged(this.value);
        }
        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);
            this.onChanged(this.value);
        }
    }
}
