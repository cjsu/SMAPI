using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace Pathoschild.Stardew.LookupAnything.Framework.Targets
{
    /// <summary>Positional metadata about a constructed building.</summary>
    internal class BuildingTarget : GenericTarget<Building>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The building's tile area.</summary>
        private readonly Rectangle TileArea;

        /// <summary>Spritesheet areas to treat as part of the sprite even if they're transparent, indexed by <see cref="Building.buildingType"/> value.</summary>
        private static readonly IDictionary<string, Rectangle[]> SpriteCollisionOverrides = new Dictionary<string, Rectangle[]>
        {
            ["Barn"] = new[] { new Rectangle(48, 90, 32, 22) }, // animal door
            ["Big Barn"] = new[] { new Rectangle(64, 90, 32, 22) }, // animal door
            ["Deluxe Barn"] = new[] { new Rectangle(64, 90, 32, 22) }, // animal door

            ["Coop"] = new[] { new Rectangle(33, 97, 14, 15) },
            ["Big Coop"] = new[] { new Rectangle(33, 97, 14, 15) },
            ["Deluxe Coop"] = new[] { new Rectangle(33, 97, 14, 15) }
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="value">The underlying in-game entity.</param>
        public BuildingTarget(GameHelper gameHelper, Building value)
            : base(gameHelper, TargetType.Building, value, new Vector2(value.tileX.Value, value.tileY.Value))
        {
            this.TileArea = new Rectangle(value.tileX.Value, value.tileY.Value, value.tilesWide.Value, value.tilesHigh.Value);
        }

        /// <summary>Get the sprite's source rectangle within its texture.</summary>
        public override Rectangle GetSpritesheetArea()
        {
            return this.Value.getSourceRectForMenu();
        }

        /// <summary>Get a rectangle which roughly bounds the visible sprite relative the viewport.</summary>
        public override Rectangle GetWorldArea()
        {
            // get source rectangle adjusted for zoom
            Rectangle sourceRect = this.GetSpritesheetArea();
            sourceRect = new Rectangle(sourceRect.X * Game1.pixelZoom, sourceRect.Y * Game1.pixelZoom, sourceRect.Width * Game1.pixelZoom, sourceRect.Height * Game1.pixelZoom);

            // get foundation area adjusted for zoom
            Rectangle bounds = new Rectangle(
                x: this.TileArea.X * Game1.tileSize,
                y: this.TileArea.Y * Game1.tileSize,
                width: this.TileArea.Width * Game1.tileSize,
                height: this.TileArea.Height * Game1.tileSize
            );

            // get combined sprite area adjusted for viewport
            return new Rectangle(
                x: bounds.X - (sourceRect.Width - bounds.Width + 1) - Game1.viewport.X,
                y: bounds.Y - (sourceRect.Height - bounds.Height + 1) - Game1.viewport.Y,
                width: Math.Max(bounds.Width, sourceRect.Width),
                height: Math.Max(bounds.Height, sourceRect.Height)
            );
        }

        /// <summary>Get whether the visible sprite intersects the specified coordinate. This can be an expensive test.</summary>
        /// <param name="tile">The tile to search.</param>
        /// <param name="position">The viewport-relative coordinates to search.</param>
        /// <param name="spriteArea">The approximate sprite area calculated by <see cref="GetWorldArea"/>.</param>
        public override bool SpriteIntersectsPixel(Vector2 tile, Vector2 position, Rectangle spriteArea)
        {
            Rectangle sourceRect = this.GetSpritesheetArea();

            // check sprite
            if (base.SpriteIntersectsPixel(tile, position, spriteArea, this.Value.texture.Value, sourceRect))
                return true;

            // special exceptions
            if (BuildingTarget.SpriteCollisionOverrides.TryGetValue(this.Value.buildingType.Value, out Rectangle[] overrides))
            {
                Vector2 spriteSheetPosition = this.GameHelper.GetSpriteSheetCoordinates(position, spriteArea, sourceRect);
                return overrides.Any(p => p.Contains((int)spriteSheetPosition.X, (int)spriteSheetPosition.Y));
            }

            return false;
        }
    }
}
