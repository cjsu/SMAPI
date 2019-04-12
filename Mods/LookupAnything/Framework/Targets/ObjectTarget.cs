using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.LookupAnything.Framework.Targets
{
    /// <summary>Positional metadata about a world object.</summary>
    internal class ObjectTarget : GenericTarget<SObject>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Simplifies access to private game code.</summary>
        private readonly IReflectionHelper Reflection;

        /// <summary>The item sprite.</summary>
        private readonly SpriteInfo CustomSprite;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="value">The underlying in-game entity.</param>
        /// <param name="tilePosition">The object's tile position in the current location (if applicable).</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        public ObjectTarget(GameHelper gameHelper, SObject value, Vector2? tilePosition, IReflectionHelper reflection)
            : base(gameHelper, TargetType.Object, value, tilePosition)
        {
            this.Reflection = reflection;
            this.CustomSprite = gameHelper.GetSprite(value, onlyCustom: true); // only get sprite if it's custom; else we'll use contextual logic (e.g. for fence direction)
        }

        /// <summary>Get the sprite's source rectangle within its texture.</summary>
        public override Rectangle GetSpritesheetArea()
        {
            if (this.CustomSprite != null)
                return this.CustomSprite.SourceRectangle;

            SObject obj = this.Value;
            switch (obj)
            {
                case Fence fence:
                    return this.GetSpritesheetArea(fence, Game1.currentLocation);

                case Furniture furniture:
                    return furniture.sourceRect.Value;

                default:
                    return obj.bigCraftable.Value
                        ? SObject.getSourceRectForBigCraftable(obj.ParentSheetIndex)
                        : Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, SObject.spriteSheetTileSize, SObject.spriteSheetTileSize);
            }

        }

        /// <summary>Get a rectangle which roughly bounds the visible sprite relative the viewport.</summary>
        public override Rectangle GetWorldArea()
        {
            // get object info
            SObject obj = this.Value;
            Rectangle boundingBox = obj.getBoundingBox(this.GetTile());

            // get sprite area
            if (this.CustomSprite != null)
            {
                Rectangle spriteArea = this.GetSpriteArea(boundingBox, this.CustomSprite.SourceRectangle);
                return new Rectangle(
                    x: spriteArea.X,
                    y: spriteArea.Y - (spriteArea.Height / 2), // custom sprite areas are offset from game logic
                    width: spriteArea.Width,
                    height: spriteArea.Height
                );
            }

            return this.GetSpriteArea(boundingBox, this.GetSpritesheetArea());
        }

        /// <summary>Get whether the visible sprite intersects the specified coordinate. This can be an expensive test.</summary>
        /// <param name="tile">The tile to search.</param>
        /// <param name="position">The viewport-relative coordinates to search.</param>
        /// <param name="spriteArea">The approximate sprite area calculated by <see cref="GetWorldArea"/>.</param>
        public override bool SpriteIntersectsPixel(Vector2 tile, Vector2 position, Rectangle spriteArea)
        {
            SObject obj = this.Value;

            // get texture
            Texture2D spriteSheet;
            if (this.CustomSprite != null)
                spriteSheet = this.CustomSprite.Spritesheet;
            else if (obj is Furniture)
                spriteSheet = Furniture.furnitureTexture;
            else if (obj is Fence)
                spriteSheet = this.Reflection.GetField<Lazy<Texture2D>>(obj, "fenceTexture").GetValue().Value;
            else if (obj.bigCraftable.Value)
                spriteSheet = Game1.bigCraftableSpriteSheet;
            else
                spriteSheet = Game1.objectSpriteSheet;

            // check pixel from sprite sheet
            Rectangle sourceRectangle = this.GetSpritesheetArea();
            SpriteEffects spriteEffects = obj.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            return this.SpriteIntersectsPixel(tile, position, spriteArea, spriteSheet, sourceRectangle, spriteEffects);
        }

        /// <summary>Get the source rectangle for a fence texture.</summary>
        /// <param name="fence">The fence object.</param>
        /// <param name="location">The location containing the fence target.</param>
        /// <remarks>Reverse-engineered from <see cref="Fence.draw(SpriteBatch,int,int,float)"/>.</remarks>
        private Rectangle GetSpritesheetArea(Fence fence, GameLocation location)
        {
            int spriteID = 1;
            if (fence.health.Value > 1.0)
            {
                int index = 0;
                Vector2 tile = fence.TileLocation;

                // connected to right fence
                tile.X += 1;
                if (location.objects.ContainsKey(tile) && location.objects[tile] is Fence && ((Fence)location.objects[tile]).countsForDrawing(fence.whichType.Value))
                    index += 100;

                // connected to left fence
                tile.X -= 2;
                if (location.objects.ContainsKey(tile) && location.objects[tile] is Fence && ((Fence)location.objects[tile]).countsForDrawing(fence.whichType.Value))
                    index += 10;

                // connected to top fence
                tile.X += 1;
                tile.Y += 1;
                if (location.objects.ContainsKey(tile) && location.objects[tile] is Fence && ((Fence)location.objects[tile]).countsForDrawing(fence.whichType.Value))
                    index += 500;

                // connected to bottom fence
                tile.Y -= 2;
                if (location.objects.ContainsKey(tile) && location.objects[tile] is Fence && ((Fence)location.objects[tile]).countsForDrawing(fence.whichType.Value))
                    index += 1000;
                if (fence.isGate.Value)
                {
                    if (index == 110)
                        return new Rectangle(fence.gatePosition.Value == Fence.gateOpenedPosition ? 24 : 0, 128, 24, 32);
                    if (index == 1500)
                        return new Rectangle(fence.gatePosition.Value == Fence.gateClosedPosition ? 16 : 0, 160, 16, 16);
                    spriteID = Fence.sourceRectForSoloGate;
                }
                else
                    spriteID = Fence.fenceDrawGuide[index];
            }

            Texture2D texture = this.Reflection.GetField<Lazy<Texture2D>>(fence, "fenceTexture").GetValue().Value;
            return new Rectangle(spriteID * Fence.fencePieceWidth % texture.Bounds.Width, spriteID * Fence.fencePieceWidth / texture.Bounds.Width * Fence.fencePieceHeight, Fence.fencePieceWidth, Fence.fencePieceHeight);
        }
    }
}
