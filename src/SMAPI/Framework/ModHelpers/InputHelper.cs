using StardewModdingAPI.Framework.Input;

namespace StardewModdingAPI.Framework.ModHelpers
{
    /// <summary>Provides an API for checking and changing input state.</summary>
    internal class InputHelper : BaseHelper, IInputHelper
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Manages the game's input state.</summary>
        private readonly SInputState InputState;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="modID">The unique ID of the relevant mod.</param>
        /// <param name="inputState">Manages the game's input state.</param>
        public InputHelper(string modID, SInputState inputState)
            : base(modID)
        {
            this.InputState = inputState;
        }

        /// <summary>Get the current cursor position.</summary>
        public ICursorPosition GetCursorPosition()
        {
            return this.InputState.CursorPosition;
        }

        /// <summary>Get whether a button is currently pressed.</summary>
        /// <param name="button">The button.</param>
        public bool IsDown(SButton button)
        {
            return this.InputState.IsDown(button);
        }

        /// <summary>Get whether a button is currently suppressed, so the game won't see it.</summary>
        /// <param name="button">The button.</param>
        public bool IsSuppressed(SButton button)
        {
            return this.InputState.SuppressButtons.Contains(button);
        }

        /// <summary>Prevent the game from handling a button press. This doesn't prevent other mods from receiving the event.</summary>
        /// <param name="button">The button to suppress.</param>
        public void Suppress(SButton button)
        {
            this.InputState.SuppressButtons.Add(button);
        }

        /// <summary>Get the status of a button.</summary>
        /// <param name="button">The button to check.</param>
        public InputStatus GetStatus(SButton button)
        {
            return this.InputState.GetStatus(button);
        }
    }
}
