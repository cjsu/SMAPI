#if !SMAPI_1_x
using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace StardewModdingAPI.Events
{
    /// <summary>Event arguments when a button is pressed or released.</summary>
    public class EventArgsInput : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The button on the controller, keyboard, or mouse.</summary>
        public SButton Button { get; }

        /// <summary>The current cursor position.</summary>
        public ICursorPosition Cursor { get; set; }

        /// <summary>Whether the input is considered a 'click' by the game for enabling action.</summary>
        public bool IsClick { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="button">The button on the controller, keyboard, or mouse.</param>
        /// <param name="cursor">The cursor position.</param>
        /// <param name="isClick">Whether the input is considered a 'click' by the game for enabling action.</param>
        public EventArgsInput(SButton button, ICursorPosition cursor, bool isClick)
        {
            this.Button = button;
            this.Cursor = cursor;
            this.IsClick = isClick;
        }

        /// <summary>Prevent the game from handling the vurrent button press. This doesn't prevent other mods from receiving the event.</summary>
        public void SuppressButton()
        {
            this.SuppressButton(this.Button);
        }

        /// <summary>Prevent the game from handling a button press. This doesn't prevent other mods from receiving the event.</summary>
        /// <param name="button">The button to suppress.</param>
        public void SuppressButton(SButton button)
        {
            // keyboard
            if (this.Button.TryGetKeyboard(out Keys key))
                Game1.oldKBState = new KeyboardState(Game1.oldKBState.GetPressedKeys().Except(new[] { key }).ToArray());

            // controller
            else if (this.Button.TryGetController(out Buttons controllerButton))
            {
                var newState = GamePad.GetState(PlayerIndex.One);
                var thumbsticks = Game1.oldPadState.ThumbSticks;
                var triggers = Game1.oldPadState.Triggers;
                var buttons = Game1.oldPadState.Buttons;
                var dpad = Game1.oldPadState.DPad;

                switch (controllerButton)
                {
                    // d-pad
                    case Buttons.DPadDown:
                        dpad = new GamePadDPad(dpad.Up, newState.DPad.Down, dpad.Left, dpad.Right);
                        break;
                    case Buttons.DPadLeft:
                        dpad = new GamePadDPad(dpad.Up, dpad.Down, newState.DPad.Left, dpad.Right);
                        break;
                    case Buttons.DPadRight:
                        dpad = new GamePadDPad(dpad.Up, dpad.Down, dpad.Left, newState.DPad.Right);
                        break;
                    case Buttons.DPadUp:
                        dpad = new GamePadDPad(newState.DPad.Up, dpad.Down, dpad.Left, dpad.Right);
                        break;

                    // trigger
                    case Buttons.LeftTrigger:
                        triggers = new GamePadTriggers(newState.Triggers.Left, triggers.Right);
                        break;
                    case Buttons.RightTrigger:
                        triggers = new GamePadTriggers(triggers.Left, newState.Triggers.Right);
                        break;

                    // thumbstick
                    case Buttons.LeftThumbstickDown:
                    case Buttons.LeftThumbstickLeft:
                    case Buttons.LeftThumbstickRight:
                    case Buttons.LeftThumbstickUp:
                        thumbsticks = new GamePadThumbSticks(newState.ThumbSticks.Left, thumbsticks.Right);
                        break;
                    case Buttons.RightThumbstickDown:
                    case Buttons.RightThumbstickLeft:
                    case Buttons.RightThumbstickRight:
                    case Buttons.RightThumbstickUp:
                        thumbsticks = new GamePadThumbSticks(newState.ThumbSticks.Right, thumbsticks.Left);
                        break;

                    // buttons
                    default:
                        var mask =
                            (buttons.A == ButtonState.Pressed ? Buttons.A : 0)
                            | (buttons.B == ButtonState.Pressed ? Buttons.B : 0)
                            | (buttons.Back == ButtonState.Pressed ? Buttons.Back : 0)
                            | (buttons.BigButton == ButtonState.Pressed ? Buttons.BigButton : 0)
                            | (buttons.LeftShoulder == ButtonState.Pressed ? Buttons.LeftShoulder : 0)
                            | (buttons.LeftStick == ButtonState.Pressed ? Buttons.LeftStick : 0)
                            | (buttons.RightShoulder == ButtonState.Pressed ? Buttons.RightShoulder : 0)
                            | (buttons.RightStick == ButtonState.Pressed ? Buttons.RightStick : 0)
                            | (buttons.Start == ButtonState.Pressed ? Buttons.Start : 0)
                            | (buttons.X == ButtonState.Pressed ? Buttons.X : 0)
                            | (buttons.Y == ButtonState.Pressed ? Buttons.Y : 0);
                        mask = mask ^ controllerButton;
                        buttons = new GamePadButtons(mask);
                        break;
                }

                Game1.oldPadState = new GamePadState(thumbsticks, triggers, buttons, dpad);
            }
        }
    }
}
#endif
