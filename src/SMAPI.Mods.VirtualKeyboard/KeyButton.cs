using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using static StardewModdingAPI.Mods.VirtualKeyboard.ModConfig;
using System.Reflection;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Mods.VirtualKeyboard
{
    class KeyButton
    {
        private readonly IModHelper helper;
        private readonly IMonitor Monitor;
        private readonly Rectangle buttonRectangle;

        private object buttonPressed;
        private object buttonReleased;
        private object legacyButtonPressed;
        private object legacyButtonReleased;

        private readonly MethodBase RaiseButtonPressed;
        private readonly MethodBase RaiseButtonReleased;
        private readonly MethodBase Legacy_KeyPressed;
        private readonly MethodBase Legacy_KeyReleased;

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

            MainActivity activity = this.helper.Reflection.GetField<MainActivity>(typeof(MainActivity), "instance").GetValue();
            object score = activity.GetType().GetField("core", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(activity);
            object eventManager = score.GetType().GetField("EventManager", BindingFlags.Public | BindingFlags.Instance).GetValue(score);

            this.buttonPressed = eventManager.GetType().GetField("ButtonPressed", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);
            this.buttonReleased = eventManager.GetType().GetField("ButtonReleased", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);

            this.RaiseButtonPressed = this.buttonPressed.GetType().GetMethod("Raise", BindingFlags.Public | BindingFlags.Instance);
            this.RaiseButtonReleased = this.buttonReleased.GetType().GetMethod("Raise", BindingFlags.Public | BindingFlags.Instance);
        }

        private bool shouldTrigger(Vector2 screenPixels)
        {
            if (this.buttonRectangle.Contains(screenPixels.X * Game1.options.zoomLevel, screenPixels.Y * Game1.options.zoomLevel))
            {
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
            if (this.shouldTrigger(screenPixels) && !this.hidden)
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
            if (!Game1.eventUp && !this.hidden && Game1.activeClickableMenu is GameMenu == false && Game1.activeClickableMenu is ShopMenu == false && Game1.activeClickableMenu is IClickableMenu == false)
            {
                IClickableMenu.drawButtonWithText(Game1.spriteBatch, Game1.smallFont, this.alias, this.buttonRectangle.X, this.buttonRectangle.Y, this.buttonRectangle.Width, this.buttonRectangle.Height, Color.BurlyWood * this.transparency);
            }
        }
    }
}
