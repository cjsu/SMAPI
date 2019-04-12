using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowTravelingMerchant : IDisposable
    {
        private bool _travelingMerchantIsHere = false;
        private ClickableTextureComponent _travelingMerchantIcon;
        private readonly IModHelper _helper;

        public void ToggleOption(bool showTravelingMerchant)
        {
            this._helper.Events.Display.RenderingHud -= this.OnRenderingHud;
            this._helper.Events.Display.RenderedHud -= this.OnRenderedHud;
            this._helper.Events.GameLoop.DayStarted -= this.OnDayStarted;

            if (showTravelingMerchant)
            {
                this.UpdateTravelingMerchant();
                this._helper.Events.Display.RenderingHud += this.OnRenderingHud;
                this._helper.Events.Display.RenderedHud += this.OnRenderedHud;
                this._helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            }
        }


        public ShowTravelingMerchant(IModHelper helper)
        {
            this._helper = helper;
        }

        public void Dispose()
        {
            this.ToggleOption(false);
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, EventArgs e)
        {
            this.UpdateTravelingMerchant();
        }

        private void UpdateTravelingMerchant()
        {
            int dayOfWeek = Game1.dayOfMonth % 7;
            this._travelingMerchantIsHere = dayOfWeek == 0 || dayOfWeek == 5;
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw traveling merchant
            if (!Game1.eventUp && this._travelingMerchantIsHere)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                this._travelingMerchantIcon = 
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40), 
                        Game1.mouseCursors, 
                        new Rectangle(192, 1411, 20, 20), 
                        2f);
                this._travelingMerchantIcon.draw(Game1.spriteBatch);
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (this._travelingMerchantIsHere && this._travelingMerchantIcon.containsPoint((int)(Game1.getMouseX() * Game1.options.zoomLevel), (int)(Game1.getMouseY() * Game1.options.zoomLevel)))
            {
                string hoverText = this._helper.SafeGetString(
                    LanguageKeys.TravelingMerchantIsInTown);
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    hoverText, Game1.dialogueFont);
            }
        }
    }
}
