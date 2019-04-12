using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using StardewModdingAPI;

namespace UIInfoSuite.UIElements
{
    class ShowCalendarAndBillboardOnGameMenuButton : IDisposable
    {
        private ClickableTextureComponent _showBillboardButton = 
            new ClickableTextureComponent(
                new Rectangle(0, 0, 99, 60), 
                Game1.content.Load<Texture2D>(Path.Combine("Maps", "summer_town")), 
                new Rectangle(122, 291, 35, 20), 
                3f);

        private readonly IModHelper _helper;

        private Item _hoverItem = null;
        private Item _heldItem = null;

        public ShowCalendarAndBillboardOnGameMenuButton(IModHelper helper)
        {
            this._helper = helper;
        }

        public void ToggleOption(bool showCalendarAndBillboard)
        {
            this._helper.Events.Display.RenderedActiveMenu -= this.OnRenderedActiveMenu;
            this._helper.Events.Input.ButtonPressed -= this.OnButtonPressed;
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;

            if (showCalendarAndBillboard)
            {
                this._helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
                this._helper.Events.Input.ButtonPressed += this.OnButtonPressed;
                this._helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, EventArgs e)
        {
            // get hover item
            this._hoverItem = Tools.GetHoveredItem();
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                List<IClickableMenu> menuList = gameMenu.pages;

                if (menuList[0] is InventoryPage inventory)
                {
                    this._heldItem = Game1.player.CursorSlotItem;
                }
            }
        }

        public void Dispose()
        {
            this.ToggleOption(false);
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.MouseLeft)
                this.ActivateBillboard();
            else if (e.Button == SButton.ControllerA)
                this.ActivateBillboard();
        }

        private void ActivateBillboard()
        {
            if (Game1.activeClickableMenu is GameMenu &&
                (Game1.activeClickableMenu as GameMenu).currentTab == 0 &&
                this._showBillboardButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                && this._heldItem == null)
            {
                if (Game1.questOfTheDay != null &&
                    string.IsNullOrEmpty(Game1.questOfTheDay.currentObjective))
                    Game1.questOfTheDay.currentObjective = "wat?";

                Game1.activeClickableMenu =
                    new Billboard(!(Game1.getMouseX() <
                    this._showBillboardButton.bounds.X + this._showBillboardButton.bounds.Width / 2));
            }
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedActiveMenu(object sender, EventArgs e)
        {
            if (this._hoverItem == null &&
                Game1.activeClickableMenu is GameMenu gameMenu &&
                gameMenu.currentTab == 0
                && this._heldItem == null)
            {
                this._showBillboardButton.bounds.X = Game1.activeClickableMenu.xPositionOnScreen + Game1.activeClickableMenu.width - 160;

                this._showBillboardButton.bounds.Y = Game1.activeClickableMenu.yPositionOnScreen + Game1.activeClickableMenu.height - 300;
                this._showBillboardButton.draw(Game1.spriteBatch);
                if (this._showBillboardButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    string hoverText = Game1.getMouseX() <
                        this._showBillboardButton.bounds.X + this._showBillboardButton.bounds.Width / 2 ? 
                        LanguageKeys.Calendar : LanguageKeys.Billboard;
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        this._helper.SafeGetString(hoverText),
                        Game1.dialogueFont);
                }
            }
        }
    }
}
