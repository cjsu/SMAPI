using Microsoft.Xna.Framework;

namespace Pathoschild.Stardew.LookupAnything.Framework.Targets
{
    /// <summary>Positional metadata about an object in the world.</summary>
    internal interface ITarget
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The target type.</summary>
        TargetType Type { get; set; }

        /// <summary>The object's tile position in the current location (if applicable).</summary>
        Vector2? Tile { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Get the target's tile position, or throw an exception if it doesn't have one.</summary>
        /// <exception cref="System.InvalidOperationException">The target doesn't have a tile position.</exception>
        Vector2 GetTile();

        /// <summary>Get whether the object is at the specified map tile position.</summary>
        /// <param name="position">The map tile position.</param>
        bool IsAtTile(Vector2 position);

        /// <summary>Get a strongly-typed value.</summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        T GetValue<T>();

        /// <summary>Get the sprite's source rectangle within its texture.</summary>
        Rectangle GetSpritesheetArea();

        /// <summary>Get a rectangle which roughly bounds the visible sprite relative the viewport.</summary>
        Rectangle GetWorldArea();

        /// <summary>Get whether the visible sprite intersects the specified coordinate. This can be an expensive test.</summary>
        /// <param name="tile">The tile to search.</param>
        /// <param name="position">The viewport-relative coordinates to search.</param>
        /// <param name="spriteArea">The approximate sprite area calculated by <see cref="GetWorldArea"/>.</param>
        bool SpriteIntersectsPixel(Vector2 tile, Vector2 position, Rectangle spriteArea);
    }
}
