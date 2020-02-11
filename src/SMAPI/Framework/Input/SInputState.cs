using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using StardewValley;

#pragma warning disable 809 // obsolete override of non-obsolete method (this is deliberate)
namespace StardewModdingAPI.Framework.Input
{
    /// <summary>Manages the game's input state.</summary>
    internal sealed class SInputState : InputState
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The maximum amount of direction to ignore for the left thumbstick.</summary>
        private const float LeftThumbstickDeadZone = 0.2f;

        /// <summary>The cursor position on the screen adjusted for the zoom level.</summary>
        private CursorPosition CursorPositionImpl;

        /// <summary>The player's last known tile position.</summary>
        private Vector2? LastPlayerTile;


        /*********
        ** Accessors
        *********/
        /// <summary>The controller state as of the last update.</summary>
        public GamePadState RealController { get; private set; }

        /// <summary>The keyboard state as of the last update.</summary>
        public KeyboardState RealKeyboard { get; private set; }

        /// <summary>The mouse state as of the last update.</summary>
        public MouseState RealMouse { get; private set; }

        /// <summary>A derivative of <see cref="RealController"/> which suppresses the buttons in <see cref="SuppressButtons"/>.</summary>
        public GamePadState SuppressedController { get; private set; }

        /// <summary>A derivative of <see cref="RealKeyboard"/> which suppresses the buttons in <see cref="SuppressButtons"/>.</summary>
        public KeyboardState SuppressedKeyboard { get; private set; }

        /// <summary>A derivative of <see cref="RealMouse"/> which suppresses the buttons in <see cref="SuppressButtons"/>.</summary>
        public MouseState SuppressedMouse { get; private set; }

        /// <summary>The cursor position on the screen adjusted for the zoom level.</summary>
        public ICursorPosition CursorPosition => this.CursorPositionImpl;

        /// <summary>The buttons which were pressed, held, or released.</summary>
        public IDictionary<SButton, InputStatus> ActiveButtons { get; private set; } = new Dictionary<SButton, InputStatus>();

        /// <summary>The buttons to suppress when the game next handles input. Each button is suppressed until it's released.</summary>
        public HashSet<SButton> SuppressButtons { get; } = new HashSet<SButton>();


        /*********
        ** Public methods
        *********/
        /// <summary>Get a copy of the current state.</summary>
        public SInputState Clone()
        {
            return new SInputState
            {
                ActiveButtons = this.ActiveButtons,
                RealController = this.RealController,
                RealKeyboard = this.RealKeyboard,
                RealMouse = this.RealMouse,
                CursorPositionImpl = this.CursorPositionImpl
            };
        }

        /// <summary>This method is called by the game, and does nothing since SMAPI will already have updated by that point.</summary>
        [Obsolete("This method should only be called by the game itself.")]
        public override void Update() { }

        /// <summary>Update the current button statuses for the given tick.</summary>
        public void TrueUpdate()
        {
            try
            {
                float zoomMultiplier = (1f / Game1.options.zoomLevel);

                // get new states
                GamePadState realController = GamePad.GetState(PlayerIndex.One);
                KeyboardState realKeyboard = Keyboard.GetState();
                MouseState realMouse = Mouse.GetState();
                var activeButtons = this.DeriveStatuses(this.ActiveButtons, realKeyboard, realMouse, realController);
                Vector2 cursorAbsolutePos = new Vector2((realMouse.X * zoomMultiplier) + Game1.viewport.X, (realMouse.Y * zoomMultiplier) + Game1.viewport.Y);
                Vector2? playerTilePos = Context.IsPlayerFree ? Game1.player.getTileLocation() : (Vector2?)null;

                // update real states
                this.ActiveButtons = activeButtons;
                this.RealController = realController;
                this.RealKeyboard = realKeyboard;
                this.RealMouse = realMouse;
                if (cursorAbsolutePos != this.CursorPositionImpl?.AbsolutePixels || playerTilePos != this.LastPlayerTile)
                {
                    this.LastPlayerTile = playerTilePos;
                    this.CursorPositionImpl = this.GetCursorPosition(realMouse, cursorAbsolutePos, zoomMultiplier);
                }

                // update suppressed states
                this.SuppressButtons.RemoveWhere(p => !this.GetStatus(activeButtons, p).IsDown());
                this.UpdateSuppression();
            }
            catch (InvalidOperationException)
            {
                // GetState() may crash for some players if window doesn't have focus but game1.IsActive == true
            }
        }

        /// <summary>Apply input suppression to current input.</summary>
        public void UpdateSuppression()
        {
            GamePadState suppressedController = this.RealController;
            KeyboardState suppressedKeyboard = this.RealKeyboard;
            MouseState suppressedMouse = this.RealMouse;

            this.SuppressGivenStates(this.ActiveButtons, ref suppressedKeyboard, ref suppressedMouse, ref suppressedController);

            this.SuppressedController = suppressedController;
            this.SuppressedKeyboard = suppressedKeyboard;
            this.SuppressedMouse = suppressedMouse;
        }

        /// <summary>Get the gamepad state visible to the game.</summary>
        [Obsolete("This method should only be called by the game itself.")]
        public override GamePadState GetGamePadState()
        {
            if (Game1.options.gamepadMode == Options.GamepadModes.ForceOff)
                return base.GetGamePadState();

            return this.ShouldSuppressNow()
                ? this.SuppressedController
                : this.RealController;
        }

        /// <summary>Get the keyboard state visible to the game.</summary>
        [Obsolete("This method should only be called by the game itself.")]
        public override KeyboardState GetKeyboardState()
        {
            return this.ShouldSuppressNow()
                ? this.SuppressedKeyboard
                : this.RealKeyboard;
        }

        /// <summary>Get the keyboard state visible to the game.</summary>
        [Obsolete("This method should only be called by the game itself.")]
        public override MouseState GetMouseState()
        {
            return this.ShouldSuppressNow()
                ? this.SuppressedMouse
                : this.RealMouse;
        }

        /// <summary>Get whether a given button was pressed or held.</summary>
        /// <param name="button">The button to check.</param>
        public bool IsDown(SButton button)
        {
            return this.GetStatus(this.ActiveButtons, button).IsDown();
        }

        /// <summary>Get whether any of the given buttons were pressed or held.</summary>
        /// <param name="buttons">The buttons to check.</param>
        public bool IsAnyDown(InputButton[] buttons)
        {
            return buttons.Any(button => this.IsDown(button.ToSButton()));
        }

        /// <summary>Get the status of a button.</summary>
        /// <param name="button">The button to check.</param>
        public InputStatus GetStatus(SButton button)
        {
            return this.GetStatus(this.ActiveButtons, button);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the current cursor position.</summary>
        /// <param name="mouseState">The current mouse state.</param>
        /// <param name="absolutePixels">The absolute pixel position relative to the map, adjusted for pixel zoom.</param>
        /// <param name="zoomMultiplier">The multiplier applied to pixel coordinates to adjust them for pixel zoom.</param>
        private CursorPosition GetCursorPosition(MouseState mouseState, Vector2 absolutePixels, float zoomMultiplier)
        {
            Vector2 screenPixels = new Vector2(mouseState.X * zoomMultiplier, mouseState.Y * zoomMultiplier);
            Vector2 tile = new Vector2((int)((Game1.viewport.X + screenPixels.X) / Game1.tileSize), (int)((Game1.viewport.Y + screenPixels.Y) / Game1.tileSize));
            Vector2 grabTile = (Game1.mouseCursorTransparency > 0 && Utility.tileWithinRadiusOfPlayer((int)tile.X, (int)tile.Y, 1, Game1.player)) // derived from Game1.pressActionButton
                ? tile
                : Game1.player.GetGrabTile();
            return new CursorPosition(absolutePixels, screenPixels, tile, grabTile);
        }

        /// <summary>Whether input should be suppressed in the current context.</summary>
        private bool ShouldSuppressNow()
        {
            return Game1.chatBox == null || !Game1.chatBox.isActive();
        }

        /// <summary>Apply input suppression to the given input states.</summary>
        /// <param name="activeButtons">The current button states to check.</param>
        /// <param name="keyboardState">The game's keyboard state for the current tick.</param>
        /// <param name="mouseState">The game's mouse state for the current tick.</param>
        /// <param name="gamePadState">The game's controller state for the current tick.</param>
        private void SuppressGivenStates(IDictionary<SButton, InputStatus> activeButtons, ref KeyboardState keyboardState, ref MouseState mouseState, ref GamePadState gamePadState)
        {
            if (this.SuppressButtons.Count == 0)
                return;

            // gather info
            HashSet<Keys> suppressKeys = new HashSet<Keys>();
            HashSet<SButton> suppressButtons = new HashSet<SButton>();
            HashSet<SButton> suppressMouse = new HashSet<SButton>();
            foreach (SButton button in this.SuppressButtons)
            {
                if (button == SButton.MouseLeft || button == SButton.MouseMiddle || button == SButton.MouseRight || button == SButton.MouseX1 || button == SButton.MouseX2)
                    suppressMouse.Add(button);
                else if (button.TryGetKeyboard(out Keys key))
                    suppressKeys.Add(key);
                else if (gamePadState.IsConnected && button.TryGetController(out Buttons _))
                    suppressButtons.Add(button);
            }

            // suppress keyboard keys
            if (keyboardState.GetPressedKeys().Any() && suppressKeys.Any())
                keyboardState = new KeyboardState(keyboardState.GetPressedKeys().Except(suppressKeys).ToArray());

            // suppress controller keys
            if (gamePadState.IsConnected && suppressButtons.Any())
            {
                GamePadStateBuilder builder = new GamePadStateBuilder(gamePadState);
                builder.SuppressButtons(suppressButtons);
                gamePadState = builder.ToGamePadState();
            }

            // suppress mouse buttons
            if (suppressMouse.Any())
            {
                mouseState = new MouseState(
                    x: mouseState.X,
                    y: mouseState.Y,
                    scrollWheel: mouseState.ScrollWheelValue,
                    leftButton: suppressMouse.Contains(SButton.MouseLeft) ? ButtonState.Released : mouseState.LeftButton,
                    middleButton: suppressMouse.Contains(SButton.MouseMiddle) ? ButtonState.Released : mouseState.MiddleButton,
                    rightButton: suppressMouse.Contains(SButton.MouseRight) ? ButtonState.Released : mouseState.RightButton,
                    xButton1: suppressMouse.Contains(SButton.MouseX1) ? ButtonState.Released : mouseState.XButton1,
                    xButton2: suppressMouse.Contains(SButton.MouseX2) ? ButtonState.Released : mouseState.XButton2
                );
            }
        }

        /// <summary>Get the status of all pressed or released buttons relative to their previous status.</summary>
        /// <param name="previousStatuses">The previous button statuses.</param>
        /// <param name="keyboard">The keyboard state.</param>
        /// <param name="mouse">The mouse state.</param>
        /// <param name="controller">The controller state.</param>
        private IDictionary<SButton, InputStatus> DeriveStatuses(IDictionary<SButton, InputStatus> previousStatuses, KeyboardState keyboard, MouseState mouse, GamePadState controller)
        {
            IDictionary<SButton, InputStatus> activeButtons = new Dictionary<SButton, InputStatus>();

            // handle pressed keys
            SButton[] down = this.GetPressedButtons(keyboard, mouse, controller).ToArray();
            foreach (SButton button in down)
                activeButtons[button] = this.DeriveStatus(this.GetStatus(previousStatuses, button), isDown: true);

            // handle released keys
            foreach (KeyValuePair<SButton, InputStatus> prev in previousStatuses)
            {
                if (prev.Value.IsDown() && !activeButtons.ContainsKey(prev.Key))
                    activeButtons[prev.Key] = InputStatus.Released;
            }

            return activeButtons;
        }

        /// <summary>Get the status of a button relative to its previous status.</summary>
        /// <param name="oldStatus">The previous button status.</param>
        /// <param name="isDown">Whether the button is currently down.</param>
        private InputStatus DeriveStatus(InputStatus oldStatus, bool isDown)
        {
            if (isDown && oldStatus.IsDown())
                return InputStatus.Held;
            if (isDown)
                return InputStatus.Pressed;
            return InputStatus.Released;
        }

        /// <summary>Get the status of a button.</summary>
        /// <param name="activeButtons">The current button states to check.</param>
        /// <param name="button">The button to check.</param>
        private InputStatus GetStatus(IDictionary<SButton, InputStatus> activeButtons, SButton button)
        {
            return activeButtons.TryGetValue(button, out InputStatus status) ? status : InputStatus.None;
        }

        /// <summary>Get the buttons pressed in the given stats.</summary>
        /// <param name="keyboard">The keyboard state.</param>
        /// <param name="mouse">The mouse state.</param>
        /// <param name="controller">The controller state.</param>
        /// <remarks>Thumbstick direction logic derived from <see cref="ButtonCollection"/>.</remarks>
        private IEnumerable<SButton> GetPressedButtons(KeyboardState keyboard, MouseState mouse, GamePadState controller)
        {
            // keyboard
            foreach (Keys key in keyboard.GetPressedKeys())
                yield return key.ToSButton();

            // mouse
            if (mouse.LeftButton == ButtonState.Pressed)
                yield return SButton.MouseLeft;
            if (mouse.RightButton == ButtonState.Pressed)
                yield return SButton.MouseRight;
            if (mouse.MiddleButton == ButtonState.Pressed)
                yield return SButton.MouseMiddle;
            if (mouse.XButton1 == ButtonState.Pressed)
                yield return SButton.MouseX1;
            if (mouse.XButton2 == ButtonState.Pressed)
                yield return SButton.MouseX2;

            // controller
            if (controller.IsConnected)
            {
                // main buttons
                if (controller.Buttons.A == ButtonState.Pressed)
                    yield return SButton.ControllerA;
                if (controller.Buttons.B == ButtonState.Pressed)
                    yield return SButton.ControllerB;
                if (controller.Buttons.X == ButtonState.Pressed)
                    yield return SButton.ControllerX;
                if (controller.Buttons.Y == ButtonState.Pressed)
                    yield return SButton.ControllerY;
                if (controller.Buttons.LeftStick == ButtonState.Pressed)
                    yield return SButton.LeftStick;
                if (controller.Buttons.RightStick == ButtonState.Pressed)
                    yield return SButton.RightStick;
                if (controller.Buttons.Start == ButtonState.Pressed)
                    yield return SButton.ControllerStart;

                // directional pad
                if (controller.DPad.Up == ButtonState.Pressed)
                    yield return SButton.DPadUp;
                if (controller.DPad.Down == ButtonState.Pressed)
                    yield return SButton.DPadDown;
                if (controller.DPad.Left == ButtonState.Pressed)
                    yield return SButton.DPadLeft;
                if (controller.DPad.Right == ButtonState.Pressed)
                    yield return SButton.DPadRight;

                // secondary buttons
                if (controller.Buttons.Back == ButtonState.Pressed)
                    yield return SButton.ControllerBack;
                if (controller.Buttons.BigButton == ButtonState.Pressed)
                    yield return SButton.BigButton;

                // shoulders
                if (controller.Buttons.LeftShoulder == ButtonState.Pressed)
                    yield return SButton.LeftShoulder;
                if (controller.Buttons.RightShoulder == ButtonState.Pressed)
                    yield return SButton.RightShoulder;

                // triggers
                if (controller.Triggers.Left > 0.2f)
                    yield return SButton.LeftTrigger;
                if (controller.Triggers.Right > 0.2f)
                    yield return SButton.RightTrigger;

                // left thumbstick direction
                if (controller.ThumbSticks.Left.Y > SInputState.LeftThumbstickDeadZone)
                    yield return SButton.LeftThumbstickUp;
                if (controller.ThumbSticks.Left.Y < -SInputState.LeftThumbstickDeadZone)
                    yield return SButton.LeftThumbstickDown;
                if (controller.ThumbSticks.Left.X > SInputState.LeftThumbstickDeadZone)
                    yield return SButton.LeftThumbstickRight;
                if (controller.ThumbSticks.Left.X < -SInputState.LeftThumbstickDeadZone)
                    yield return SButton.LeftThumbstickLeft;

                // right thumbstick direction
                if (this.IsRightThumbstickOutsideDeadZone(controller.ThumbSticks.Right))
                {
                    if (controller.ThumbSticks.Right.Y > 0)
                        yield return SButton.RightThumbstickUp;
                    if (controller.ThumbSticks.Right.Y < 0)
                        yield return SButton.RightThumbstickDown;
                    if (controller.ThumbSticks.Right.X > 0)
                        yield return SButton.RightThumbstickRight;
                    if (controller.ThumbSticks.Right.X < 0)
                        yield return SButton.RightThumbstickLeft;
                }
            }
        }

        /// <summary>Get whether the right thumbstick should be considered outside the dead zone.</summary>
        /// <param name="direction">The right thumbstick value.</param>
        private bool IsRightThumbstickOutsideDeadZone(Vector2 direction)
        {
            return direction.Length() > 0.9f;
        }
    }
}
