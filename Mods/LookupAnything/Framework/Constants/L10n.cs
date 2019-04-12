using System.Diagnostics.CodeAnalysis;
using StardewValley;

namespace Pathoschild.Stardew.LookupAnything.Framework.Constants
{
    /// <summary>Localisation Keys matching the mod's <c>i18n</c> schema.</summary>
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass", Justification = "Irrelevant in this context.")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Deliberately named to keep translation keys short.")]
    internal static class L10n
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Generic field value translations.</summary>
        public static class Generic
        {
            /// <summary>A value like <c>{{seasonName}} {{dayNumber}}</c>. Expected tokens: <c>{{seasonName}}, {{seasonNumber}}, {{dayNumber}}, {{year}}</c>.</summary>
            public const string Date = "generic.date";

            /// <summary>A value like <c>{{seasonName}} {{dayNumber}} in year {{Year}}</c>. Expected tokens: <c>{{seasonName}}, {{seasonNumber}}, {{dayNumber}}, {{year}}</c>.</summary>
            public const string DateWithYear = "generic.date-with-year";

            /// <summary>A value like <c>{{percent}}%</c>.</summary>
            public const string Percent = "generic.percent";

            /// <summary>A value like <c>{{percent}}% chance of {{label}}</c>.</summary>
            public const string PercentChanceOf = "generic.percent-chance-of";

            /// <summary>A value like <c>{{percent}}% ({{value}} of {{max}})</c>.</summary>
            public const string PercentRatio = "generic.percent-ratio";

            /// <summary>A value like <c>{{value}} of {{max}}</c>.</summary>
            public const string Ratio = "generic.ratio";

            /// <summary>A value like <c>{{min}} to {{max}}</c>.</summary>
            public const string Range = "generic.range";

            /// <summary>A value like <c>yes</c>.</summary>
            public const string Yes = "generic.yes";

            /// <summary>A value like <c>no</c>.</summary>
            public const string No = "generic.no";

            /// <summary>A value like <c>{{count}} seconds</c>.</summary>
            public const string Seconds = "generic.seconds";

            /// <summary>A value like <c>{{count}} minutes</c>.</summary>
            public const string Minutes = "generic.minutes";

            /// <summary>A value like <c>{{count}} hours</c>.</summary>
            public const string Hours = "generic.hours";

            /// <summary>A value like <c>{{count}} days</c>.</summary>
            public const string Days = "generic.days";

            /// <summary>A value like <c>in {{count}} days</c>.</summary>
            public const string InXDays = "generic.in-x-days";

            /// <summary>A value like <c>tomorrow</c>.</summary>
            public const string Tomorrow = "generic.tomorrow";

            /// <summary>A value like <c>{{price}}g</c>.</summary>
            public const string Price = "generic.price";

            /// <summary>A value like <c>{{price}}g ({{quality}})</c>.</summary>
            public const string PriceForQuality = "generic.price-for-quality";

            /// <summary>A value like <c>{{price}}g for stack of {{count}}</c>.</summary>
            public const string PriceForStack = "generic.price-for-stack";
        }

        /// <summary>Lookup subject types.</summary>
        public static class Types
        {
            /// <summary>A value like <c>Building</c>.</summary>
            public const string Building = "type.building";

            /// <summary>A value like <c>{{fruitName}} Tree</c>.</summary>
            public const string FruitTree = "type.fruit-tree";

            /// <summary>A value like <c>Monster</c>.</summary>
            public const string Monster = "type.monster";

            /// <summary>A value like <c>Player</c>.</summary>
            public const string Player = "type.player";

            /// <summary>A value like <c>Map tile</c>.</summary>
            public const string Tile = "type.map-tile";

            /// <summary>A value like <c>Tree</c>.</summary>
            public const string Tree = "type.tree";

            /// <summary>A value like <c>Villager</c>.</summary>
            public const string Villager = "type.villager";

            /// <summary>A value like <c>Other</c>.</summary>
            public const string Other = "type.other";
        }

        /// <summary>Community Center bundle areas.</summary>
        public static class BundleAreas
        {
            /// <summary>A value like <c>Pantry</c>.</summary>
            public const string Pantry = "bundle-area.pantry";

            /// <summary>A value like <c>Crafts Room</c>.</summary>
            public const string CraftsRoom = "bundle-area.crafts-room";

            /// <summary>A value like <c>Fish Tank</c>.</summary>
            public const string FishTank = "bundle-area.fish-tank";

            /// <summary>A value like <c>Boiler Room</c>.</summary>
            public const string BoilerRoom = "bundle-area.boiler-room";

            /// <summary>A value like <c>Vault</c>.</summary>
            public const string Vault = "bundle-area.vault";

            /// <summary>A value like <c>Bulletin Board</c>.</summary>
            public const string BulletinBoard = "bundle-area.bulletin-board";
        }

        /// <summary>Recipe types.</summary>
        public static class RecipeTypes
        {
            /// <summary>A value like <c>Cooking</c>.</summary>
            public const string Cooking = "recipe-type.cooking";

            /// <summary>A value like <c>Crafting</c>.</summary>
            public const string Crafting = "recipe-type.crafting";
        }

        /// <summary>Animal lookup translations.</summary>
        public static class Animal
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Love</c>.</summary>
            public const string Love = "animal.love";

            /// <summary>A value like <c>Happiness</c>.</summary>
            public const string Happiness = "animal.happiness";

            /// <summary>A value like <c>Mood today</c>.</summary>
            public const string Mood = "animal.mood";

            /// <summary>A value like <c>Complaints</c>.</summary>
            public const string Complaints = "animal.complaints";

            /// <summary>A value like <c>Produce ready</c>.</summary>
            public const string ProduceReady = "animal.produce-ready";

            /// <summary>A value like <c>Growth</c>.</summary>
            public const string Growth = "animal.growth";

            /// <summary>A value like <c>Sells for</c>.</summary>
            public const string SellsFor = "animal.sells-for";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>was disturbed by {{name}}</c>.</summary>
            public const string ComplaintsWildAnimalAttack = "animal.complaints.wild-animal-attack";

            /// <summary>A value like <c>wasn't fed yesterday</c>.</summary>
            public const string ComplaintsHungry = "animal.complaints.hungry";

            /// <summary>A value like <c>was left outside last night</c>.</summary>
            public const string ComplaintsLeftOut = "animal.complaints.left-out";

            /// <summary>A value like <c>moved into new home</c>.</summary>
            public const string ComplaintsNewHome = "animal.complaints.new-home";

            /// <summary>A value like <c>no heater in winter</c>.</summary>
            public const string ComplaintsNoHeater = "animal.complaints.no-heater";

            /// <summary>A value like <c>hasn't been petted today</c>.</summary>
            public const string ComplaintsNotPetted = "animal.complaints.not-petted";
        }

        /// <summary>building lookup translations.</summary>
        public static class Building
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Animals</c>.</summary>
            public const string Animals = "building.animals";

            /// <summary>A value like <c>Construction</c>.</summary>
            public const string Construction = "building.construction";

            /// <summary>A value like <c>Feed trough</c>.</summary>
            public const string FeedTrough = "building.feed-trough";

            /// <summary>A value like <c>Horse</c>.</summary>
            public const string Horse = "building.horse";

            /// <summary>A value like <c>Horse</c>.</summary>
            public const string HorseLocation = "building.horse-location";

            /// <summary>A value like <c>Harvesting enabled</c>.</summary>
            public const string JunimoHarvestingEnabled = "building.junimo-harvesting-enabled";

            /// <summary>A value like <c>Owner</c>.</summary>
            public const string Owner = "building.owner";

            /// <summary>A value like <c>Produce ready</c>.</summary>
            public const string OutputProcessing = "building.output-processing";

            /// <summary>A value like <c>Produce ready</c>.</summary>
            public const string OutputReady = "building.output-ready";

            /// <summary>A value like <c>Slimes</c>.</summary>
            public const string Slimes = "building.slimes";

            /// <summary>A value like <c>Stored hay</c>.</summary>
            public const string StoredHay = "building.stored-hay";

            /// <summary>A value like <c>Upgrades</c>.</summary>
            public const string Upgrades = "building.upgrades";

            /// <summary>A value like <c>Water trough</c>.</summary>
            public const string WaterTrough = "building.water-trough";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>{{count}} of max {{max}} animals</c>.</summary>
            public const string AnimalsSummary = "building.animals.summary";

            /// <summary>A value like <c>ready on {{date}}</c>.</summary>
            public const string ConstructionSummary = "building.construction.summary";

            /// <summary>A value like <c>automated</c>.</summary>
            public const string FeedTroughAutomated = "building.feed-trough.automated";

            /// <summary>A value like <c>{{filled}} of {{max}} feed slots filled</c>.</summary>
            public const string FeedTroughSummary = "building.feed-trough.summary";

            /// <summary>A value like <c>{{location}} ({{x}}, {{y}})</c>.</summary>
            public const string HorseLocationSummary = "building.horse-location.summary";

            /// <summary>A value like <c>no owner</c>.</summary>
            public const string OwnerNone = "building.owner.none";

            /// <summary>A value like <c>{{count}} of max {{max}} slimes</c>.</summary>
            public const string SlimesSummary = "building.slimes.summary";

            /// <summary>A value like <c>{{hayCount}} hay (max capacity: {{maxHay}})</c>.</summary>
            public const string StoredHaySummaryOneSilo = "building.stored-hay.summary-one-silo";

            /// <summary>A value like <c>{{hayCount}} hay in {{siloCount}} silos (max capacity: {{maxHay}})</c>.</summary>
            public const string StoredHaySummaryMultipleSilos = "building.stored-hay.summary-multiple-silos";

            /// <summary>A value like <c>up to 4 animals, add cows</c>.</summary>
            public const string UpgradesBarn0 = "building.upgrades.barn.0";

            /// <summary>A value like <c>up to 8 animals, add pregnancy and goats</c>.</summary>
            public const string UpgradesBarn1 = "building.upgrades.barn.1";

            /// <summary>A value like <c>up to 12 animals, add autofeed, pigs, and sheep"</c>.</summary>
            public const string UpgradesBarn2 = "building.upgrades.barn.2";

            /// <summary>A value like <c>initial cabin</c>.</summary>
            public const string UpgradesCabin0 = "building.upgrades.cabin.0";

            /// <summary>A value like <c>add kitchen, enable marriage</c>.</summary>
            public const string UpgradesCabin1 = "building.upgrades.cabin.1";

            /// <summary>A value like <c>enable children</c>.</summary>
            public const string UpgradesCabin2 = "building.upgrades.cabin.2";

            /// <summary>A value like <c>up to 4 animals; add chickens</c>.</summary>
            public const string UpgradesCoop0 = "building.upgrades.coop.0";

            /// <summary>A value like <c>up to 8 animals; add incubator, dinosaurs, and ducks</c>.</summary>
            public const string UpgradesCoop1 = "building.upgrades.coop.1";

            /// <summary>A value like <c>up to 12 animals; add autofeed and rabbits</c>.</summary>
            public const string UpgradesCoop2 = "building.upgrades.coop.2";

            /// <summary>A value like <c>{{filled}} of {{max}} water troughs filled</c>.</summary>
            public const string WaterTroughSummary = "building.water-trough.summary";
        }

        /// <summary>Fruit tree lookup translations.</summary>
        public static class FruitTree
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Complaints</c>.</summary>
            public const string Complaints = "fruit-tree.complaints";

            /// <summary>A value like <c>Growth</c>.</summary>
            public const string Growth = "fruit-tree.growth";

            /// <summary>A value like <c>{{fruitName}} Tree</c>.</summary>
            public const string Name = "fruit-tree.name";

            /// <summary>A value like <c>Next fruit</c>.</summary>
            public const string NextFruit = "fruit-tree.next-fruit";

            /// <summary>A value like <c>Season</c>.</summary>
            public const string Season = "fruit-tree.season";

            /// <summary>A value like <c>Quality</c>.</summary>
            public const string Quality = "fruit-tree.quality";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>can't grow because there are adjacent objects</c>.</summary>
            public const string ComplaintsAdjacentObjects = "fruit-tree.complaints.adjacent-objects";

            /// <summary>A value like <c>mature on {{date}}</c>.</summary>
            public const string GrowthSummary = "fruit-tree.growth.summary";

            /// <summary>A value like <c>struck by lightning! Will recover in {{count}} days.</c>.</summary>
            public const string NextFruitStruckByLightning = "fruit-tree.next-fruit.struck-by-lightning";

            /// <summary>A value like <c>out of season</c>.</summary>
            public const string NextFruitOutOfSeason = "fruit-tree.next-fruit.out-of-season";

            /// <summary>A value like <c>won't grow any more fruit until you harvest those it has</c>.</summary>
            public const string NextFruitMaxFruit = "fruit-tree.next-fruit.max-fruit";

            /// <summary>A value like <c>too young to bear fruit</c>.</summary>
            public const string NextFruitTooYoung = "fruit-tree.next-fruit.too-young";

            /// <summary>A value like <c>{{quality}} now</c>.</summary>
            public const string QualityNow = "fruit-tree.quality.now";

            /// <summary>A value like <c>{{quality}} on {{date}}</c>.</summary>
            public const string QualityOnDate = "fruit-tree.quality.on-date";

            /// <summary>A value like <c>{{quality}} on {{date}} next year</c>.</summary>
            public const string QualityOnDateNextYear = "fruit-tree.quality.on-date-next-year";

            /// <summary>A value like <c>{{season}} (or anytime in greenhouse)</c>.</summary>
            public const string SeasonSummary = "fruit-tree.season.summary";
        }

        /// <summary>Crop lookup translations.</summary>
        public static class Crop
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Crop</c>.</summary>
            public const string Summary = "crop.summary";

            /// <summary>A value like <c>Harvest</c>.</summary>
            public const string Harvest = "crop.harvest";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>This crop is dead.</c>.</summary>
            public const string SummaryDead = "crop.summary.dead";

            /// <summary>A value like <c>drops {{count}}</c>.</summary>
            public const string SummaryDropsX = "crop.summary.drops-x";

            /// <summary>A value like <c>drops {{min}} to {{max}} ({{percent}}% chance of extra crops)</c>.</summary>
            public const string SummaryDropsXToY = "crop.summary.drops-x-to-y";

            /// <summary>A value like <c>harvest after {{daysToFirstHarvest}} days</c>.</summary>
            public const string SummaryHarvestOnce = "crop.summary.harvest-once";

            /// <summary>A value like <c>harvest after {{daysToFirstHarvest}} days, then every {{daysToNextHarvests}} days</c>.</summary>
            public const string SummaryHarvestMulti = "crop.summary.harvest-multi";

            /// <summary>A value like <c>grows in {{seasons}}</c>.</summary>
            public const string SummarySeasons = "crop.summary.seasons";

            /// <summary>A value like <c>sells for {{price}}</c>.</summary>
            public const string SummarySellsFor = "crop.summary.sells-for";

            /// <summary>A value like <c>now</c>.</summary>
            public const string HarvestNow = "crop.harvest.now";

            /// <summary>A value like <c>too late in the season for the next harvest (would be on {{date}})</c>.</summary>
            public const string HarvestTooLate = "crop.harvest.too-late";
        }

        /// <summary>Item lookup translations.</summary>
        public static class Item
        {
            /// <summary>A value like <c>Aging</c>.</summary>
            public const string CaskSchedule = "item.cask-schedule";

            /// <summary>A value like <c>Bait</c>.</summary>
            public const string CrabpotBait = "item.crabpot-bait";

            /// <summary>A value like <c>Needs bait!</c>.</summary>
            public const string CrabpotBaitNeeded = "item.crabpot-bait-needed";

            /// <summary>A value like <c>Not needed due to Luremaster profession.</c>.</summary>
            public const string CrabpotBaitNotNeeded = "item.crabpot-bait-not-needed";

            /// <summary>A value like <c>Contents</c>.</summary>
            public const string Contents = "item.contents";

            /// <summary>A value like <c>Needed for</c>.</summary>
            public const string NeededFor = "item.needed-for";

            /// <summary>A value like <c>Sells for</c>.</summary>
            public const string SellsFor = "item.sells-for";

            /// <summary>A value like <c>Sells to</c>.</summary>
            public const string SellsTo = "item.sells-to";

            /// <summary>A value like <c>Likes this</c>.</summary>
            public const string LikesThis = "item.likes-this";

            /// <summary>A value like <c>Loves this</c>.</summary>
            public const string LovesThis = "item.loves-this";

            /// <summary>A value like <c>Health</c>.</summary>
            public const string FenceHealth = "item.fence-health";

            /// <summary>A value like <c>Recipes</c>.</summary>
            public const string Recipes = "item.recipes";

            /// <summary>A value like <c>Owned</c>.</summary>
            public const string Owned = "item.number-owned";

            /// <summary>A value like <c>Cooked</c>.</summary>
            public const string Cooked = "item.number-cooked";

            /// <summary>A value like <c>Crafted</c>.</summary>
            public const string Crafted = "item.number-crafted";

            /// <summary>A value like <c>See also</c>.</summary>
            public const string SeeAlso = "item.see-also";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>{{quality}} ready now</c>.</summary>
            public const string CaskScheduleNow = "item.cask-schedule.now";

            /// <summary>A value like <c>{{quality}} now (use pickaxe to stop aging)</c>.</summary>
            public const string CaskSchedulePartial = "item.cask-schedule.now-partial";

            /// <summary>A value like <c>{{quality}} tomorrow</c>.</summary>
            public const string CaskScheduleTomorrow = "item.cask-schedule.tomorrow";

            /// <summary>A value like <c>{{quality}} in {{count}} days ({{date}})</c>.</summary>
            public const string CaskScheduleInXDays = "item.cask-schedule.in-x-days";

            /// <summary>A value like <c>has {{name}}</c>.</summary>
            public const string ContentsPlaced = "item.contents.placed";

            /// <summary>A value like <c>{{name}} ready</c>.</summary>
            public const string ContentsReady = "item.contents.ready";

            /// <summary>A value like <c>{{name}} in {{time}}</c>.</summary>
            public const string ContentsPartial = "item.contents.partial";

            /// <summary>A value like <c>community center ({{bundles}})</c>.</summary>
            public const string NeededForCommunityCenter = "item.needed-for.community-center";

            /// <summary>A value like <c>full shipment achievement (ship one)</c>.</summary>
            public const string NeededForFullShipment = "item.needed-for.full-shipment";

            /// <summary>A value like <c>polyculture achievement (ship {{count}} more)</c>.</summary>
            public const string NeededForPolyculture = "item.needed-for.polyculture";

            /// <summary>A value like <c>full collection achievement (donate one to museum)</c>.</summary>
            public const string NeededForFullCollection = "item.needed-for.full-collection";

            /// <summary>A value like <c>gourmet chef achievement (cook {{recipes}})</c>.</summary>
            public const string NeededForGourmetChef = "item.needed-for.gourmet-chef";

            /// <summary>A value like <c>craft master achievement (make {{recipes}})</c>.</summary>
            public const string NeededForCraftMaster = "item.needed-for.craft-master";

            /// <summary>A value like <c>shipping box</c>.</summary>
            public const string SellsToShippingBox = "item.sells-to.shipping-box";

            /// <summary>A value like <c>no decay with Gold Clock</c>.</summary>
            public const string FenceHealthGoldClock = "item.fence-health.gold-clock";

            /// <summary>A value like <c>{{percent}}% (roughly {{count}} days left)</c>.</summary>
            public const string FenceHealthSummary = "item.fence-health.summary";

            /// <summary>A value like <c>{{name}} (needs {{count}})</c>.</summary>
            public const string RecipesEntry = "item.recipes.entry";

            /// <summary>A value like <c>you own {{count}} of these</c>.</summary>
            public const string OwnedSummary = "item.number-owned.summary";

            /// <summary>A value like <c>you made {{count}} of these</c>.</summary>
            public const string CraftedSummary = "item.number-crafted.summary";
        }

        /// <summary>Monster lookup translations.</summary>
        public static class Monster
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Invincible</c>.</summary>
            public const string Invincible = "monster.invincible";

            /// <summary>A value like <c>Health</c>.</summary>
            public const string Health = "monster.health";

            /// <summary>A value like <c>Drops</c>.</summary>
            public const string Drops = "monster.drops";

            /// <summary>A value like <c>XP</c>.</summary>
            public const string Experience = "monster.experience";

            /// <summary>A value like <c>Defence</c>.</summary>
            public const string Defence = "monster.defence";

            /// <summary>A value like <c>Attack</c>.</summary>
            public const string Attack = "monster.attack";

            /// <summary>A value like <c>Adventure Guild</c>.</summary>
            public const string AdventureGuild = "monster.adventure-guild";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>nothing</c>.</summary>
            public const string DropsNothing = "monster.drops.nothing";

            /// <summary>A value like <c>complete</c>.</summary>
            public const string AdventureGuildComplete = "monster.adventure-guild.complete";

            /// <summary>A value like <c>in progress</c>.</summary>
            public const string AdventureGuildIncomplete = "monster.adventure-guild.incomplete";

            /// <summary>A value like <c>killed {{count}} of {{requiredCount}}</c>.</summary>
            public const string AdventureGuildProgress = "monster.adventure-guild.progress";
        }

        /// <summary>NPC lookup translations.</summary>
        public static class Npc
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Birthday</c>.</summary>
            public const string Birthday = "npc.birthday";

            /// <summary>A value like <c>Can romance</c>.</summary>
            public const string CanRomance = "npc.can-romance";

            /// <summary>A value like <c>Friendship</c>.</summary>
            public const string Friendship = "npc.friendship";

            /// <summary>A value like <c>Talked today</c>.</summary>
            public const string TalkedToday = "npc.talked-today";

            /// <summary>A value like <c>Gifted today</c>.</summary>
            public const string GiftedToday = "npc.gifted-today";

            /// <summary>A value like <c>Gifted this week</c>.</summary>
            public const string GiftedThisWeek = "npc.gifted-this-week";

            /// <summary>A value like <c>Likes gifts</c>.</summary>
            public const string LikesGifts = "npc.likes-gifts";

            /// <summary>A value like <c>Loves gifts</c>.</summary>
            public const string LovesGifts = "npc.loves-gifts";

            /// <summary>A value like <c>Neutral gifts</c>.</summary>
            public const string NeutralGifts = "npc.neutral-gifts";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>You're married! &lt;</c>.</summary>
            public const string CanRomanceMarried = "npc.can-romance.married";

            /// <summary>A value like <c>You haven't met them yet.</c>.</summary>
            public const string FriendshipNotMet = "npc.friendship.not-met";

            /// <summary>A value like <c>need bouquet for next</c>.</summary>
            public const string FriendshipNeedBouquet = "npc.friendship.need-bouquet";

            /// <summary>A value like <c>next in {{count}} pts</c>.</summary>
            public const string FriendshipNeedPoints = "npc.friendship.need-points";
        }

        /// <summary>NPC child lookup translations.</summary>
        public static class NpcChild
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Age</c>.</summary>
            public const string Age = "npc.child.age";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>{{label}} ({{count}} days to {{nextLabel}})</c>.</summary>
            public const string AgeDescriptionPartial = "npc.child.age.description-partial";

            /// <summary>A value like <c>{{label}}</c>.</summary>
            public const string AgeDescriptionGrown = "npc.child.age.description-grown";

            /// <summary>A value like <c>newborn</c>.</summary>
            public const string AgeNewborn = "npc.child.age.newborn";

            /// <summary>A value like <c>baby</c>.</summary>
            public const string AgeBaby = "npc.child.age.baby";

            /// <summary>A value like <c>crawler</c>.</summary>
            public const string AgeCrawler = "npc.child.age.crawler";

            /// <summary>A value like <c>toddler</c>.</summary>
            public const string AgeToddler = "npc.child.age.toddler";
        }

        /// <summary>Pet lookup translations.</summary>
        public static class Pet
        {
            /// <summary>A value like <c>Love</c>.</summary>
            public const string Love = "pet.love";

            /// <summary>A value like <c>Petted today</c>.</summary>
            public const string PettedToday = "pet.petted-today";
        }

        /// <summary>Player lookup translations.</summary>
        public static class Player
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Farm name</c>.</summary>
            public const string FarmName = "player.farm-name";

            /// <summary>A value like <c>Farm map</c>.</summary>
            public const string FarmMap = "player.farm-map";

            /// <summary>A value like <c>Favourite thing</c>.</summary>
            public const string FavoriteThing = "player.favorite-thing";

            /// <summary>A value like <c>Gender</c>.</summary>
            public const string Gender = "player.gender";

            /// <summary>A value like <c>Spouse</c>.</summary>
            public const string Spouse = "player.spouse";

            /// <summary>A value like <c>Combat skill</c>.</summary>
            public const string CombatSkill = "player.combat-skill";

            /// <summary>A value like <c>Farming skill</c>.</summary>
            public const string FarmingSkill = "player.farming-skill";

            /// <summary>A value like <c>Foraging skill</c>.</summary>
            public const string ForagingSkill = "player.foraging-skill";

            /// <summary>A value like <c>Fishing skill</c>.</summary>
            public const string FishingSkill = "player.fishing-skill";

            /// <summary>A value like <c>Mining skill</c>.</summary>
            public const string MiningSkill = "player.mining-skill";

            /// <summary>A value like <c>Luck</c>.</summary>
            public const string Luck = "player.luck";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>Custom</c>.</summary>
            public const string FarmMapCustom = "player.farm-map.custom";

            /// <summary>A value like <c>male</c>.</summary>
            public const string GenderMale = "player.gender.male";

            /// <summary>A value like <c>female</c>.</summary>
            public const string GenderFemale = "player.gender.female";

            /// <summary>A value like <c>({{percent}}% to many random checks)</c>.</summary>
            public const string LuckSummary = "player.luck.summary";

            /// <summary>A value like <c>level {{level}} ({{expNeeded}} XP to next)</c>.</summary>
            public const string SkillProgress = "player.skill.progress";

            /// <summary>A value like <c>level {{level}}</c>.</summary>
            public const string SkillProgressLast = "player.skill.progress-last";
        }

        /// <summary>Tile lookup translations.</summary>
        public static class Tile
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>A tile position on the map. This is displayed because you enabled tile lookups in the configuration.</c>.</summary>
            public const string Description = "tile.description";

            /// <summary>A value like <c>Map name</c>.</summary>
            public const string MapName = "tile.map-name";

            /// <summary>A value like <c>Tile</c>.</summary>
            public const string TileField = "tile.tile";

            /// <summary>A value like <c>{{layerName}}: tile index</c>.</summary>
            public const string TileIndex = "tile.tile-index";

            /// <summary>A value like <c>{{layerName}}: tilesheet</c>.</summary>
            public const string TileSheet = "tile.tilesheet";

            /// <summary>A value like <c>{{layerName}}: blend mode</c>.</summary>
            public const string BlendMode = "tile.blend-mode";

            /// <summary>A value like <c>{{layerName}}: ix props: {{propertyName}}</c>.</summary>
            public const string IndexProperty = "tile.index-property";

            /// <summary>A value like <c>{{layerName}}: props: {{propertyName}}</c>.</summary>
            public const string TileProperty = "tile.tile-property";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>no tile here</c>.</summary>
            public const string TileFieldNoneFound = "tile.tile.none-here";
        }

        /// <summary>Wild tree lookup translations.</summary>
        public static class Tree
        {
            /****
            ** Labels
            ****/
            /// <summary>A value like <c>Maple Tree</c>.</summary>
            public const string NameMaple = "tree.name.maple";

            /// <summary>A value like <c>Oak Tree</c>.</summary>
            public const string NameOak = "tree.name.oak";

            /// <summary>A value like <c>Pine Tree</c>.</summary>
            public const string NamePine = "tree.name.pine";

            /// <summary>A value like <c>Palm Tree</c>.</summary>
            public const string NamePalm = "tree.name.palm";

            /// <summary>A value like <c>Big Mushroom</c>.</summary>
            public const string NameBigMushroom = "tree.name.big-mushroom";

            /// <summary>A value like <c>Unknown Tree</c>.</summary>
            public const string NameUnknown = "tree.name.unknown";

            /// <summary>A value like <c>Growth stage</c>.</summary>
            public const string Stage = "tree.stage";

            /// <summary>A value like <c>Next growth</c>.</summary>
            public const string NextGrowth = "tree.next-growth";

            /// <summary>A value like <c>Has seed</c>.</summary>
            public const string HasSeed = "tree.has-seed";

            /****
            ** Values
            ****/
            /// <summary>A value like <c>Fully grown</c>.</summary>
            public const string StageDone = "tree.stage.done";

            /// <summary>A value like <c>{{stageName}} ({{step}} of {{max}})</c>.</summary>
            public const string StagePartial = "tree.stage.partial";

            /// <summary>A value like <c>can't grow in winter outside greenhouse</c>.</summary>
            public const string NextGrowthWinter = "tree.next-growth.winter";

            /// <summary>A value like <c>can't grow because other trees are too close</c>.</summary>
            public const string NextGrowthAdjacentTrees = "tree.next-growth.adjacent-trees";

            /// <summary>A value like <c>20% chance to grow into {{stage}} tomorrow</c>.</summary>
            public const string NextGrowthRandom = "tree.next-growth.random";
        }

        /*********
        ** Public methods
        *********/
        /// <summary>Get a translation key for an enum value.</summary>
        /// <param name="stage">The tree growth stage.</param>
        public static string For(WildTreeGrowthStage stage)
        {
            return $"tree.stages.{stage}";
        }

        /// <summary>Get a translation key for an enum value.</summary>
        /// <param name="quality">The item quality.</param>
        public static string For(ItemQuality quality)
        {
            return $"quality.{quality.GetName()}";
        }

        /// <summary>Get a translation key for an enum value.</summary>
        /// <param name="status">The friendship status.</param>
        public static string For(FriendshipStatus status)
        {
            return $"friendship-status.{status.ToString().ToLower()}";
        }

        /// <summary>Get a translation key for an enum value.</summary>
        /// <param name="age">The child age.</param>
        public static string For(ChildAge age)
        {
            return $"npc.child.age.{age.ToString().ToLower()}";
        }
    }
}
