using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.LookupAnything.Framework.Targets
{
    /// <summary>Positional metadata about a fruit tree.</summary>
    internal class FruitTreeTarget : GenericTarget<FruitTree>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="value">The underlying in-game entity.</param>
        /// <param name="tilePosition">The object's tile position in the current location (if applicable).</param>
        public FruitTreeTarget(GameHelper gameHelper, FruitTree value, Vector2? tilePosition = null)
            : base(gameHelper, TargetType.FruitTree, value, tilePosition) { }

        /// <summary>Get the sprite's source rectangle within its texture.</summary>
        public override Rectangle GetSpritesheetArea()
        {
            FruitTree tree = this.Value;

            // stump
            if (tree.stump.Value)
                return new Rectangle(384, tree.treeType.Value * 5 * 16 + 48, 48, 32);

            // growing tree
            if (tree.growthStage.Value < 4)
            {
                switch (tree.growthStage.Value)
                {
                    case 0:
                    case 1:
                    case 2:
                        return new Rectangle(tree.growthStage.Value * 48, tree.treeType.Value * 5 * 16, 48, 80);

                    default:
                        return new Rectangle(144, tree.treeType.Value * 5 * 16, 48, 80);
                }
            }

            // grown tree
            return new Rectangle((12 + (tree.GreenHouseTree ? 1 : Utility.getSeasonNumber(Game1.currentSeason)) * 3) * 16, tree.treeType.Value * 5 * 16, 48, 16 + 64);
        }

        /// <summary>Get a rectangle which roughly bounds the visible sprite relative the viewport.</summary>
        /// <remarks>Reverse-engineered from <see cref="FruitTree.draw"/>.</remarks>
        public override Rectangle GetWorldArea()
        {
            FruitTree tree = this.Value;
            Rectangle sprite = this.GetSpritesheetArea();

            int width = sprite.Width * Game1.pixelZoom;
            int height = sprite.Height * Game1.pixelZoom;
            int x, y;
            if (tree.growthStage.Value < 4)
            {
                // apply crazy offset logic for growing fruit trees
                Vector2 tile = this.GetTile();
                Vector2 offset = new Vector2((float)Math.Max(-8.0, Math.Min(Game1.tileSize, Math.Sin(tile.X * 200.0 / (2.0 * Math.PI)) * -16.0)), (float)Math.Max(-8.0, Math.Min(Game1.tileSize, Math.Sin(tile.X * 200.0 / (2.0 * Math.PI)) * -16.0)));
                Vector2 centerBottom = new Vector2(tile.X * Game1.tileSize + Game1.tileSize / 2 + offset.X, tile.Y * Game1.tileSize - sprite.Height + Game1.tileSize * 2 + offset.Y) - new Vector2(Game1.viewport.X, Game1.viewport.Y);
                x = (int)centerBottom.X - width / 2;
                y = (int)centerBottom.Y - height;
            }
            else
            {
                // grown trees are centered on tile
                Rectangle tileArea = base.GetWorldArea();
                x = tileArea.Center.X - width / 2;
                y = tileArea.Bottom - height;
            }

            return new Rectangle(x, y, width, height);
        }

        /// <summary>Get whether the visible sprite intersects the specified coordinate. This can be an expensive test.</summary>
        /// <param name="tile">The tile to search.</param>
        /// <param name="position">The viewport-relative coordinates to search.</param>
        /// <param name="spriteArea">The approximate sprite area calculated by <see cref="GetWorldArea"/>.</param>
        /// <remarks>Reverse engineered from <see cref="FruitTree.draw"/>.</remarks>
        public override bool SpriteIntersectsPixel(Vector2 tile, Vector2 position, Rectangle spriteArea)
        {
            Texture2D spriteSheet = FruitTree.texture;
            Rectangle sourceRectangle = this.GetSpritesheetArea();
            SpriteEffects spriteEffects = this.Value.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            return this.SpriteIntersectsPixel(tile, position, spriteArea, spriteSheet, sourceRectangle, spriteEffects);
        }
    }
}
