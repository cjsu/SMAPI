using Microsoft.Xna.Framework;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Constants
{
    /// <summary>Constant mod values.</summary>
    internal static class Constant
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The maximum stack size for which to calculate a stack price (e.g. to avoid showing a stack size for infinite store inventory).</summary>
        public static readonly int MaxStackSizeForPricing = 999;

        /// <summary>Whether bold text should be enabled where needed.</summary>
        /// <remarks>This is disabled for languages like Chinese which are difficult to read in bold.</remarks>
        public static bool AllowBold => Game1.content.GetCurrentLanguage() != LocalizedContentManager.LanguageCode.zh;

        /// <summary>The largest expected sprite size (measured in tiles).</summary>
        /// <remarks>This is used to account for sprites that extend beyond their tile when searching for targets. These values should be large enough to cover the largest target sprites, but small enough to minimise expensive cursor collision checks.</remarks>
        public static readonly Vector2 MaxTargetSpriteSize = new Vector2(3, 5);

        /// <summary>Equivalent to <see cref="MaxTargetSpriteSize"/>, but for building targets.</summary>
        public static readonly Vector2 MaxBuildingTargetSpriteSize = new Vector2(10, 10);

        /// <summary>The <see cref="StardewValley.Farmer.mailReceived"/> keys referenced by the mod.</summary>
        public static class MailLetters
        {
            /// <summary>Set when the spouse gives the player a stardrop.</summary>
            public const string ReceivedSpouseStardrop = "CF_Spouse";

            /// <summary>Set when the player buys a Joja membership, which demolishes the community center.</summary>
            public const string JojaMember = "JojaMember";
        }

        /// <summary>The season names.</summary>
        public static class SeasonNames
        {
            /// <summary>The internal name for Spring.</summary>
            public const string Spring = "spring";

            /// <summary>The internal name for Summer.</summary>
            public const string Summer = "summer";

            /// <summary>The internal name for Fall.</summary>
            public const string Fall = "fall";

            /// <summary>The internal name for Winter.</summary>
            public const string Winter = "winter";
        }

        /// <summary>The names of items referenced by the mod.</summary>
        public static class ItemNames
        {
            /// <summary>The internal name for the heater object.</summary>
            public static string Heater = "Heater";
        }

        /// <summary>The names of buildings referenced by the mod.</summary>
        public static class BuildingNames
        {
            /// <summary>The internal name for the Gold Clock.</summary>
            public static string GoldClock = "Gold Clock";
        }

        /// <summary>The parent sheet indexes referenced by the mod.</summary>
        public static class ObjectIndexes
        {
            /// <summary>The parent sheet index for the auto-grabber.</summary>
            public static int AutoGrabber = 165;
        }
    }
}
