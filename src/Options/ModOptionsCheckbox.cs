using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;

namespace SMDroid.Options
{
    public class ModOptionsCheckbox : OptionsCheckbox
    {
        Action<bool> onChanged;
        public ModOptionsCheckbox(string label, int whichOption, Action<bool> onChanged, int x = -1, int y = -1) : base(label, whichOption, x, y)
        {
            this.onChanged = onChanged;
        }
        public override void receiveLeftClick(int x, int y)
        {
            base.receiveLeftClick(x, y);
            this.onChanged(this.isChecked);
        }
        public override void receiveKeyPress(Keys key)
        {
            base.receiveKeyPress(key);
            this.onChanged(this.isChecked);
        }
        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);
            this.onChanged(this.isChecked);
        }
    }
}
