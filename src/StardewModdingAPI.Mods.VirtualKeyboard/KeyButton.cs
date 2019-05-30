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
        private readonly int padding;

        private readonly IReflectedMethod RaiseButtonPressed;
        private readonly IReflectedMethod RaiseButtonReleased;
        private readonly IReflectedMethod Legacy_KeyPressed;
        private readonly IReflectedMethod Legacy_KeyReleased;

        private readonly SButton button;
        private readonly float transparency;
        public bool hidden;
        private bool raisingPressed = false;
        private bool raisingReleased = false;

        public KeyButton(IModHelper helper, VirtualButton buttonDefine, IMonitor monitor)
        {
            this.Monitor = monitor;
            this.helper = helper;
            this.hidden = true;
            this.buttonRectangle = new Rectangle(buttonDefine.rectangle.X, buttonDefine.rectangle.Y, buttonDefine.rectangle.Width, buttonDefine.rectangle.Height);
            this.padding = buttonDefine.rectangle.Padding;
            this.button = buttonDefine.key;
            if (buttonDefine.transparency <= 0.01f || buttonDefine.transparency > 1f)
            {
                buttonDefine.transparency = 0.5f;
            }
            this.transparency = buttonDefine.transparency;

            helper.Events.Display.RenderingHud += this.OnRenderingHud;
            helper.Events.Input.ButtonReleased += this.EventInputButtonReleased;
            helper.Events.Input.ButtonPressed += this.EventInputButtonPressed;

            //TODO
            //Use C# Reflection and re-enable SMAPI IReflected checks

            MainActivity activity = this.helper.Reflection.GetField<MainActivity>(typeof(MainActivity), "instance").GetValue();
            object score = activity.GetType().GetField("core", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(activity);
            object eventManager = score.GetType().GetField("EventManager", BindingFlags.Public | BindingFlags.Instance).GetValue(score);

            object buttonPressed = eventManager.GetType().GetField("ButtonPressed", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);
            object buttonReleased = eventManager.GetType().GetField("ButtonReleased", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);

            object legacyButtonPressed = eventManager.GetType().GetField("Legacy_KeyPressed", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);
            object legacyButtonReleased = eventManager.GetType().GetField("Legacy_KeyReleased", BindingFlags.Public | BindingFlags.Instance).GetValue(eventManager);

            this.RaiseButtonPressed = this.helper.Reflection.GetMethod(buttonPressed, "Raise");
            this.RaiseButtonReleased = this.helper.Reflection.GetMethod(buttonReleased, "Raise");

            this.Legacy_KeyPressed = this.helper.Reflection.GetMethod(legacyButtonPressed, "Raise");
            this.Legacy_KeyReleased = this.helper.Reflection.GetMethod(legacyButtonReleased, "Raise");
        }

        private bool shouldTrigger(Vector2 point)
        {
            int x1 = Mouse.GetState().X / (int)Game1.NativeZoomLevel;
            int y1 = Mouse.GetState().Y / (int)Game1.NativeZoomLevel;
            if (this.buttonRectangle.Contains(x1, y1))
            {
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
            Vector2 point = e.Cursor.ScreenPixels;
            if (this.shouldTrigger(point))
            {
                object inputState = e.GetType().GetField("InputState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(e);
                 
                object buttonPressedEventArgs = Activator.CreateInstance(typeof(ButtonPressedEventArgs), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { this.button, e.Cursor, inputState }, null);
                EventArgsKeyPressed eventArgsKey = new EventArgsKeyPressed((Keys)this.button);
                try
                {
                    this.raisingPressed = true;

                    //METHODBASE.INVOKE Method
                    //this.RaiseButtonPressed.Invoke("What goes here???", new object[] { buttonPressedEventArgs });

                    this.RaiseButtonPressed.Invoke(new object[] { buttonPressedEventArgs });
                    this.Legacy_KeyPressed.Invoke(new object[] { eventArgsKey });
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
            Vector2 point = e.Cursor.ScreenPixels;
            if (this.shouldTrigger(point))
            {
                object inputState = this.helper.Reflection.GetField<object>(e, "InputState").GetValue();
                object buttonReleasedEventArgs = Activator.CreateInstance(typeof(ButtonReleasedEventArgs), BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { this.button, e.Cursor, inputState }, null);
                EventArgsKeyPressed eventArgsKeyReleased = new EventArgsKeyPressed((Keys)this.button);
                try
                {
                    this.raisingReleased = true;
                    this.RaiseButtonReleased.Invoke(new object[] { buttonReleasedEventArgs });
                    this.Legacy_KeyReleased.Invoke(new object[] { eventArgsKeyReleased });
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
            if (!Game1.eventUp && !this.hidden)
            {
                IClickableMenu.drawButtonWithText(Game1.spriteBatch, Game1.smallFont, this.button.ToString(), this.buttonRectangle.X, this.buttonRectangle.Y, this.buttonRectangle.Width, this.buttonRectangle.Height, Color.BurlyWood * this.transparency);
            }
        }
    }
}
