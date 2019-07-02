using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class DebrisMethods : Debris
    {
        public DebrisMethods(Item item, Vector2 debrisOrigin)
            : base()
        {
            base.init(item, debrisOrigin);
        }

        public DebrisMethods(int objectIndex, Vector2 debrisOrigin, Vector2 playerPosition)
            : base()
        {
            base.init(objectIndex, debrisOrigin, playerPosition);
        }

        public DebrisMethods(Item item, Vector2 debrisOrigin, Vector2 targetLocation)
            : base()
        {
            base.init(item, debrisOrigin, targetLocation);
        }

        public DebrisMethods(string spriteSheet, int numberOfChunks, Vector2 debrisOrigin)
            : base()
        {
            base.init(spriteSheet, numberOfChunks, debrisOrigin);
        }

        public DebrisMethods(string spriteSheet, Rectangle sourceRect, int numberOfChunks, Vector2 debrisOrigin)
            : base()
        {
            base.init(spriteSheet, sourceRect, numberOfChunks, debrisOrigin);
        }

        public DebrisMethods(int debrisType, int numberOfChunks, Vector2 debrisOrigin, Vector2 playerPosition)
            : base()
        {
            base.init(debrisType, numberOfChunks, debrisOrigin, playerPosition);
        }

        public DebrisMethods(int number, Vector2 debrisOrigin, Color messageColor, float scale, Character toHover)
            : base()
        {
            base.init(number, debrisOrigin, messageColor, scale, toHover);
        }

        public DebrisMethods(int debrisType, int numberOfChunks, Vector2 debrisOrigin, Vector2 playerPosition, float velocityMultiplayer)
            : base()
        {
            base.init(debrisType, numberOfChunks, debrisOrigin, playerPosition, velocityMultiplayer);
        }

        public DebrisMethods(string message, int numberOfChunks, Vector2 debrisOrigin, Color messageColor, float scale, float rotation)
            : base()
        {
            base.init(message, numberOfChunks, debrisOrigin, messageColor, scale, rotation);
        }

        public DebrisMethods(string spriteSheet, Rectangle sourceRect, int numberOfChunks, Vector2 debrisOrigin, Vector2 playerPosition, int groundLevel)
            : base()
        {
            base.init(spriteSheet, sourceRect, numberOfChunks, debrisOrigin, playerPosition, groundLevel);
        }

        public DebrisMethods(int type, int numberOfChunks, Vector2 debrisOrigin, Vector2 playerPosition, int groundLevel, int color = -1)
            : base()
        {
            base.init(type, numberOfChunks, debrisOrigin, playerPosition, groundLevel, color);
        }
        public DebrisMethods(string spriteSheet, Rectangle sourceRect, int numberOfChunks, Vector2 debrisOrigin, Vector2 playerPosition, int groundLevel, int sizeOfSourceRectSquares)
           : base()
        {
            base.init(spriteSheet, sourceRect, numberOfChunks, debrisOrigin, playerPosition, groundLevel, sizeOfSourceRectSquares);
        }
    }
}
