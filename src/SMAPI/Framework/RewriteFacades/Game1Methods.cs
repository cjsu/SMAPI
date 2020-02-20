using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class Game1Methods : Game1
    {
        public static RainDrop[] rainDrops => (typeof(RainManager).GetField("_rainDropList", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(RainManager.Instance) as List<RainDrop>).ToArray();
        public static new IList<IClickableMenu> onScreenMenus => Game1.onScreenMenus;

        public static bool IsRainingProp
        {
            get
            {
                return RainManager.Instance.isRaining;
            }
            set
            {
                RainManager.Instance.isRaining = value;
            }
        }

        public static bool IsSnowingProp
        {
            get
            {
                return WeatherDebrisManager.Instance.isSnowing;
            }
            set
            {
                WeatherDebrisManager.Instance.isSnowing = value;
            }
        }

        public static bool IsDebrisWeatherProp
        {
            get
            {
                return WeatherDebrisManager.Instance.isDebrisWeather;
            }
            set
            {
                WeatherDebrisManager.Instance.isDebrisWeather = value;
            }
        }

        public static void updateDebrisWeatherForMovement(List<WeatherDebris> debris)
        {
            WeatherDebrisManager.Instance.UpdateDebrisWeatherForMovement();
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new string parseText(string text, SpriteFont whichFont, int width)
        {
            return parseText(text, whichFont, width, 1);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(LocationRequest locationRequest, int tileX, int tileY, int facingDirectionAfterWarp)
        {
            warpFarmer(locationRequest, tileX, tileY, facingDirectionAfterWarp, true, false);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(string locationName, int tileX, int tileY, bool flip)
        {
            warpFarmer(locationName, tileX, tileY, flip ? ((player.FacingDirection + 2) % 4) : player.FacingDirection);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void warpFarmer(string locationName, int tileX, int tileY, int facingDirectionAfterWarp)
        {
            warpFarmer(locationName, tileX, tileY, facingDirectionAfterWarp, false, true, false);
        }

        public static void panScreen(int x, int y)
        {
            panScreen(x, y, 0);
        }
    }
}
