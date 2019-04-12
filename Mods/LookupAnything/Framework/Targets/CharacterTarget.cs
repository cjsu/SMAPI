using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Monsters;

namespace Pathoschild.Stardew.LookupAnything.Framework.Targets
{
    /// <summary>Positional metadata about an NPC.</summary>
    internal class CharacterTarget : GenericTarget<NPC>
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private game code.</summary>
        private readonly IReflectionHelper Reflection;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="type">The target type.</param>
        /// <param name="value">The underlying in-game entity.</param>
        /// <param name="tilePosition">The object's tile position in the current location (if applicable).</param>
        /// <param name="reflectionHelper">Simplifies access to private game code.</param>
        public CharacterTarget(GameHelper gameHelper, TargetType type, NPC value, Vector2? tilePosition, IReflectionHelper reflectionHelper)
            : base(gameHelper, type, value, tilePosition)
        {
            this.Reflection = reflectionHelper;
        }

        /// <summary>Get the sprite's source rectangle within its texture.</summary>
        public override Rectangle GetSpritesheetArea()
        {
            return this.Value.Sprite.SourceRect;
        }

        /// <summary>Get a rectangle which roughly bounds the visible sprite relative the viewport.</summary>
        public override Rectangle GetWorldArea()
        {
            NPC npc = this.Value;
            AnimatedSprite sprite = npc.Sprite;
            var boundingBox = npc.GetBoundingBox(); // the 'occupied' area at the NPC's feet

            // calculate y origin
            float yOrigin;
            if (npc is DustSpirit)
                yOrigin = boundingBox.Bottom;
            else if (npc is Bat)
                yOrigin = boundingBox.Center.Y;
            else if (npc is Bug)
                yOrigin = boundingBox.Top - sprite.SpriteHeight * Game1.pixelZoom + (float)(System.Math.Sin(Game1.currentGameTime.TotalGameTime.Milliseconds / 1000.0 * (2.0 * System.Math.PI)) * 10.0);
            else if (npc is SquidKid squidKid)
            {
                int yOffset = this.Reflection.GetField<int>(squidKid, "yOffset").GetValue();
                yOrigin = boundingBox.Bottom - sprite.SpriteHeight * Game1.pixelZoom + yOffset;
            }
            else
                yOrigin = boundingBox.Top;

            // get bounding box
            int height = sprite.SpriteHeight * Game1.pixelZoom;
            int width = sprite.SpriteWidth * Game1.pixelZoom;
            float x = boundingBox.Center.X - (width / 2);
            float y = yOrigin + boundingBox.Height - height + npc.yJumpOffset * 2;

            return new Rectangle((int)(x - Game1.viewport.X), (int)(y - Game1.viewport.Y), width, height);
        }

        /// <summary>Get whether the visible sprite intersects the specified coordinate. This can be an expensive test.</summary>
        /// <param name="tile">The tile to search.</param>
        /// <param name="position">The viewport-relative coordinates to search.</param>
        /// <param name="spriteArea">The approximate sprite area calculated by <see cref="GetWorldArea"/>.</param>
        public override bool SpriteIntersectsPixel(Vector2 tile, Vector2 position, Rectangle spriteArea)
        {
            NPC npc = this.Value;
            AnimatedSprite sprite = npc.Sprite;

            // allow any part of the sprite area for monsters
            // (Monsters have complicated and inconsistent sprite behaviour which isn't really
            // worth reverse-engineering, and sometimes move around so much that a pixel-perfect
            // check is inconvenient anyway.)
            if (npc is Monster)
                return spriteArea.Contains((int)position.X, (int)position.Y);

            // check sprite for non-monster NPCs
            SpriteEffects spriteEffects = npc.flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            return this.SpriteIntersectsPixel(tile, position, spriteArea, sprite.Texture, sprite.sourceRect, spriteEffects);
        }
    }
}
