namespace StardewModdingAPI
{
    /// <summary>The input status for a button during an update frame.</summary>
    public enum InputStatus
    {
        /// <summary>The button was neither pressed, held, nor released.</summary>
        None,

        /// <summary>The button was pressed in this frame.</summary>
        Pressed,

        /// <summary>The button has been held since the last frame.</summary>
        Held,

        /// <summary>The button was released in this frame.</summary>
        Released
    }

    /// <summary>Extension methods for <see cref="InputStatus"/>.</summary>
    internal static class InputStatusExtensions
    {
        /// <summary>Whether the button was pressed or held.</summary>
        /// <param name="status">The button status.</param>
        public static bool IsDown(this InputStatus status)
        {
            return status == InputStatus.Held || status == InputStatus.Pressed;
        }
    }
}
