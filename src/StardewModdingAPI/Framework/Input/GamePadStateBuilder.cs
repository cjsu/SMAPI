using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace StardewModdingAPI.Framework.Input
{
    /// <summary>An abstraction for manipulating controller state.</summary>
    internal class GamePadStateBuilder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The current button states.</summary>
        private readonly IDictionary<SButton, ButtonState> ButtonStates;

        /// <summary>The left trigger value.</summary>
        private float LeftTrigger;

        /// <summary>The right trigger value.</summary>
        private float RightTrigger;

        /// <summary>The left thumbstick position.</summary>
        private Vector2 LeftStickPos;

        /// <summary>The left thumbstick position.</summary>
        private Vector2 RightStickPos;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="state">The initial controller state.</param>
        public GamePadStateBuilder(GamePadState state)
        {
            this.ButtonStates = new Dictionary<SButton, ButtonState>
            {
                [SButton.DPadUp] = state.DPad.Up,
                [SButton.DPadDown] = state.DPad.Down,
                [SButton.DPadLeft] = state.DPad.Left,
                [SButton.DPadRight] = state.DPad.Right,

                [SButton.ControllerA] = state.Buttons.A,
                [SButton.ControllerB] = state.Buttons.B,
                [SButton.ControllerX] = state.Buttons.X,
                [SButton.ControllerY] = state.Buttons.Y,
                [SButton.LeftStick] = state.Buttons.LeftStick,
                [SButton.RightStick] = state.Buttons.RightStick,
                [SButton.LeftShoulder] = state.Buttons.LeftShoulder,
                [SButton.RightShoulder] = state.Buttons.RightShoulder,
                [SButton.ControllerBack] = state.Buttons.Back,
                [SButton.ControllerStart] = state.Buttons.Start,
                [SButton.BigButton] = state.Buttons.BigButton
            };
            this.LeftTrigger = state.Triggers.Left;
            this.RightTrigger = state.Triggers.Right;
            this.LeftStickPos = state.ThumbSticks.Left;
            this.RightStickPos = state.ThumbSticks.Right;
        }

        /// <summary>Mark all matching buttons unpressed.</summary>
        /// <param name="buttons">The buttons.</param>
        public void SuppressButtons(IEnumerable<SButton> buttons)
        {
            foreach (SButton button in buttons)
                this.SuppressButton(button);
        }

        /// <summary>Mark a button unpressed.</summary>
        /// <param name="button">The button.</param>
        public void SuppressButton(SButton button)
        {
            switch (button)
            {
                // left thumbstick
                case SButton.LeftThumbstickUp:
                    if (this.LeftStickPos.Y > 0)
                        this.LeftStickPos.Y = 0;
                    break;
                case SButton.LeftThumbstickDown:
                    if (this.LeftStickPos.Y < 0)
                        this.LeftStickPos.Y = 0;
                    break;
                case SButton.LeftThumbstickLeft:
                    if (this.LeftStickPos.X < 0)
                        this.LeftStickPos.X = 0;
                    break;
                case SButton.LeftThumbstickRight:
                    if (this.LeftStickPos.X > 0)
                        this.LeftStickPos.X = 0;
                    break;

                // right thumbstick
                case SButton.RightThumbstickUp:
                    if (this.RightStickPos.Y > 0)
                        this.RightStickPos.Y = 0;
                    break;
                case SButton.RightThumbstickDown:
                    if (this.RightStickPos.Y < 0)
                        this.RightStickPos.Y = 0;
                    break;
                case SButton.RightThumbstickLeft:
                    if (this.RightStickPos.X < 0)
                        this.RightStickPos.X = 0;
                    break;
                case SButton.RightThumbstickRight:
                    if (this.RightStickPos.X > 0)
                        this.RightStickPos.X = 0;
                    break;

                // triggers
                case SButton.LeftTrigger:
                    this.LeftTrigger = 0;
                    break;
                case SButton.RightTrigger:
                    this.RightTrigger = 0;
                    break;

                // buttons
                default:
                    if (this.ButtonStates.ContainsKey(button))
                        this.ButtonStates[button] = ButtonState.Released;
                    break;
            }
        }

        /// <summary>Construct an equivalent gamepad state.</summary>
        public GamePadState ToGamePadState()
        {
            return new GamePadState(
                leftThumbStick: this.LeftStickPos,
                rightThumbStick: this.RightStickPos,
                leftTrigger: this.LeftTrigger,
                rightTrigger: this.RightTrigger,
                buttons: this.GetBitmask(this.GetPressedButtons()) // MonoDevelop requires one bitmask here; don't specify multiple values
            );
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Get all pressed buttons.</summary>
        private IEnumerable<Buttons> GetPressedButtons()
        {
            foreach (var pair in this.ButtonStates)
            {
                if (pair.Value == ButtonState.Pressed && pair.Key.TryGetController(out Buttons button))
                    yield return button;
            }
        }

        /// <summary>Get a bitmask representing the given buttons.</summary>
        /// <param name="buttons">The buttons to represent.</param>
        private Buttons GetBitmask(IEnumerable<Buttons> buttons)
        {
            Buttons flag = 0;
            foreach (Buttons button in buttons)
                flag |= button;
            return flag;
        }
    }
}
