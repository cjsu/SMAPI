namespace StardewModdingAPI.Framework
{
    /// <summary>A delegate which requests that SMAPI immediately exit the game. This should only be invoked when an irrecoverable fatal error happens that risks save corruption or game-breaking bugs.</summary>
    /// <param name="module">The module which requested an immediate exit.</param>
    /// <param name="reason">The reason provided for the shutdown.</param>
    internal delegate void RequestExitDelegate(string module, string reason);
}
