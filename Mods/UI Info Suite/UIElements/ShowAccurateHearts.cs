using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UIInfoSuite.UIElements
{
    class ShowAccurateHearts : IDisposable
    {
        private string[] _friendNames;
        private SocialPage _socialPage;
        private IModEvents _events;

        private readonly int[][] _numArray = new int[][]
        {
            new int[] { 1, 1, 0, 1, 1 },
            new int[] { 1, 1, 1, 1, 1 },
            new int[] { 0, 1, 1, 1, 0 },
            new int[] { 0, 0, 1, 0, 0 }
        };

        public ShowAccurateHearts(IModEvents events)
        {
            this._events = events;
        }

        public void ToggleOption(bool showAccurateHearts)
        {
            this._events.Display.MenuChanged -= this.OnMenuChanged;
            this._events.Display.RenderedActiveMenu -= this.OnRenderedActiveMenu;

            if (showAccurateHearts)
            {
                this._events.Display.MenuChanged += this.OnMenuChanged;
                this._events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            }
        }

        public void Dispose()
        {
            this.ToggleOption(false);
        }

        /// <summary>When a menu is open (<see cref="Game1.activeClickableMenu"/> isn't null), raised after that menu is drawn to the sprite batch but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedActiveMenu(object sender, RenderedActiveMenuEventArgs e)
        {
            // draw heart fills
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                if (gameMenu.currentTab == 2)
                {
                    if (this._socialPage != null)
                    {
                        int slotPosition = (int)typeof(SocialPage)
                            .GetField(
                                "slotPosition",
                                BindingFlags.Instance | BindingFlags.NonPublic)
                                .GetValue(this._socialPage);
                        int yOffset = 0;

                        for (int i = slotPosition; i < slotPosition + 5 && i < this._friendNames.Length; ++i)
                        {
                            int yPosition = Game1.activeClickableMenu.yPositionOnScreen + 130 + yOffset;
                            yOffset += 112;
                            Friendship friendshipValues;
                            string nextName = this._friendNames[i];
                            if (Game1.player.friendshipData.TryGetValue(nextName, out friendshipValues))
                            {
                                int friendshipRawValue = friendshipValues.Points;

                                if (friendshipRawValue > 0)
                                {
                                    int pointsToNextHeart = friendshipRawValue % 250;
                                    int numHearts = friendshipRawValue / 250;

                                    if (friendshipRawValue < 3000 &&
                                        this._friendNames[i] == Game1.player.spouse ||
                                        friendshipRawValue < 2500)
                                    {
                                        this.DrawEachIndividualSquare(numHearts, pointsToNextHeart, yPosition);
                                        //if (!Game1.options.hardwareCursor)
                                        //    Game1.spriteBatch.Draw(
                                        //        Game1.mouseCursors,
                                        //        new Vector2(Game1.getMouseX(), Game1.getMouseY()),
                                        //        Game1.getSourceRectForStandardTileSheet(
                                        //            Game1.mouseCursors, Game1.mouseCursor,
                                        //            16,
                                        //            16),
                                        //        Color.White,
                                        //        0.0f,
                                        //        Vector2.Zero,
                                        //        Game1.pixelZoom + (float)(Game1.dialogueButtonScale / 150.0),
                                        //        SpriteEffects.None,
                                        //        1f);
                                    }
                                }
                            }
                        }

                        string hoverText = typeof(GameMenu).GetField("hoverText", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameMenu) as string;
                        IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            hoverText,
                            Game1.smallFont);
                    }
                    else
                    {
                        this.ExtendMenuIfNeeded();
                    }
                }
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            this.ExtendMenuIfNeeded();
        }

        private void ExtendMenuIfNeeded()
        {
            if (Game1.activeClickableMenu is GameMenu gameMenu)
            {
                List<IClickableMenu> menuList = gameMenu.pages;

                foreach (IClickableMenu menu in menuList)
                {
                    if (menu is SocialPage page)
                    {
                        this._socialPage = page;
                        this._friendNames = (typeof(SocialPage).GetField("names", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this._socialPage) as List<object>)
                            .Select(name => name.ToString())
                            .ToArray();
                        break;
                    }
                }
            }
        }

        private void DrawEachIndividualSquare(int friendshipLevel, int friendshipPoints, int yPosition)
        {
            int numberOfPointsToDraw = (int)(((double)friendshipPoints) / 12.5);
            int num2;

            if (friendshipLevel > 10)
            {
                num2 = 32 * (friendshipLevel - 10);
                yPosition += 28;
            }
            else
            {
                num2 = 32 * friendshipLevel;
            }

            for (int i = 3; i >= 0 && numberOfPointsToDraw > 0; --i)
            {
                for (int j = 0; j < 5 && numberOfPointsToDraw > 0; ++j, --numberOfPointsToDraw)
                {
                    if (this._numArray[i][j] == 1)
                    {
                        Game1.spriteBatch.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                Game1.activeClickableMenu.xPositionOnScreen + 316 + num2 + j * 4,
                                yPosition + 14 + i * 4,
                                4,
                                4),
                            Color.Crimson);
                    }
                }
            }
        }
    }
}
