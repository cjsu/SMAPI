using System;
using StardewModdingAPI.Framework.Input;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides an API for checking and changing input state.</summary>
    internal class InputHelper : BaseHelper, IInputHelper
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Manages the game's input state for the current player instance. That may not be the main player in split-screen mode.</summary>
        private readonly Func<SInputState> CurrentInputState;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="currentInputState">Manages the game's input state for the current player instance. That may not be the main player in split-screen mode.</param>
        public InputHelper(string modID, Func<SInputState> currentInputState)
            : base(modID)
        {
            this.CurrentInputState = currentInputState;
        }

        /// <inheritdoc />
        public ICursorPosition GetCursorPosition()
        {
            return this.CurrentInputState().CursorPosition;
        }

        /// <inheritdoc />
        public bool IsDown(SButton button)
        {
            return this.CurrentInputState().IsDown(button);
        }

        /// <inheritdoc />
        public bool IsSuppressed(SButton button)
        {
            return this.CurrentInputState().IsSuppressed(button);
        }

        /// <inheritdoc />
        public void Suppress(SButton button)
        {
            this.CurrentInputState().OverrideButton(button, setDown: false);
        }

        /// <inheritdoc />
        public SButtonState GetState(SButton button)
        {
            return this.CurrentInputState().GetState(button);
        }
    }
}
