using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using System;

namespace SkullCavernElevator
{
    public class MyElevatorMenuWithScrollbar : MineElevatorMenu
    {
        // Fields
        private const int SCROLLSTEP = 11;
        private const int ELEVATORSIZE = 0x79;
        public ClickableTextureComponent upArrow;
        public ClickableTextureComponent downArrow;
        public ClickableTextureComponent scrollBar;
        public Rectangle scrollBarRunner;
        private int currentItemIndex;
        private int elevatorStep;
        private int maxElevators;
        private bool scrolling;

        // Methods
        public MyElevatorMenuWithScrollbar(int elevatorStep, double difficulty)
        {
            this.elevatorStep = 5;
            base.initialize(0, 0, 0, 0, true);
            this.elevatorStep = elevatorStep;
            this.maxElevators = (int)(((double)((Game1.player.deepestMineLevel - 120) / elevatorStep)) / difficulty);
            if (((Game1.gameMode == 3) && (Game1.player != null)) && !Game1.eventUp)
            {
                Game1.player.Halt();
                base.elevators.Clear();
                int num = 120;
                base.width = (num > 50) ? (0x1e4 + (IClickableMenu.borderWidth * 2)) : Math.Min((int)(220 + (IClickableMenu.borderWidth * 2)), (int)((num * 0x2c) + (IClickableMenu.borderWidth * 2)));
                base.height = Math.Max((int)(0x40 + (IClickableMenu.borderWidth * 3)), (int)(((((num * 0x2c) / (base.width - IClickableMenu.borderWidth)) * 0x2c) + 0x40) + (IClickableMenu.borderWidth * 3)));
                base.xPositionOnScreen = (Game1.viewport.Width / 2) - (base.width / 2);
                base.yPositionOnScreen = (Game1.viewport.Height / 2) - (base.height / 2);
                Game1.playSound("crystal");
                this.upArrow = new ClickableTextureComponent(new Rectangle((base.xPositionOnScreen + base.width) + 0x10, base.yPositionOnScreen + 0x40, 0x2c, 0x30), Game1.mouseCursors, new Rectangle(0x1a5, 0x1cb, 11, 12), 4f, false);
                this.downArrow = new ClickableTextureComponent(new Rectangle((base.xPositionOnScreen + base.width) + 0x10, (base.yPositionOnScreen + base.height) - 0x40, 0x2c, 0x30), Game1.mouseCursors, new Rectangle(0x1a5, 0x1d8, 11, 12), 4f, false);
                this.scrollBar = new ClickableTextureComponent(new Rectangle(this.upArrow.bounds.X + 12, (this.upArrow.bounds.Y + this.upArrow.bounds.Height) + 4, 0x18, 40), Game1.mouseCursors, new Rectangle(0x1b3, 0x1cf, 6, 10), 4f, false);
                this.scrollBarRunner = new Rectangle(this.scrollBar.bounds.X, (this.upArrow.bounds.Y + this.upArrow.bounds.Height) + 4, this.scrollBar.bounds.Width, ((base.height - 0x80) - this.upArrow.bounds.Height) - 8);
                int x = (base.xPositionOnScreen + IClickableMenu.borderWidth) + ((IClickableMenu.spaceToClearSideBorder * 3) / 4);
                int y = (base.yPositionOnScreen + IClickableMenu.borderWidth) + (IClickableMenu.borderWidth / 3);
                base.elevators.Add(new ClickableComponent(new Rectangle(x, y, 0x2c, 0x2c), "0"));
                int num4 = (x + 0x40) - 20;
                if (num4 > ((base.xPositionOnScreen + base.width) - IClickableMenu.borderWidth))
                {
                    num4 = (base.xPositionOnScreen + IClickableMenu.borderWidth) + ((IClickableMenu.spaceToClearSideBorder * 3) / 4);
                    y += 0x2c;
                }
                for (int i = 1; i <= num; i++)
                {
                    base.elevators.Add(new ClickableComponent(new Rectangle(num4, y, 0x2c, 0x2c), (i * elevatorStep).ToString()));
                    num4 += 0x2c;
                    if (num4 > ((base.xPositionOnScreen + base.width) - IClickableMenu.borderWidth))
                    {
                        num4 = (base.xPositionOnScreen + IClickableMenu.borderWidth) + ((IClickableMenu.spaceToClearSideBorder * 3) / 4);
                        y += 0x2c;
                    }
                }
                base.initializeUpperRightCloseButton();
            }
        }

        private void downArrowPressed()
        {
            this.downArrow.scale = this.downArrow.baseScale;
            this.currentItemIndex += 11;
            if (this.currentItemIndex > (this.maxElevators - 0x79))
            {
                this.currentItemIndex = (this.maxElevators - 0x79) + 1;
            }
            this.setScrollBarToCurrentIndex();
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(base.xPositionOnScreen, (base.yPositionOnScreen - 0x40) + 8, base.width + 0x15, base.height + 0x40, false, true, null, false, false);
            base.upperRightCloseButton.draw(b);
            this.upArrow.draw(b);
            this.downArrow.draw(b);
            IClickableMenu.drawTextureBox(b, Game1.mouseCursors, new Rectangle(0x193, 0x17f, 6, 6), this.scrollBarRunner.X, this.scrollBarRunner.Y, this.scrollBarRunner.Width, this.scrollBarRunner.Height, Color.White, 4f, false);
            this.scrollBar.draw(b);
            for (int i = 0; i < 0x79; i++)
            {
                ClickableComponent elevator = base.elevators[i];
                elevator.name = ((i + this.currentItemIndex) * this.elevatorStep).ToString();
                drawElevator(b, elevator);
            }
            base.drawMouse(b);
        }
        private static void drawElevator(SpriteBatch b, ClickableComponent elevator)
        {
            b.Draw(Game1.mouseCursors, new Vector2((float)(elevator.bounds.X - 4), (float)(elevator.bounds.Y + 4)), new Rectangle((elevator.scale > 1.0) ? 0x10b : 0x100, 0x100, 10, 10), Color.Black * 0.5f, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, 0.865f);
            b.Draw(Game1.mouseCursors, new Vector2((float)elevator.bounds.X, (float)elevator.bounds.Y), new Rectangle((elevator.scale > 1.0) ? 0x10b : 0x100, 0x100, 10, 10), Color.White, 0f, Vector2.Zero, (float)4f, SpriteEffects.None, 0.868f);
            Vector2 position = new Vector2((float)((elevator.bounds.X + 0x10) + (NumberSprite.numberOfDigits(Convert.ToInt32(elevator.name)) * 6)), (float)((elevator.bounds.Y + 0x18) - (NumberSprite.getHeight() / 4)));
            NumberSprite.draw(Convert.ToInt32(elevator.name), b, position, (((Game1.CurrentMineLevel == (Convert.ToInt32(elevator.name) + 120)) && Game1.currentLocation == (Game1.mine)) || ((Convert.ToInt32(elevator.name) == 0) && Game1.currentLocation != (Game1.mine))) ? (Color.Gray * 0.75f) : Color.Gold, 0.5f, 0.86f, 1f, 0, 0);
        }
        public override void leftClickHeld(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.leftClickHeld(x, y);
                if (this.scrolling)
                {
                    int y2 = scrollBar.bounds.Y;
                    scrollBar.bounds.Y = Math.Min(base.yPositionOnScreen + base.height - 64 - 12 - scrollBar.bounds.Height, Math.Max(y, base.yPositionOnScreen + upArrow.bounds.Height + 20));
                    currentItemIndex = Math.Min(maxElevators - 121 + 1, Math.Max(0, (int)((double)(maxElevators - 121) * (double)((float)(y - scrollBarRunner.Y) / (float)scrollBarRunner.Height))));
                    setScrollBarToCurrentIndex();
                    int y3 = scrollBar.bounds.Y;
                    if (y2 != y3)
                    {
                        Game1.playSound("shiny4");
                    }
                }
            }
        }
        public override void performHoverAction(int x, int y)
        {
            if (!GameMenu.forcePreventClose)
            {
                this.upArrow.tryHover(x, y, 0.1f);
                this.downArrow.tryHover(x, y, 0.1f);
                this.scrollBar.tryHover(x, y, 0.1f);
                foreach (ClickableComponent local1 in base.elevators)
                {
                    local1.scale = !local1.containsPoint(x, y) ? 1f : 2f;
                }
            }
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (downArrow.containsPoint(x, y))
            {
                if (currentItemIndex < Math.Max(0, maxElevators - 121))
                {
                    downArrowPressed();
                    Game1.playSound("shwip");
                }
            }
            else if (upArrow.containsPoint(x, y))
            {
                if (currentItemIndex > 0)
                {
                    upArrowPressed();
                    Game1.playSound("shwip");
                }
            }
            else if (scrollBar.containsPoint(x, y))
            {
                scrolling = true;
            }
            else if (!downArrow.containsPoint(x, y) && x > base.xPositionOnScreen + base.width && x < base.xPositionOnScreen + base.width + 128 && y > base.yPositionOnScreen && y < base.yPositionOnScreen + base.height)
            {
                scrolling = true;
                this.leftClickHeld(x, y);
                this.releaseLeftClick(x, y);
            }
            else if (this.isWithinBounds(x, y))
            {
                bool flag = false;
                foreach (ClickableComponent elevator in base.elevators)
                {
                    if (elevator.containsPoint(x, y))
                    {
                        MineShaft mineShaft = (Game1.currentLocation as MineShaft);
                        if (((mineShaft != null) ? new int?(mineShaft.mineLevel) : null) == Convert.ToInt32(elevator.name) + 120)
                        {
                            return;
                        }
                        Game1.playSound("smallSelect");
                        if (Convert.ToInt32(elevator.name) == 0)
                        {
                            if ((Game1.currentLocation)!=(Game1.mine))
                            {
                                return;
                            }
                            Game1.warpFarmer("SkullCave", 3, 4, 2);
                            Game1.exitActiveMenu();
                            Game1.changeMusicTrack("none");
                            flag = true;
                        }
                        else
                        {
                            if ((Game1.currentLocation)==(Game1.mine) && Convert.ToInt32(elevator.name) == Game1.mine.mineLevel)
                            {
                                return;
                            }
                            Game1.player.ridingMineElevator = true;
                            Game1.enterMine(Convert.ToInt32(elevator.name) + 120);
                            Game1.exitActiveMenu();
                            flag = true;
                        }
                    }
                }
                if (!flag)
                {
                    this.receiveLeftClick(x, y, true);
                }
            }
            else
            {
                Game1.exitActiveMenu();
            }
        }
        public override void receiveScrollWheelAction(int direction)
        {
            if (!GameMenu.forcePreventClose)
            {
                base.receiveScrollWheelAction(direction);
                if ((direction > 0) && (this.currentItemIndex > 0))
                {
                    this.upArrowPressed();
                    Game1.playSound("shiny4");
                }
                else if ((direction < 0) && (this.currentItemIndex < Math.Max(0, this.maxElevators - 0x79)))
                {
                    this.downArrowPressed();
                    Game1.playSound("shiny4");
                }
            }
        }
        private void setScrollBarToCurrentIndex()
        {
            if (base.elevators.Count > 0)
            {
                this.scrollBar.bounds.Y = (((int)((((double)this.scrollBarRunner.Height) / ((double)Math.Max(1, (this.maxElevators - 0x79) + 1))) * this.currentItemIndex)) + this.upArrow.bounds.Bottom) + 4;
                if (this.currentItemIndex == ((this.maxElevators - 0x79) + 1))
                {
                    this.scrollBar.bounds.Y = (this.downArrow.bounds.Y - this.scrollBar.bounds.Height) - 4;
                }
            }
        }
        private void upArrowPressed()
        {
            this.upArrow.scale = this.upArrow.baseScale;
            this.currentItemIndex -= 11;
            if (this.currentItemIndex < 0)
            {
                this.currentItemIndex = 0;
            }
            this.setScrollBarToCurrentIndex();
        }
    }
}

