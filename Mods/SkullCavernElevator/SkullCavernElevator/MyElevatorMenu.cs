using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using System;

namespace SkullCavernElevator.SkullCavernElevator
{
    public class MyElevatorMenu : MineElevatorMenu
    {
        // Methods
        public MyElevatorMenu(int elevatorStep, double difficulty)
        {
            base.initialize(0, 0, 0, 0, true);
            if (((Game1.gameMode == 3) && (Game1.player != null)) && !Game1.eventUp)
            {
                Game1.player.Halt();
                base.elevators.Clear();
                int num = (int)(((double)((Game1.player.deepestMineLevel - 120) / elevatorStep)) / difficulty);
                base.width = (num > 50) ? (0x1e4 + (IClickableMenu.borderWidth * 2)) : Math.Min((int)(220 + (IClickableMenu.borderWidth * 2)), (int)((num * 0x2c) + (IClickableMenu.borderWidth * 2)));
                base.height = Math.Max((int)(0x40 + (IClickableMenu.borderWidth * 3)), (int)(((((num * 0x2c) / (base.width - IClickableMenu.borderWidth)) * 0x2c) + 0x40) + (IClickableMenu.borderWidth * 3)));
                base.xPositionOnScreen = (Game1.viewport.Width / 2) - (base.width / 2);
                base.yPositionOnScreen = (Game1.viewport.Height / 2) - (base.height / 2);
                Game1.playSound("crystal");
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
                    num4 = (num4 + 0x40) - 20;
                    if (num4 > ((base.xPositionOnScreen + base.width) - IClickableMenu.borderWidth))
                    {
                        num4 = (base.xPositionOnScreen + IClickableMenu.borderWidth) + ((IClickableMenu.spaceToClearSideBorder * 3) / 4);
                        y += 0x2c;
                    }
                }
                base.initializeUpperRightCloseButton();
            }
        }
        public override void draw(SpriteBatch b)
        {
            base.draw(b);
            foreach (ClickableComponent component in base.elevators)
            {
                Vector2 position = new Vector2((float)((component.bounds.X + 0x10) + (NumberSprite.numberOfDigits(Convert.ToInt32(component.name)) * 6)), (float)((component.bounds.Y + 0x18) - (NumberSprite.getHeight() / 4)));
                NumberSprite.draw(Convert.ToInt32(component.name), b, position, (((Game1.CurrentMineLevel == (Convert.ToInt32(component.name) + 120)) && Game1.currentLocation == Game1.mine) || ((Convert.ToInt32(component.name) == 0) && Game1.currentLocation != Game1.mine)) ? (Color.Gray * 0.75f) : Color.Gold, 0.5f, 0.86f, 1f, 0, 0);
            }
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.isWithinBounds(x, y))
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
                            if (Game1.currentLocation != Game1.mine)
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
                            if ((Game1.currentLocation == Game1.mine) && Convert.ToInt32(elevator.name) == Game1.mine.mineLevel)
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
                    base.receiveLeftClick(x, y, true);
                }
            }
            else
            {
                Game1.exitActiveMenu();
            }
        }
    }

}

