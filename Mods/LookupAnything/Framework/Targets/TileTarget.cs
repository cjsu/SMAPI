using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.LookupAnything.Framework.Targets
{
    /// <summary>Positional metadata about a map tile.</summary>
    internal class TileTarget : GenericTarget<Vector2>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="position">The tile position.</param>
        public TileTarget(GameHelper gameHelper, Vector2 position)
            : base(gameHelper, TargetType.Tile, position, position) { }

        /// <summary>Get the sprite's source rectangle within its texture.</summary>
        public override Rectangle GetSpritesheetArea()
        {
            return Rectangle.Empty;
        }
    }
}
