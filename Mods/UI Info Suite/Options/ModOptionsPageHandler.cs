using UIInfoSuite.UIElements;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.Options
{
    class ModOptionsPageHandler : IDisposable
    {
        private List<ModOptionsElement> _optionsElements = new List<ModOptionsElement>();
        private readonly List<IDisposable> _elementsToDispose;
        private readonly IDictionary<string, string> _options;
        private ModOptionsPageButton _modOptionsPageButton;
        private ModOptionsPage _modOptionsPage;
        private readonly IModHelper _helper;

        private int _modOptionsTabPageNumber;

        private readonly LuckOfDay _luckOfDay;
        private readonly ShowBirthdayIcon _showBirthdayIcon;
        private readonly ShowAccurateHearts _showAccurateHearts;
        private readonly LocationOfTownsfolk _locationOfTownsfolk;
        private readonly ShowWhenAnimalNeedsPet _showWhenAnimalNeedsPet;
        private readonly ShowCalendarAndBillboardOnGameMenuButton _showCalendarAndBillboardOnGameMenuButton;
        private readonly ShowCropAndBarrelTime _showCropAndBarrelTime;
        private readonly ShowItemEffectRanges _showScarecrowAndSprinklerRange;
        //private readonly ExperienceBar _experienceBar;
        private readonly ShowItemHoverInformation _showItemHoverInformation;
        private readonly ShowTravelingMerchant _showTravelingMerchant;
        private readonly ShopHarvestPrices _shopHarvestPrices;
        private readonly ShowQueenOfSauceIcon _showQueenOfSauceIcon;
        private readonly ShowToolUpgradeStatus _showToolUpgradeStatus;

        public ModOptionsPageHandler(IModHelper helper, IDictionary<string, string> options)
        {
            this._options = options;
            helper.Events.Display.MenuChanged += this.ToggleModOptions;
            this._helper = helper;
            ModConfig modConfig = this._helper.ReadConfig<ModConfig>();
            this._luckOfDay = new LuckOfDay(helper);
            this._showBirthdayIcon = new ShowBirthdayIcon(helper.Events);
            this._showAccurateHearts = new ShowAccurateHearts(helper.Events);
            this._locationOfTownsfolk = new LocationOfTownsfolk(helper, this._options);
            this._showWhenAnimalNeedsPet = new ShowWhenAnimalNeedsPet(helper);
            this._showCalendarAndBillboardOnGameMenuButton = new ShowCalendarAndBillboardOnGameMenuButton(helper);
            this._showScarecrowAndSprinklerRange = new ShowItemEffectRanges(modConfig, helper.Events);
            //this._experienceBar = new ExperienceBar(helper);
            this._showItemHoverInformation = new ShowItemHoverInformation(helper.Events);
            this._shopHarvestPrices = new ShopHarvestPrices(helper);
            this._showQueenOfSauceIcon = new ShowQueenOfSauceIcon(helper);
            this._showTravelingMerchant = new ShowTravelingMerchant(helper);
            this._showCropAndBarrelTime = new ShowCropAndBarrelTime(helper);
            this._showToolUpgradeStatus = new ShowToolUpgradeStatus(helper);

            this._elementsToDispose = new List<IDisposable>()
            {
                this._luckOfDay,
                this._showBirthdayIcon,
                this._showAccurateHearts,
                this._locationOfTownsfolk,
                this._showWhenAnimalNeedsPet,
                this._showCalendarAndBillboardOnGameMenuButton,
                this._showCropAndBarrelTime,
                //this._experienceBar,
                this._showItemHoverInformation,
                this._showTravelingMerchant,
                this._shopHarvestPrices,
                this._showQueenOfSauceIcon,
                this._showToolUpgradeStatus
            };

            int whichOption = 1;
            Version thisVersion = Assembly.GetAssembly(this.GetType()).GetName().Version;
            this._optionsElements.Add(new ModOptionsElement("UI Info Suite v" +
                thisVersion.Major + "." + thisVersion.Minor + "." + thisVersion.Build));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowLuckIcon), whichOption++, this._luckOfDay.Toggle, this._options, OptionKeys.ShowLuckIcon));
            //this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowLevelUpAnimation), whichOption++, this._experienceBar.ToggleLevelUpAnimation, this._options, OptionKeys.ShowLevelUpAnimation));
            //this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowExperienceBar), whichOption++, this._experienceBar.ToggleShowExperienceBar, this._options, OptionKeys.ShowExperienceBar));
            //this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.AllowExperienceBarToFadeOut), whichOption++, this._experienceBar.ToggleExperienceBarFade, this._options, OptionKeys.AllowExperienceBarToFadeOut));
            //this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowExperienceGain), whichOption++, this._experienceBar.ToggleShowExperienceGain, this._options, OptionKeys.ShowExperienceGain));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowLocationOfTownsPeople), whichOption++, this._locationOfTownsfolk.ToggleShowNPCLocationsOnMap, this._options, OptionKeys.ShowLocationOfTownsPeople));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowBirthdayIcon), whichOption++, this._showBirthdayIcon.ToggleOption, this._options, OptionKeys.ShowBirthdayIcon));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowHeartFills), whichOption++, this._showAccurateHearts.ToggleOption, this._options, OptionKeys.ShowHeartFills));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowAnimalsNeedPets), whichOption++, this._showWhenAnimalNeedsPet.ToggleOption, this._options, OptionKeys.ShowAnimalsNeedPets));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.DisplayCalendarAndBillboard), whichOption++, this._showCalendarAndBillboardOnGameMenuButton.ToggleOption, this._options, OptionKeys.DisplayCalendarAndBillboard));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowCropAndBarrelTooltip), whichOption++, this._showCropAndBarrelTime.ToggleOption, this._options, OptionKeys.ShowCropAndBarrelTooltip));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowItemEffectRanges), whichOption++, this._showScarecrowAndSprinklerRange.ToggleOption, this._options, OptionKeys.ShowItemEffectRanges));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowExtraItemInformation), whichOption++, this._showItemHoverInformation.ToggleOption, this._options, OptionKeys.ShowExtraItemInformation));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowTravelingMerchant), whichOption++, this._showTravelingMerchant.ToggleOption, this._options, OptionKeys.ShowTravelingMerchant));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowHarvestPricesInShop), whichOption++, this._shopHarvestPrices.ToggleOption, this._options, OptionKeys.ShowHarvestPricesInShop));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowWhenNewRecipesAreAvailable), whichOption++, this._showQueenOfSauceIcon.ToggleOption, this._options, OptionKeys.ShowWhenNewRecipesAreAvailable));
            this._optionsElements.Add(new ModOptionsCheckbox(this._helper.SafeGetString(OptionKeys.ShowToolUpgradeStatus), whichOption++, this._showToolUpgradeStatus.ToggleOption, this._options, OptionKeys.ShowToolUpgradeStatus));

        }


        public void Dispose()
        {
            foreach (IDisposable item in this._elementsToDispose)
                item.Dispose();
        }

        private void OnButtonLeftClicked(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu)
            {
                this.SetActiveClickableMenuToModOptionsPage();
                Game1.playSound("smallSelect");
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ToggleModOptions(object sender, MenuChangedEventArgs e)
        {
            // remove from old menu
            if (e.OldMenu != null)
            {
                this._helper.Events.Display.RenderedActiveMenu -= this.DrawButton;
                if (this._modOptionsPageButton != null)
                    this._modOptionsPageButton.OnLeftClicked -= this.OnButtonLeftClicked;

                if (e.OldMenu is GameMenu oldMenu)
                {
                    List<IClickableMenu> tabPages = oldMenu.pages;
                    tabPages.Remove(this._modOptionsPage);
                }
            }

            // add to new menu
            if (e.NewMenu is GameMenu newMenu)
            {
                if (this._modOptionsPageButton == null)
                {
                    this._modOptionsPage = new ModOptionsPage(this._optionsElements, this._helper.Events);
                    this._modOptionsPageButton = new ModOptionsPageButton(this._helper.Events);
                }

                this._helper.Events.Display.RenderedActiveMenu += this.DrawButton;
                this._modOptionsPageButton.OnLeftClicked += this.OnButtonLeftClicked;
                List<IClickableMenu> tabPages = newMenu.pages;

                this._modOptionsTabPageNumber = tabPages.Count;
                tabPages.Add(this._modOptionsPage);
            }
        }

        private void SetActiveClickableMenuToModOptionsPage()
        {
            if (Game1.activeClickableMenu is GameMenu menu)
                menu.currentTab = this._modOptionsTabPageNumber;
        }

        private void DrawButton(object sender, EventArgs e)
        {
            if (Game1.activeClickableMenu is GameMenu &&
                (Game1.activeClickableMenu as GameMenu).currentTab != 3) //don't render when the map is showing
            {
                if ((Game1.activeClickableMenu as GameMenu).currentTab == this._modOptionsTabPageNumber)
                {
                    this._modOptionsPageButton.yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen + 24;
                }
                else
                {
                    this._modOptionsPageButton.yPositionOnScreen = Game1.activeClickableMenu.yPositionOnScreen + 16;
                }
                this._modOptionsPageButton.draw(Game1.spriteBatch);

                //Might need to render hover text here
            }
        }
    }
}
