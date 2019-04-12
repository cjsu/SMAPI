using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class LuckOfDay : IDisposable
    {
        private string _hoverText = string.Empty;
        private Color _color = new Color(Color.White.ToVector4());
        private ClickableTextureComponent _icon;
        private readonly IModHelper _helper;

        public void Toggle(bool showLuckOfDay)
        {
            this._helper.Events.Player.Warped -= this.OnWarped;
            this._helper.Events.Display.RenderingHud -= this.OnRenderingHud;
            this._helper.Events.Display.RenderedHud -= this.OnRenderedHud;
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;

            if (showLuckOfDay)
            {
                this.AdjustIconXToBlackBorder();
                this._helper.Events.Player.Warped += this.OnWarped;
                this._helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
                this._helper.Events.Display.RenderingHud += this.OnRenderingHud;
                this._helper.Events.Display.RenderedHud += this.OnRenderedHud;
            }
        }

        public LuckOfDay(IModHelper helper)
        {
            this._helper = helper;
        }

        public void Dispose()
        {
            this.Toggle(false);
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // calculate luck
            if (e.IsMultipleOf(30)) // half second
            {
                this._color = new Color(Color.White.ToVector4());

                if (Game1.dailyLuck < -0.04)
                {
                    this._hoverText = this._helper.SafeGetString(LanguageKeys.MaybeStayHome);
                    this._color.B = 155;
                    this._color.G = 155;
                }
                else if (Game1.dailyLuck < 0)
                {
                    this._hoverText = this._helper.SafeGetString(LanguageKeys.NotFeelingLuckyAtAll);
                    this._color.B = 165;
                    this._color.G = 165;
                    this._color.R = 165;
                    this._color *= 0.8f;
                }
                else if (Game1.dailyLuck <= 0.04)
                {
                    this._hoverText = this._helper.SafeGetString(LanguageKeys.LuckyButNotTooLucky);
                }
                else
                {
                    this._hoverText = this._helper.SafeGetString(LanguageKeys.FeelingLucky);
                    this._color.B = 155;
                    this._color.R = 155;
                }
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (this._icon.containsPoint((int)(Game1.getMouseX() * Game1.options.zoomLevel), (int)(Game1.getMouseY() * Game1.options.zoomLevel)))
                IClickableMenu.drawHoverText(Game1.spriteBatch, this._hoverText, Game1.dialogueFont);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw dice icon
            if (!Game1.eventUp && Game1.activeClickableMenu == null)
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                this._icon.bounds.X = iconPosition.X;
                this._icon.bounds.Y = iconPosition.Y;
                this._icon.draw(Game1.spriteBatch, this._color, 1f);

            }
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            // adjust icon X to black border
            if (e.IsLocalPlayer)
            {
                this.AdjustIconXToBlackBorder();
            }
        }

        private void AdjustIconXToBlackBorder()
        {
            this._icon = new ClickableTextureComponent("",
                new Rectangle(Tools.GetWidthInPlayArea() - 174,
                    320,
                    10 * 6,
                    10 * 6),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(50, 428, 10, 14),
                6,
                false);
        }
    }
}
