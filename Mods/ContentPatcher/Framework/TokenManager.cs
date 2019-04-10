using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ContentPatcher.Framework.Conditions;
using ContentPatcher.Framework.Constants;
using ContentPatcher.Framework.Tokens;
using ContentPatcher.Framework.Tokens.ValueProviders;
using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace ContentPatcher.Framework
{
    /// <summary>Manages the available contextual tokens.</summary>
    internal class TokenManager : IContext
    {
        /*********
        ** Fields
        *********/
        /// <summary>The available global tokens.</summary>
        private readonly GenericTokenContext GlobalContext = new GenericTokenContext();

        /// <summary>The available tokens defined within the context of each content pack.</summary>
        private readonly Dictionary<IContentPack, ModTokenContext> LocalTokens = new Dictionary<IContentPack, ModTokenContext>();


        /*********
        ** Accessors
        *********/
        /// <summary>Whether the basic save info is loaded (including the date, weather, and player info). The in-game locations and world may not exist yet.</summary>
        public bool IsBasicInfoLoaded { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="contentHelper">The content helper from which to load data assets.</param>
        /// <param name="installedMods">The installed mod IDs.</param>
        public TokenManager(IContentHelper contentHelper, IEnumerable<string> installedMods)
        {
            foreach (IValueProvider valueProvider in this.GetGlobalValueProviders(contentHelper, installedMods))
                this.GlobalContext.Tokens[new TokenName(valueProvider.Name)] = new GenericToken(valueProvider);
        }

        /// <summary>Get the tokens which are defined for a specific content pack. This returns a reference to the list, which can be held for a live view of the tokens. If the content pack isn't currently tracked, this will add it.</summary>
        /// <param name="contentPack">The content pack to manage.</param>
        public ModTokenContext TrackLocalTokens(IContentPack contentPack)
        {
            if (!this.LocalTokens.TryGetValue(contentPack, out ModTokenContext localTokens))
            {
                this.LocalTokens[contentPack] = localTokens = new ModTokenContext(this);
                foreach (IValueProvider valueProvider in this.GetLocalValueProviders(contentPack))
                    localTokens.Add(new GenericToken(valueProvider));
            }

            return localTokens;
        }

        /// <summary>Update the current context.</summary>
        public void UpdateContext()
        {
            foreach (IToken token in this.GlobalContext.Tokens.Values)
            {
                if (token.IsMutable)
                    token.UpdateContext(this);
            }

            foreach (ModTokenContext localContext in this.LocalTokens.Values)
                localContext.UpdateContext(this);
        }

        /****
        ** IContext
        ****/
        /// <summary>Get whether the context contains the given token.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        public bool Contains(TokenName name, bool enforceContext)
        {
            return this.GlobalContext.Contains(name, enforceContext);
        }

        /// <summary>Get the underlying token which handles a key.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        /// <returns>Returns the matching token, or <c>null</c> if none was found.</returns>
        public IToken GetToken(TokenName name, bool enforceContext)
        {
            return this.GlobalContext.GetToken(name, enforceContext);
        }

        /// <summary>Get the underlying tokens.</summary>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        public IEnumerable<IToken> GetTokens(bool enforceContext)
        {
            return this.GlobalContext.GetTokens(enforceContext);
        }

        /// <summary>Get the current values of the given token for comparison.</summary>
        /// <param name="name">The token name.</param>
        /// <param name="enforceContext">Whether to only consider tokens that are available in the context.</param>
        /// <returns>Return the values of the matching token, or an empty list if the token doesn't exist.</returns>
        /// <exception cref="ArgumentNullException">The specified key is null.</exception>
        public IEnumerable<string> GetValues(TokenName name, bool enforceContext)
        {
            return this.GlobalContext.GetValues(name, enforceContext);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the global value providers with which to initialise the token manager.</summary>
        /// <param name="contentHelper">The content helper from which to load data assets.</param>
        /// <param name="installedMods">The installed mod IDs.</param>
        private IEnumerable<IValueProvider> GetGlobalValueProviders(IContentHelper contentHelper, IEnumerable<string> installedMods)
        {
            bool NeedsBasicInfo() => this.IsBasicInfoLoaded;

            // installed mods
            yield return new ImmutableValueProvider(ConditionType.HasMod.ToString(), new InvariantHashSet(installedMods), canHaveMultipleValues: true);

            // language
            yield return new ConditionTypeValueProvider(ConditionType.Language, () => contentHelper.CurrentLocaleConstant.ToString(), allowedValues: Enum.GetNames(typeof(LocalizedContentManager.LanguageCode)).Where(p => p != LocalizedContentManager.LanguageCode.th.ToString()));

            // in-game date
            yield return new ConditionTypeValueProvider(ConditionType.Season, () => SDate.Now().Season, NeedsBasicInfo, allowedValues: new[] { "Spring", "Summer", "Fall", "Winter" });
            yield return new ConditionTypeValueProvider(ConditionType.Day, () => SDate.Now().Day.ToString(CultureInfo.InvariantCulture), NeedsBasicInfo, allowedValues: Enumerable.Range(1, 28).Select(p => p.ToString()));
            yield return new ConditionTypeValueProvider(ConditionType.DayOfWeek, () => SDate.Now().DayOfWeek.ToString(), NeedsBasicInfo, allowedValues: Enum.GetNames(typeof(DayOfWeek)));
            yield return new ConditionTypeValueProvider(ConditionType.Year, () => SDate.Now().Year.ToString(CultureInfo.InvariantCulture), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.DaysPlayed, () => Game1.stats.DaysPlayed.ToString(CultureInfo.InvariantCulture), NeedsBasicInfo);

            // other in-game conditions
            yield return new ConditionTypeValueProvider(ConditionType.DayEvent, () => this.GetDayEvent(contentHelper), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.FarmCave, () => this.GetEnum(Game1.player.caveChoice.Value, FarmCaveType.None).ToString(), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.FarmhouseUpgrade, () => Game1.player.HouseUpgradeLevel.ToString(), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.FarmName, () => Game1.player.farmName.Value, NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.FarmType, () => this.GetEnum(Game1.whichFarm, FarmType.Custom).ToString(), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.HasFlag, () => this.GetMailFlags(), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.HasSeenEvent, () => this.GetEventsSeen(), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.PlayerGender, () => (Game1.player.IsMale ? Gender.Male : Gender.Female).ToString(), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.PreferredPet, () => (Game1.player.catPerson ? PetType.Cat : PetType.Dog).ToString(), NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.PlayerName, () => Game1.player.Name, NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.Spouse, () => Game1.player?.spouse, NeedsBasicInfo);
            yield return new ConditionTypeValueProvider(ConditionType.Weather, () => this.GetCurrentWeather(), NeedsBasicInfo, allowedValues: Enum.GetNames(typeof(Weather)));
            yield return new HasProfessionValueProvider(NeedsBasicInfo);
            yield return new HasWalletItemValueProvider(NeedsBasicInfo);
            yield return new SkillLevelValueProvider(NeedsBasicInfo);
            yield return new VillagerRelationshipValueProvider();
            yield return new VillagerHeartsValueProvider();
        }

        /// <summary>Get the local value providers with which to initialise a local context.</summary>
        /// <param name="contentPack">The content pack for which to get tokens.</param>
        private IEnumerable<IValueProvider> GetLocalValueProviders(IContentPack contentPack)
        {
            yield return new HasFileValueProvider(contentPack.DirectoryPath);
        }

        /// <summary>Get a constant for a given value.</summary>
        /// <typeparam name="TEnum">The constant enum type.</typeparam>
        /// <param name="value">The value to convert.</param>
        /// <param name="defaultValue">The value to use if the value is invalid.</param>
        private TEnum GetEnum<TEnum>(int value, TEnum defaultValue)
        {
            return Enum.IsDefined(typeof(TEnum), value)
                ? (TEnum)(object)value
                : defaultValue;
        }

        /// <summary>Get the current weather from the game state.</summary>
        private string GetCurrentWeather()
        {
            if (Utility.isFestivalDay(Game1.dayOfMonth, Game1.currentSeason) || (SaveGame.loaded?.weddingToday ?? Game1.weddingToday))
                return Weather.Sun.ToString();

            if (Game1.isSnowing)
                return Weather.Snow.ToString();
            if (RainManager.Instance.isRaining)
                return (Game1.isLightning ? Weather.Storm : Weather.Rain).ToString();
            if (SaveGame.loaded?.isDebrisWeather ?? WeatherDebrisManager.Instance.isDebrisWeather)
                return Weather.Wind.ToString();

            return Weather.Sun.ToString();
        }

        /// <summary>Get the event IDs seen by the player.</summary>
        private IEnumerable<string> GetEventsSeen()
        {
            Farmer player = Game1.player;
            if (player == null)
                return new string[0];

            return player.eventsSeen
                .OrderBy(p => p)
                .Select(p => p.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Get the letter IDs and mail flags set for the player.</summary>
        /// <remarks>See game logic in <see cref="Farmer.hasOrWillReceiveMail"/>.</remarks>
        private IEnumerable<string> GetMailFlags()
        {
            Farmer player = Game1.player;
            if (player == null)
                return new string[0];

            return player
                .mailReceived
                .Union(player.mailForTomorrow)
                .Union(player.mailbox);
        }

        /// <summary>Get the name for today's day event (e.g. wedding or festival) from the game data.</summary>
        /// <param name="contentHelper">The content helper from which to load festival data.</param>
        private string GetDayEvent(IContentHelper contentHelper)
        {
            // marriage
            if (SaveGame.loaded?.weddingToday ?? Game1.weddingToday)
                return "wedding";

            // festival
            IDictionary<string, string> festivalDates = contentHelper.Load<Dictionary<string, string>>("Data\\Festivals\\FestivalDates", ContentSource.GameContent);
            if (festivalDates.TryGetValue($"{Game1.currentSeason}{Game1.dayOfMonth}", out string festivalName))
                return festivalName;

            return null;
        }
    }
}
