using Microsoft.Xna.Framework;
using UIInfoSuite.Extensions;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Text;
using StardewValley.Objects;
using StardewModdingAPI;
using StardewValley.Locations;
using StardewValley.Buildings;

namespace UIInfoSuite.UIElements
{
    class ShowCropAndBarrelTime : IDisposable
    {
        private readonly Dictionary<int, string> _indexOfCropNames = new Dictionary<int, string>();
        private StardewValley.Object _currentTile;
        private TerrainFeature _terrain;
        private Building _currentTileBuilding = null;
        private readonly IModHelper _helper;

        public ShowCropAndBarrelTime(IModHelper helper)
        {
            this._helper = helper;
        }

        public void ToggleOption(bool showCropAndBarrelTimes)
        {
            this._helper.Events.Display.RenderingHud -= this.OnRenderingHud;
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;

            if (showCropAndBarrelTimes)
            {
                this._helper.Events.Display.RenderingHud += this.OnRenderingHud;
                this._helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(4))
                return;

            // get tile under cursor
            this._currentTileBuilding = Game1.currentLocation is BuildableGameLocation buildableLocation
                ? buildableLocation.getBuildingAt(Game1.currentCursorTile)
                : null;
            if (Game1.currentLocation != null && Game1.activeClickableMenu == null)
            {
                if (Game1.currentLocation.Objects == null ||
                    !Game1.currentLocation.Objects.TryGetValue(Game1.currentCursorTile, out this._currentTile))
                {
                    this._currentTile = null;
                }

                if (Game1.currentLocation.terrainFeatures == null ||
                    !Game1.currentLocation.terrainFeatures.TryGetValue(Game1.currentCursorTile, out this._terrain))
                {
                    if (this._currentTile is IndoorPot pot &&
                        pot.hoeDirt.Value != null)
                    {
                        this._terrain = pot.hoeDirt.Value;
                    }
                    else
                    {
                        this._terrain = null;
                    }
                }
            }
            else
            {
                this._currentTile = null;
                this._terrain = null;
            }
        }

        public void Dispose()
        {
            this.ToggleOption(false);
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw hover tooltip
            if (this._currentTileBuilding != null)
            {
                if (this._currentTileBuilding is Mill millBuilding)
                {
                    if (millBuilding.input.Value != null)
                    {
                        if (!millBuilding.input.Value.isEmpty())
                        {
                            int wheatCount = 0;
                            int beetCount = 0;

                            foreach (Item item in millBuilding.input.Value.items)
                            {
                                if (item != null &&
                                    !string.IsNullOrEmpty(item.Name))
                                {
                                    switch (item.Name)
                                    {
                                        case "Wheat": wheatCount = item.Stack; break;
                                        case "Beet": beetCount = item.Stack; break;
                                    }
                                }
                            }

                            StringBuilder builder = new StringBuilder();

                            if (wheatCount > 0)
                                builder.Append(wheatCount + " wheat");

                            if (beetCount > 0)
                            {
                                if (wheatCount > 0)
                                    builder.Append(Environment.NewLine);
                                builder.Append(beetCount + " beets");
                            }

                            if (builder.Length > 0)
                            {
                                IClickableMenu.drawHoverText(
                                   Game1.spriteBatch,
                                   builder.ToString(),
                                   Game1.smallFont);
                            }
                        }
                    }
                }
            }
            else if (this._currentTile != null &&
                (!this._currentTile.bigCraftable.Value ||
                this._currentTile.MinutesUntilReady > 0))
            {
                if (this._currentTile.bigCraftable.Value &&
                    this._currentTile.MinutesUntilReady > 0 &&
                    this._currentTile.heldObject.Value != null &&
                    this._currentTile.Name != "Heater")
                {
                    StringBuilder hoverText = new StringBuilder();
                    hoverText.AppendLine(this._currentTile.heldObject.Value.DisplayName);
                    
                    if (this._currentTile is Cask)
                    {
                        Cask currentCask = this._currentTile as Cask;
                        hoverText.Append((int)(currentCask.daysToMature.Value / currentCask.agingRate.Value))
                            .Append(" " + this._helper.SafeGetString(
                            LanguageKeys.DaysToMature));
                    }
                    else
                    {
                        int hours = this._currentTile.MinutesUntilReady / 60;
                        int minutes = this._currentTile.MinutesUntilReady % 60;
                        if (hours > 0)
                            hoverText.Append(hours).Append(" ")
                                .Append(this._helper.SafeGetString(
                                    LanguageKeys.Hours))
                                .Append(", ");
                        hoverText.Append(minutes).Append(" ")
                            .Append(this._helper.SafeGetString(
                                LanguageKeys.Minutes));
                    }
                    IClickableMenu.drawHoverText(
                        Game1.spriteBatch,
                        hoverText.ToString(),
                        Game1.smallFont);
                }
            }
            else if (this._terrain != null)
            {
                if (this._terrain is HoeDirt)
                {
                    HoeDirt hoeDirt = this._terrain as HoeDirt;
                    if (hoeDirt.crop != null &&
                        !hoeDirt.crop.dead.Value)
                    {
                        int num = 0;

                        if (hoeDirt.crop.fullyGrown.Value &&
                            hoeDirt.crop.dayOfCurrentPhase.Value > 0)
                        {
                            num = hoeDirt.crop.dayOfCurrentPhase.Value;
                        }
                        else
                        {
                            for (int i = 0; i < hoeDirt.crop.phaseDays.Count - 1; ++i)
                            {
                                if (hoeDirt.crop.currentPhase.Value == i)
                                    num -= hoeDirt.crop.dayOfCurrentPhase.Value;

                                if (hoeDirt.crop.currentPhase.Value <= i)
                                    num += hoeDirt.crop.phaseDays[i];
                            }
                        }

                        if (hoeDirt.crop.indexOfHarvest.Value > 0)
                        {
                            string hoverText = this._indexOfCropNames.SafeGet(hoeDirt.crop.indexOfHarvest.Value);
                            if (string.IsNullOrEmpty(hoverText))
                            {
                                Debris debris = new Debris();
                                debris.init(hoeDirt.crop.indexOfHarvest.Value, Vector2.Zero, Vector2.Zero);
                                hoverText = new StardewValley.Object(debris.chunkType.Value, 1).DisplayName;
                                this._indexOfCropNames.Add(hoeDirt.crop.indexOfHarvest.Value, hoverText);
                            }

                            StringBuilder finalHoverText = new StringBuilder();
                            finalHoverText.Append(hoverText).Append(": ");
                            if (num > 0)
                            {
                                finalHoverText.Append(num).Append(" ")
                                    .Append(this._helper.SafeGetString(
                                        LanguageKeys.Days));
                            }
                            else
                            {
                                finalHoverText.Append(this._helper.SafeGetString(
                                    LanguageKeys.ReadyToHarvest));
                            }
                            IClickableMenu.drawHoverText(
                                Game1.spriteBatch,
                                finalHoverText.ToString(),
                                Game1.smallFont);
                        }
                    }
                }
                else if (this._terrain is FruitTree)
                {
                    FruitTree tree = this._terrain as FruitTree;
                    Debris debris = new Debris();
                    debris.init(tree.indexOfFruit.Value, Vector2.Zero, Vector2.Zero);
                    string text = new StardewValley.Object(debris.chunkType.Value, 1).DisplayName;
                    if (tree.daysUntilMature.Value > 0)
                    {
                        text += Environment.NewLine + tree.daysUntilMature.Value + " " +
                                this._helper.SafeGetString(
                                    LanguageKeys.DaysToMature);

                    }
                    IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            text,
                            Game1.smallFont);
                }
            }
        }
    }
}
