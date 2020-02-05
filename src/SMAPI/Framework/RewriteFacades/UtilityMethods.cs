using System.Diagnostics.CodeAnalysis;
using StardewValley;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class UtilityMethods : Utility
    {
        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new void trashItem(Item item)
        {
            trashItem(item, -1);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new int getTrashReclamationPrice(Item item, Farmer player)
        {
            return getTrashReclamationPrice(item, player, -1);
        }

        [SuppressMessage("ReSharper", "CS0109", Justification = "The 'new' modifier applies when compiled on Windows.")]
        public static new int getRandomItemFromSeason(string season, int randomSeedAddition, bool forQuest, bool changeDaily = true)
        {
            return getRandomItemFromSeason(season, randomSeedAddition, forQuest);
        }
    }
}
