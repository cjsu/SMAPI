using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.LookupAnything.Framework.Targets
{
    /// <summary>Positional metadata about an unknown object.</summary>
    internal class UnknownTarget : GenericTarget<object>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="value">The underlying in-game entity.</param>
        /// <param name="tilePosition">The object's tile position in the current location (if applicable).</param>
        public UnknownTarget(GameHelper gameHelper, object value, Vector2? tilePosition = null)
            : base(gameHelper, TargetType.Unknown, value, tilePosition) { }

        /// <summary>Get the sprite's source rectangle within its texture.</summary>
        public override Rectangle GetSpritesheetArea()
        {
            return Rectangle.Empty;
        }
    }
}
