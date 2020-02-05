using System;
using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Reflection;
using Microsoft.Xna.Framework.Input;
using static StardewModdingAPI.Mods.VirtualKeyboard.ModConfig;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class KeyButton
    {
        private readonly IModHelper helper;
        private readonly IMonitor Monitor;
        private readonly Rectangle buttonRectangle;

        private object buttonPressed;
        private object buttonReleased;

        private readonly MethodBase RaiseButtonPressed;
        private readonly MethodBase RaiseButtonReleased;

        private readonly SButton buttonKey;
        private readonly float transparency;
        private readonly string alias;
        public bool hidden;
        private bool raisingPressed = false;
        private bool raisingReleased = false;

        public KeyButton(IModHelper helper, VirtualButton buttonDefine, IMonitor monitor)
        {
            this.Monitor = monitor;
            this.helper = helper;
            this.hidden = true;
            this.buttonRectangle = new Rectangle(buttonDefine.rectangle.X, buttonDefine.rectangle.Y, buttonDefine.rectangle.Width, buttonDefine.rectangle.Height);
            this.buttonKey = buttonDefine.key;

            if (buttonDefine.alias == null)
                this.alias = this.buttonKey.ToString();
            else
                this.alias = buttonDefine.alias;

            if (buttonDefine.transparency <= 0.01f || buttonDefine.transparency > 1f)
            {
                buttonDefine.transparency = 0.5f;
            }
            this.transparency = buttonDefine.transparency;

            helper.Events.Display.RenderingHud += this.OnRenderingHud;
            helper.Events.Input.ButtonReleased += this.EventInputButtonReleased;
            helper.Events.Input.ButtonPressed += this.EventInputButtonPressed;

            object score = this.GetSCore(this.helper);
            object eventManager = score.GetType().GetField("EventManager", BindingFlags.Public | BindingFlags.Instance).GetValue(score);

            this.buttonPressed = eventManager.GetType().GetField("ButtonPressed", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);
            this.buttonReleased = eventManager.GetType().GetField("ButtonReleased", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);

            this.RaiseButtonPressed = this.buttonPressed.GetType().GetMethod("Raise", BindingFlags.Public | BindingFlags.Instance);
            this.RaiseButtonReleased = this.buttonReleased.GetType().GetMethod("Raise", BindingFlags.Public | BindingFlags.Instance);
        }

        private object GetSCore(IModHelper helper)
        {
            MainActivity activity = this.helper.Reflection.GetField<MainActivity>(typeof(MainActivity), "instance").GetValue();
            object score = activity.GetType().GetField("core", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(activity);
            return score;
        }

        private bool shouldTrigger(Vector2 screenPixels)
        {
            if (this.buttonRectangle.Contains(screenPixels.X * Game1.options.zoomLevel, screenPixels.Y * Game1.options.zoomLevel) && !this.hidden)
            {
                if (!this.hidden)
                    Toolbar.toolbarPressed = true;
                return true;
            }
            return false;
        }

        private void EventInputButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (this.raisingPressed)
            {
                return;
            }

            Vector2 screenPixels = e.Cursor.ScreenPixels;
            if (this.shouldTrigger(screenPixels))
            {
                object inputState = e.GetType().GetField("InputState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(e);

                object buttonPressedEventArgs = Activator.CreateInstance(typeof(ButtonPressedEventArgs), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { this.buttonKey, e.Cursor, inputState }, null);
                try
                {
                    this.raisingPressed = true;

                    this.RaiseButtonPressed.Invoke(this.buttonPressed, new object[] { buttonPressedEventArgs });
                }
                finally
                {
                    this.raisingPressed = false;
                }
            }
        }

        private void EventInputButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (this.raisingReleased)
            {
                return;
            }

            Vector2 screenPixels = e.Cursor.ScreenPixels;
            if (this.shouldTrigger(screenPixels))
            {
                if (this.buttonKey == SButton.RightWindows)
                {
                    Game1.activeClickableMenu = new NamingMenu(command =>
                    {
                        if (command.Length > 0)
                        {
                            object score = this.GetSCore(this.helper);
                            object sgame = score.GetType().GetField("GameInstance", BindingFlags.Public | BindingFlags.Instance)?.GetValue(score);
                            ConcurrentQueue<string> commandQueue = sgame.GetType().GetProperty("CommandQueue", BindingFlags.Public | BindingFlags.Instance)?.GetValue(sgame) as ConcurrentQueue<string>;
                            commandQueue?.Enqueue(command);
                            Game1.activeClickableMenu.exitThisMenu();
                        }

                    }, "Command", "")
                    {
                        randomButton = new ClickableTextureComponent(new Rectangle(-100, -100, 0, 0),
                            Game1.mobileSpriteSheet, new Rectangle(87, 22, 20, 20), 4f, false)
                    };
                    return;
                }
                if (this.buttonKey == SButton.RightControl)
                {
                    SGameConsole.Instance.Show();
                    return;
                }
                object inputState = e.GetType().GetField("InputState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(e);
                object buttonReleasedEventArgs = Activator.CreateInstance(typeof(ButtonReleasedEventArgs), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { this.buttonKey, e.Cursor, inputState }, null);
                try
                {
                    this.raisingReleased = true;
                    this.RaiseButtonReleased.Invoke(this.buttonReleased, new object[] { buttonReleasedEventArgs });
                }
                finally
                {
                    this.raisingReleased = false;
                }
            }
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, EventArgs e)
        {
            if (!this.hidden)
            {
                float scale = this.transparency;
                if (!Game1.eventUp && Game1.activeClickableMenu is GameMenu == false && Game1.activeClickableMenu is ShopMenu == false && Game1.activeClickableMenu is IClickableMenu == false)
                {
                    scale *= 0.5f;
                }
                IClickableMenu.drawButtonWithText(Game1.spriteBatch, Game1.smallFont, this.alias, this.buttonRectangle.X, this.buttonRectangle.Y, this.buttonRectangle.Width, this.buttonRectangle.Height, Color.BurlyWood * scale);
            }
        }
    }
}
