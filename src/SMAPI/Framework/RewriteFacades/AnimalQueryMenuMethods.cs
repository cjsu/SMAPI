using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class AnimalQueryMenuMethods : AnimalQueryMenu
    {
        public AnimalQueryMenuMethods(FarmAnimal animal) : base(animal)
        {
        }

        public ClickableTextureComponent AllowReproductionButtonProp
        {
            get
            {
                if (this.allowReproductionButton != null)
                {
                    return new ClickableTextureComponent(this.allowReproductionButton.bounds, Game1.mouseCursors, new Rectangle(0x80, 0x189, 9, 9), this.allowReproductionButton.scale, false);
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    this.allowReproductionButton = new ClickableComponent(value.bounds, "reproButton");
                }
                this.allowReproductionButton = null;
            }
        }

        public ClickableTextureComponent SellButtonProp
        {
            get
            {
                if (this.sellButton != null)
                {
                    return new ClickableTextureComponent(this.sellButton.bounds, Game1.mouseCursors, new Rectangle(0, 0x180, 0x10, 0x10), this.sellButton.scale, false);
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    this.sellButton = new ClickableComponent(value.bounds, "sellButton");
                }
                this.sellButton = null;
            }
        }

        public ClickableTextureComponent MoveHomeButtonProp
        {
            get
            {
                if (this.moveHomeButton != null)
                {
                    return new ClickableTextureComponent(this.moveHomeButton.bounds, Game1.mouseCursors, new Rectangle(0x10, 0x180, 0x10, 0x10), this.moveHomeButton.scale, false);
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    this.moveHomeButton = new ClickableComponent(value.bounds, "moveHomeButton");
                }
                this.moveHomeButton = null;
            }
        }

    }
}
