using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;

namespace UIInfoSuite.Options
{
    public class ModOptionsPage : IClickableMenu
    {
        private const int Width = 800;

        private List<ClickableComponent> _optionSlots = new List<ClickableComponent>();
        private List<ModOptionsElement> _options;
        private string _hoverText;
        private int _optionsSlotHeld;
        private int _currentItemIndex;
        private bool _isScrolling;
        private ClickableTextureComponent _upArrow;
        private ClickableTextureComponent _downArrow;
        private ClickableTextureComponent _scrollBar;
        private Rectangle _scrollBarRunner;

        public ModOptionsPage(List<ModOptionsElement> options, IModEvents events)
            : base(Game1.activeClickableMenu.xPositionOnScreen, Game1.activeClickableMenu.yPositionOnScreen + 10, Width, Game1.activeClickableMenu.height)
        {
            this._options = options;
            this._upArrow = new ClickableTextureComponent(
                new Rectangle(
                    this.xPositionOnScreen + this.width + Game1.tileSize / 4,
                    this.yPositionOnScreen + Game1.tileSize, 
                    11 * Game1.pixelZoom, 
                    12 * Game1.pixelZoom), 
                Game1.mouseCursors, 
                new Rectangle(421, 459, 11, 12), 
                Game1.pixelZoom);

            this._downArrow = new ClickableTextureComponent(
                new Rectangle(
                    this._upArrow.bounds.X,
                    this.yPositionOnScreen + this.height - Game1.tileSize,
                    this._upArrow.bounds.Width,
                    this._upArrow.bounds.Height),
                Game1.mouseCursors,
                new Rectangle(421, 472, 11, 12),
                Game1.pixelZoom);

            this._scrollBar = new ClickableTextureComponent(
                new Rectangle(
                    this._upArrow.bounds.X + Game1.pixelZoom * 3,
                    this._upArrow.bounds.Y + this._upArrow.bounds.Height + Game1.pixelZoom,
                    6 * Game1.pixelZoom,
                    10 * Game1.pixelZoom),
                Game1.mouseCursors,
                new Rectangle(435, 463, 6, 10),
                Game1.pixelZoom);

            this._scrollBarRunner = new Rectangle(this._scrollBar.bounds.X,
                this._scrollBar.bounds.Y,
                this._scrollBar.bounds.Width,
                this.height - Game1.tileSize * 2 - this._upArrow.bounds.Height - Game1.pixelZoom * 2);

            for (int i = 0; i < 7; ++i)
                this._optionSlots.Add(new ClickableComponent(
                    new Rectangle(
                        this.xPositionOnScreen + Game1.tileSize / 4,
                        this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * (this.height - Game1.tileSize * 2) / 7,
                        this.width - Game1.tileSize / 2,
                        (this.height - Game1.tileSize * 2) / 7 + Game1.pixelZoom),
                    i.ToString()));

            events.Display.MenuChanged += this.OnMenuChanged;
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is GameMenu)
            {
                this.xPositionOnScreen = Game1.activeClickableMenu.xPositionOnScreen;
                this.yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen + 10;
                this.height = Game1.activeClickableMenu.height;

                for (int i = 0; i < this._optionSlots.Count; ++i)
                {
                    ClickableComponent next = this._optionSlots[i];
                    next.bounds.X = this.xPositionOnScreen + Game1.tileSize / 4;
                    next.bounds.Y = this.yPositionOnScreen + Game1.tileSize * 5 / 4 + Game1.pixelZoom + i * (this.height - Game1.tileSize * 2) / 7;
                    next.bounds.Width = this.width - Game1.tileSize / 2;
                    next.bounds.Height = (this.height - Game1.tileSize * 2) / 7 + Game1.pixelZoom;
                }

                this._upArrow.bounds.X = this.xPositionOnScreen + this.width + Game1.tileSize / 4;
                this._upArrow.bounds.Y = this.yPositionOnScreen + Game1.tileSize;
                this._upArrow.bounds.Width = 11 * Game1.pixelZoom;
                this._upArrow.bounds.Height = 12 * Game1.pixelZoom;

                this._downArrow.bounds.X = this._upArrow.bounds.X;
                this._downArrow.bounds.Y = this.yPositionOnScreen + this.height - Game1.tileSize;
                this._downArrow.bounds.Width = this._upArrow.bounds.Width;
                this._downArrow.bounds.Height = this._upArrow.bounds.Height;

                this._scrollBar.bounds.X = this._upArrow.bounds.X + Game1.pixelZoom * 3;
                this._scrollBar.bounds.Y = this._upArrow.bounds.Y + this._upArrow.bounds.Height + Game1.pixelZoom;
                this._scrollBar.bounds.Width = 6 * Game1.pixelZoom;
                this._scrollBar.bounds.Height = 10 * Game1.pixelZoom;

                this._scrollBarRunner.X = this._scrollBar.bounds.X;
                this._scrollBarRunner.Y = this._scrollBar.bounds.Y;
                this._scrollBarRunner.Width = this._scrollBar.bounds.Width;
                this._scrollBarRunner.Height = this.height - Game1.tileSize * 2 - this._upArrow.bounds.Height - Game1.pixelZoom * 2;
            }
        }

        private void SetScrollBarToCurrentItem()
        {
            if (this._options.Count > 0)
            {
                this._scrollBar.bounds.Y = this._scrollBarRunner.Height / Math.Max(1, this._options.Count - 7 + 1) * this._currentItemIndex + this._upArrow.bounds.Bottom + Game1.pixelZoom;

                if (this._currentItemIndex == this._options.Count - 7)
                {
                    this._scrollBar.bounds.Y = this._downArrow.bounds.Y - this._scrollBar.bounds.Height - Game1.pixelZoom;
                }
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.leftClickHeld(x, y);

                if (this._isScrolling)
                {
                    int yBefore = this._scrollBar.bounds.Y;

                    this._scrollBar.bounds.Y = Math.Min(
                        this.yPositionOnScreen + this.height - Game1.tileSize - Game1.pixelZoom * 3 - this._scrollBar.bounds.Height, 
                        Math.Max(
                            y,
                            this.yPositionOnScreen + this._upArrow.bounds.Height + Game1.pixelZoom * 5));

                    this._currentItemIndex = Math.Min(
                        this._options.Count - 7, 
                        Math.Max(
                            0,
                            this._options.Count * (y - this._scrollBarRunner.Y) / this._scrollBarRunner.Height));

                    this.SetScrollBarToCurrentItem();

                    if (yBefore != this._scrollBar.bounds.Y)
                        Game1.playSound("shiny4");
                }
                else if (this._optionsSlotHeld > -1 && this._optionsSlotHeld + this._currentItemIndex < this._options.Count)
                {
                    this._options[this._currentItemIndex + this._optionsSlotHeld].LeftClickHeld(
                        x - this._optionSlots[this._optionsSlotHeld].bounds.X,
                        y - this._optionSlots[this._optionsSlotHeld].bounds.Y);
                }
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (this._optionsSlotHeld > -1 &&
                this._optionsSlotHeld + this._currentItemIndex < this._options.Count)
            {
                this._options[this._currentItemIndex + this._optionsSlotHeld].ReceiveKeyPress(key);
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.receiveScrollWheelAction(direction);

                if (direction > 0 && this._currentItemIndex > 0)
                {
                    this.UpArrowPressed();
                    Game1.playSound("shiny4");
                }
                else if (direction < 0 && this._currentItemIndex < Math.Max(0, this._options.Count - 7))
                {
                    this.DownArrowPressed();
                    Game1.playSound("shiny4");
                }
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.releaseLeftClick(x, y);

                if (this._optionsSlotHeld > -1 && this._optionsSlotHeld + this._currentItemIndex < this._options.Count)
                {
                    ClickableComponent optionSlot = this._optionSlots[this._optionsSlotHeld];
                    this._options[this._currentItemIndex + this._optionsSlotHeld].LeftClickReleased(x - optionSlot.bounds.X, y - optionSlot.bounds.Y);
                }
                this._optionsSlotHeld = -1;
                this._isScrolling = false;
            }
        }

        private void DownArrowPressed()
        {
            this._downArrow.scale = this._downArrow.baseScale;
            ++this._currentItemIndex;
            this.SetScrollBarToCurrentItem();
        }

        private void UpArrowPressed()
        {
            this._upArrow.scale = this._upArrow.baseScale;
            --this._currentItemIndex;
            this.SetScrollBarToCurrentItem();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!GameMenu.forcePreventClose)
            {
                if (this._downArrow.containsPoint(x, y) && this._currentItemIndex < Math.Max(0, this._options.Count - 7))
                {
                    this.DownArrowPressed();
                    Game1.playSound("shwip");
                }
                else if (this._upArrow.containsPoint(x, y) && this._currentItemIndex > 0)
                {
                    this.UpArrowPressed();
                    Game1.playSound("shwip");
                }
                else if (this._scrollBar.containsPoint(x, y))
                {
                    this._isScrolling = true;
                }
                else if (!this._downArrow.containsPoint(x, y) && 
                    x > this.xPositionOnScreen + this.width && 
                    x < this.xPositionOnScreen + this.width + Game1.tileSize * 2 &&
                    y > this.yPositionOnScreen &&
                    y < this.yPositionOnScreen + this.height)
                {
                    this._isScrolling = true;
                    base.leftClickHeld(x, y);
                    base.releaseLeftClick(x, y);
                }
                this._currentItemIndex = Math.Max(0, Math.Min(this._options.Count - 7, this._currentItemIndex));
                for (int i = 0; i < this._optionSlots.Count; ++i)
                {
                    if (this._optionSlots[i].bounds.Contains(x, y) &&
                        this._currentItemIndex + i < this._options.Count &&
                        this._options[this._currentItemIndex + i].Bounds.Contains(x - this._optionSlots[i].bounds.X, y - this._optionSlots[i].bounds.Y))
                    {
                        this._options[this._currentItemIndex + i].ReceiveLeftClick(
                            x - this._optionSlots[i].bounds.X, 
                            y - this._optionSlots[i].bounds.Y);
                        this._optionsSlotHeld = i;
                        break;
                    }
                }
            }
        }


        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            
        }

        public override void receiveGamePadButton(Buttons b)
        {
            if (b == Buttons.A)
            {
                this.receiveLeftClick(Game1.getMouseX(), Game1.getMouseY());
            }
        }

        public override void performHoverAction(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                this._hoverText = "";
                this._upArrow.tryHover(x, y);
                this._downArrow.tryHover(x, y);
                this._scrollBar.tryHover(x, y);
            }
        }

        public override void draw(SpriteBatch batch)
        {
            Game1.drawDialogueBox(this.xPositionOnScreen, this.yPositionOnScreen - 10, this.width, this.height, false, true);
            batch.End();
            batch.Begin(SpriteSortMode.FrontToBack, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
            for (int i = 0; i < this._optionSlots.Count; ++i)
            {
                if (this._currentItemIndex >= 0 &&
                    this._currentItemIndex + i < this._options.Count)
                {
                    this._options[this._currentItemIndex + i].Draw(
                        batch,
                        this._optionSlots[i].bounds.X,
                        this._optionSlots[i].bounds.Y);
                }
            }
            batch.End();
            batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null);
            if (!GameMenu.forcePreventClose)
            {
                this._upArrow.draw(batch);
                this._downArrow.draw(batch);
                if (this._options.Count > 7)
                {
                    IClickableMenu.drawTextureBox(
                        batch, 
                        Game1.mouseCursors, 
                        new Rectangle(403, 383, 6, 6),
                        this._scrollBarRunner.X,
                        this._scrollBarRunner.Y,
                        this._scrollBarRunner.Width,
                        this._scrollBarRunner.Height, 
                        Color.White, 
                        Game1.pixelZoom, 
                        false);
                    this._scrollBar.draw(batch);
                }
            }
            if (this._hoverText != "")
                IClickableMenu.drawHoverText(batch, this._hoverText, Game1.smallFont);

            //if (Game1.options.hardwareCursor)
            //{
            //    Game1.spriteBatch.Draw(
            //        Game1.mouseCursors,
            //        new Vector2(
            //            Game1.getMouseX(),
            //            Game1.getMouseY()),
            //        new Rectangle?(
            //            Game1.getSourceRectForStandardTileSheet(
            //                Game1.mouseCursors,
            //                Game1.mouseCursor,
            //                16,
            //                16)),
            //        Color.White,
            //        0.0f,
            //        Vector2.Zero,
            //        (float)(Game1.pixelZoom + (Game1.dialogueButtonScale / 150.0)),
            //        SpriteEffects.None,
            //        1f);
            //}
        }
    }
}
