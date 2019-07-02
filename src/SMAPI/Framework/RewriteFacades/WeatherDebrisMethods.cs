using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class WeatherDebrisMethods : WeatherDebris
    {
        public WeatherDebrisMethods(Vector2 position, int which, float rotationVelocity, float dx, float dy)
            : base(position, which, dx, dy)
        {
        }
    }
}
