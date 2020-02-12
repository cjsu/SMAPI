using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using StardewValley;
using StardewValley.Locations;

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
        public static int getRandomItemFromSeason(string season, int randomSeedAddition, bool forQuest, bool changeDaily = true)
        {
            Random random = new Random((int)Game1.uniqueIDForThisGame + (changeDaily ? (int)Game1.stats.DaysPlayed : 0) + randomSeedAddition);
            List<int> source = new List<int>() { 68, 66, 78, 80, 86, 152, 167, 153, 420 };
            List<string> stringList1 = new List<string>(Game1.player.craftingRecipes.Keys);
            List<string> stringList2 = new List<string>(Game1.player.cookingRecipes.Keys);
            if (forQuest)
            {
                stringList1 = Utility.GetAllPlayerUnlockedCraftingRecipes();
                stringList2 = Utility.GetAllPlayerUnlockedCookingRecipes();
            }
            if (forQuest && (MineShaft.lowestLevelReached > 40 || Utility.GetAllPlayerDeepestMineLevel() >= 1) || !forQuest && (Game1.player.deepestMineLevel > 40 || Game1.player.timesReachedMineBottom >= 1))
                source.AddRange(new int[5] { 62, 70, 72, 84, 422 });
            if (forQuest && (MineShaft.lowestLevelReached > 80 || Utility.GetAllPlayerDeepestMineLevel() >= 1) || !forQuest && (Game1.player.deepestMineLevel > 80 || Game1.player.timesReachedMineBottom >= 1))
                source.AddRange(new int[3] { 64, 60, 82 });
            if (Utility.doesAnyFarmerHaveMail("ccVault"))
                source.AddRange(new int[4] { 88, 90, 164, 165 });
            if (stringList1.Contains("Furnace"))
                source.AddRange(new int[4] { 334, 335, 336, 338 });
            if (stringList1.Contains("Quartz Globe"))
                source.Add(339);
            if (season.Equals("spring"))
                source.AddRange(new int[17] { 0x10, 0x12, 20, 0x16, 0x81, 0x83, 0x84, 0x88, 0x89, 0x8e, 0x8f, 0x91, 0x93, 0x94, 0x98, 0xa7, 0x10b });
            else if (season.Equals("summer"))
                source.AddRange(new int[] { 0x80, 130, 0x84, 0x88, 0x8a, 0x8e, 0x90, 0x91, 0x92, 0x95, 150, 0x9b, 0x18c, 0x18e, 0x192, 0x10b });
            else if (season.Equals("fall"))
                source.AddRange(new int[] { 0x194, 0x196, 0x198, 410, 0x81, 0x83, 0x84, 0x88, 0x89, 0x8b, 140, 0x8e, 0x8f, 0x94, 150, 0x9a, 0x9b, 0x10d });
            else if (season.Equals("winter"))
                source.AddRange(new int[] { 0x19c, 0x19e, 0x1a0, 0x1a2, 130, 0x83, 0x84, 0x88, 140, 0x8d, 0x90, 0x92, 0x93, 150, 0x97, 0x9a, 0x10d });
            if (forQuest)
            {
                foreach (string key in stringList2)
                {
                    if (random.NextDouble() >= 0.4)
                    {
                        List<int> intList = Utility.possibleCropsAtThisTime(Game1.currentSeason, Game1.dayOfMonth <= 7);
                        Dictionary<string, string> dictionary = Game1.content.Load<Dictionary<string, string>>("Data//CookingRecipes");
                        if (dictionary.ContainsKey(key))
                        {
                            string[] strArray = dictionary[key].Split('/')[0].Split(' ');
                            bool flag = true;
                            for (int index = 0; index < strArray.Length; ++index)
                            {
                                MethodInfo isCategoryIngredientAvailable = typeof(Utility).GetMethod("isCategoryIngredientAvailable", BindingFlags.NonPublic | BindingFlags.Static);
                                if (!source.Contains(Convert.ToInt32(strArray[index]))
                                    && !(bool)isCategoryIngredientAvailable.Invoke(null, new object[] { Convert.ToInt32(strArray[index]) })
                                    && (intList == null || !intList.Contains(Convert.ToInt32(strArray[index]))))
                                {
                                    flag = false;
                                    break;
                                }
                            }
                            if (flag)
                                source.Add(Convert.ToInt32(dictionary[key].Split('/')[2]));
                        }
                    }
                }
            }
            return source[random.Next(source.Count)];
        }
    }
}
