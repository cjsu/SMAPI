using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;

namespace UIInfoSuite.UIElements
{
    class ShowBirthdayIcon : IDisposable
    {
        private NPC _birthdayNPC;
        private ClickableTextureComponent _birthdayIcon;
        private readonly IModEvents _events;

        public ShowBirthdayIcon(IModEvents events)
        {
            this._events = events;
        }

        public void ToggleOption(bool showBirthdayIcon)
        {
            this._events.GameLoop.DayStarted -= this.OnDayStarted;
            this._events.Display.RenderingHud -= this.OnRenderingHud;
            this._events.Display.RenderedHud -= this.OnRenderedHud;
            this._events.GameLoop.UpdateTicked -= this.OnUpdateTicked;

            if (showBirthdayIcon)
            {
                this.CheckForBirthday();
                this._events.GameLoop.DayStarted += this.OnDayStarted;
                this._events.Display.RenderingHud += this.OnRenderingHud;
                this._events.Display.RenderedHud += this.OnRenderedHud;
                this._events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // check if gift has been given
            if (e.IsOneSecond && this._birthdayNPC != null && Game1.player?.friendshipData != null)
            {
                Game1.player.friendshipData.FieldDict.TryGetValue(this._birthdayNPC.Name, out Netcode.NetRef<Friendship> netRef);
                //var birthdayNPCDetails = Game1.player.friendshipData.SafeGet(_birthdayNPC.name);
                Friendship birthdayNPCDetails = netRef;
                if (birthdayNPCDetails != null)
                {
                    if (birthdayNPCDetails.GiftsToday == 1)
                        this._birthdayNPC = null;
                }
            }
        }

        public void Dispose()
        {
            this.ToggleOption(false);
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            this.CheckForBirthday();
        }

        private void CheckForBirthday()
        {
            this._birthdayNPC = null;
            foreach (GameLocation location in Game1.locations)
            {
                foreach (NPC character in location.characters)
                {
                    if (character.isBirthday(Game1.currentSeason, Game1.dayOfMonth))
                    {
                        this._birthdayNPC = character;
                        break;
                    }
                }
                
                if (this._birthdayNPC != null)
                    break;
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, EventArgs e)
        {
            // draw birthday icon
            if (!Game1.eventUp)
            {
                if (this._birthdayNPC != null)
                {
                    Rectangle headShot = this._birthdayNPC.GetHeadShot();
                    Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                    float scale = 2.9f;

                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        new Vector2(iconPosition.X, iconPosition.Y),
                        new Rectangle(228, 409, 16, 16),
                        Color.White,
                        0.0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        1f);

                    this._birthdayIcon =
                        new ClickableTextureComponent(
                            this._birthdayNPC.Name,
                            new Rectangle(
                                iconPosition.X - 7,
                                iconPosition.Y - 2,
                                (int)(16.0 * scale),
                                (int)(16.0 * scale)),
                            null,
                            this._birthdayNPC.Name,
                            this._birthdayNPC.Sprite.Texture,
                            headShot,
                            2f);

                    this._birthdayIcon.draw(Game1.spriteBatch);
                }
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (this._birthdayNPC != null && 
                (this._birthdayIcon?.containsPoint((int)(Game1.getMouseX() * Game1.options.zoomLevel), (int)(Game1.getMouseY() * Game1.options.zoomLevel)) ?? false))
            {
                string hoverText = string.Format("{0}'s Birthday", this._birthdayNPC.Name);
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    hoverText,
                    Game1.dialogueFont);
            }
        }
    }
}
