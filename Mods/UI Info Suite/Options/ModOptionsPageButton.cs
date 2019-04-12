using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace UIInfoSuite.Options
{
    class ModOptionsPageButton : IClickableMenu
    {

        public Rectangle Bounds { get; }
        //private readonly ModOptionsPageHandler _optionsPageHandler;
        //private bool _hasClicked;

        public event EventHandler OnLeftClicked;

        public ModOptionsPageButton(IModEvents events)
        {
            //_optionsPageHandler = optionsPageHandler;
            this.width = 64;
            this.height = 64;
            GameMenu activeClickableMenu = Game1.activeClickableMenu as GameMenu;

            this.xPositionOnScreen = activeClickableMenu.xPositionOnScreen + activeClickableMenu.width - 200;
            this.yPositionOnScreen = activeClickableMenu.yPositionOnScreen + 16;
            this.Bounds = new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.width, this.height);
            events.Input.ButtonPressed += this.OnButtonPressed;
            events.Display.MenuChanged += this.OnMenuChanged;
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is GameMenu menu)
            {
                this.xPositionOnScreen = menu.xPositionOnScreen + menu.width - 200;
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        public void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA)
            {
                int x = (int)e.Cursor.ScreenPixels.X;
                int y = (int)e.Cursor.ScreenPixels.Y;
                if (this.isWithinBounds(x, y))
                {
                    this.receiveLeftClick(x, y);
                    OnLeftClicked?.Invoke(this, null);
                }
            }

            //if (e.NewState.LeftButton != ButtonState.Pressed || !(Game1.activeClickableMenu is GameMenu))
            //{
            //    _hasClicked = false;
            //}
            //else if ((Game1.activeClickableMenu as GameMenu).currentTab != 3 && 
            //    isWithinBounds(e.NewPosition.X, e.NewPosition.Y) && 
            //    !_hasClicked)
            //{
            //    receiveLeftClick(e.NewPosition.X, e.NewPosition.Y);

            //}
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            Game1.spriteBatch.Draw(Game1.mouseCursors, 
                new Vector2(this.xPositionOnScreen, this.yPositionOnScreen), 
                new Rectangle(16, 368, 16, 16), 
                Color.White, 
                0.0f, 
                Vector2.Zero, 
                Game1.pixelZoom, 
                SpriteEffects.None, 
                1f);

            b.Draw(Game1.mouseCursors, 
                new Vector2(this.xPositionOnScreen + 8, this.yPositionOnScreen + 14), 
                new Rectangle(32, 672, 16, 16), 
                Color.White, 
                0.0f, 
                Vector2.Zero, 
                3f, 
                SpriteEffects.None, 
                1f);

            if (this.isWithinBounds(Game1.getMouseX(), Game1.getMouseY()))
            {
                IClickableMenu.drawHoverText(Game1.spriteBatch, "UI Info Mod Options", Game1.smallFont);
            }
            Tools.DrawMouseCursor();
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }
    }
}
