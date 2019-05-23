using System;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI
{
    /// <summary>Represents a cursor position in the different coordinate systems.</summary>
    public interface ICursorPosition : IEquatable<ICursorPosition>
    {
        /// <summary>The pixel position relative to the top-left corner of the in-game map.</summary>
        Vector2 AbsolutePixels { get; }

        /// <summary>The pixel position relative to the top-left corner of the visible screen.</summary>
        Vector2 ScreenPixels { get; }

        /// <summary>The tile position under the cursor relative to the top-left corner of the map.</summary>
        Vector2 Tile { get; }

        /// <summary>The tile position that the game considers under the cursor for purposes of clicking actions. This may be different than <see cref="Tile"/> if that's too far from the player.</summary>
        Vector2 GrabTile { get; }
    }
}
