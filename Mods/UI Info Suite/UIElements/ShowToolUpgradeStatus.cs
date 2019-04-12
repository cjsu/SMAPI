using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowToolUpgradeStatus : IDisposable
    {
        private readonly IModHelper _helper;
        private Rectangle _toolTexturePosition;
        private string _hoverText;
        private Tool _toolBeingUpgraded;
        private ClickableTextureComponent _toolUpgradeIcon;

        public ShowToolUpgradeStatus(IModHelper helper)
        {
            this._helper = helper;
        }

        public void ToggleOption(bool showToolUpgradeStatus)
        {
            this._helper.Events.Display.RenderingHud -= this.OnRenderingHud;
            this._helper.Events.Display.RenderedHud -= this.OnRenderedHud;
            this._helper.Events.GameLoop.DayStarted -= this.OnDayStarted;
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;

            if (showToolUpgradeStatus)
            {
                this.UpdateToolInfo();
                this._helper.Events.Display.RenderingHud += this.OnRenderingHud;
                this._helper.Events.Display.RenderedHud += this.OnRenderedHud;
                this._helper.Events.GameLoop.DayStarted += this.OnDayStarted;
                this._helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (e.IsOneSecond && this._toolBeingUpgraded != Game1.player.toolBeingUpgraded.Value)
                this.UpdateToolInfo();
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            this.UpdateToolInfo();
        }

        private void UpdateToolInfo()
        {
            // 
            if (Game1.player.toolBeingUpgraded.Value != null)
            {
                this._toolBeingUpgraded = Game1.player.toolBeingUpgraded.Value;
                this._toolTexturePosition = new Rectangle();

                if (this._toolBeingUpgraded is StardewValley.Tools.WateringCan)
                {
                    this._toolTexturePosition.X = 32;
                    this._toolTexturePosition.Y = 228;
                    this._toolTexturePosition.Width = 16;
                    this._toolTexturePosition.Height = 11;
                }
                else
                {
                    this._toolTexturePosition.Width = 16;
                    this._toolTexturePosition.Height = 16;
                    this._toolTexturePosition.X = 81;
                    this._toolTexturePosition.Y = 31;

                    if (!(this._toolBeingUpgraded is StardewValley.Tools.Hoe))
                    {
                        this._toolTexturePosition.Y += 64;

                        if (!(this._toolBeingUpgraded is StardewValley.Tools.Pickaxe))
                        {
                            this._toolTexturePosition.Y += 64;
                        }
                    }
                }

                this._toolTexturePosition.X += (111 * this._toolBeingUpgraded.UpgradeLevel);

                if (this._toolTexturePosition.X > Game1.toolSpriteSheet.Width)
                {
                    this._toolTexturePosition.Y += 32;
                    this._toolTexturePosition.X -= 333;
                }

                if (Game1.player.daysLeftForToolUpgrade.Value > 0)
                {
                    this._hoverText = string.Format(this._helper.SafeGetString(LanguageKeys.DaysUntilToolIsUpgraded),
                        Game1.player.daysLeftForToolUpgrade.Value, this._toolBeingUpgraded.DisplayName);
                }
                else
                {
                    this._hoverText = string.Format(this._helper.SafeGetString(LanguageKeys.ToolIsFinishedBeingUpgraded),
                        this._toolBeingUpgraded.DisplayName);
                }
            }
            else
            {
                this._toolBeingUpgraded = null;
            }
            
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open). Content drawn to the sprite batch at this point will appear under the HUD.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw tool upgrade status
            if (!Game1.eventUp && this._toolBeingUpgraded != null)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                this._toolUpgradeIcon =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.toolSpriteSheet,
                        this._toolTexturePosition,
                        2.5f);
                this._toolUpgradeIcon.draw(Game1.spriteBatch);
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (this._toolBeingUpgraded != null && this._toolUpgradeIcon.containsPoint((int)(Game1.getMouseX() * Game1.options.zoomLevel), (int)(Game1.getMouseY() * Game1.options.zoomLevel)))
            {
                IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        this._hoverText, Game1.dialogueFont);
            }
        }

        public void Dispose()
        {
            this.ToggleOption(false);
            this._toolBeingUpgraded = null;
        }
    }
}
