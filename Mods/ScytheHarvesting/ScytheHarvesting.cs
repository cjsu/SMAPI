using Microsoft.Xna.Framework;
using SMDroid.Options;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScytheHarvesting
{

    public class ScytheHarvesting : StardewModdingAPI.Mod
    {
        public static ModConfig config;
        private static int TickCount { get; set; } = 0;
        private void CountCurrentHarvestableCrop()
        {
            IEnumerable<KeyValuePair<Vector2, TerrainFeature>> enumerable = Game1.currentLocation.terrainFeatures.Pairs;
            if (enumerable != null)
            {
                IEnumerable<TerrainFeature> enumerable2 = from x in enumerable
                                                          select x.Value into x
                                                          where x is HoeDirt
                                                          select x;
                this.CountOfCropsReadyForHarvest = (from x in enumerable2
                                                    select (HoeDirt)x into x
                                                    where x.crop != null
                                                    where x.readyForHarvest()
                                                    select x).Count<HoeDirt>();
            }
        }

        private void CreateSunflowerSeeds(int index, int x, int y, int quantity)
        {
            Game1.createMultipleObjectDebris(index, x, y, quantity);
        }

        public override void Entry(IModHelper helper)
        {
            config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.UpdateTicked += this.Events_TickUpdate;
            helper.Events.Input.ButtonPressed += this.Events_MouseActionOnHoeDirt;
        }
        public override List<OptionsElement> GetConfigMenuItems()
        {
            List<OptionsElement> options = new List<OptionsElement>();
            ModOptionsCheckbox _optionsCheckboxEnabled = new ModOptionsCheckbox("镰刀收割", 0x8765, this.Toogle, -1, -1);
            _optionsCheckboxEnabled.isChecked = config.EnableMod;
            options.Add(_optionsCheckboxEnabled);
            ModOptionsCheckbox _optionsCheckboxEnableFlowers = new ModOptionsCheckbox("收割花朵", 0x8765, delegate (bool value) {
                if (config.EnableFlowers != value)
                {
                    config.EnableFlowers = value;
                    base.Helper.WriteConfig<ModConfig>(config);
                }
            }, -1, -1);
            _optionsCheckboxEnableFlowers.isChecked = config.EnableFlowers;
            options.Add(_optionsCheckboxEnableFlowers);
            return options;
        }
        private void Events_MouseActionOnHoeDirt(object sender, EventArgs e)
        {

            if ((config.EnableMod && Context.IsWorldReady) && (Game1.currentLocation.IsFarm || (Game1.currentLocation.Name == "Greenhouse")))
            {
                this.SetTargetAsXP();
            }
        }

        private void Events_TickUpdate(object sender, EventArgs e)
        {
            if ((Context.IsWorldReady && (Game1.currentLocation != null)) && ((Context.IsWorldReady && config.EnableMod) && (Game1.currentLocation.IsFarm || Game1.currentLocation.Name.Equals("Greenhouse"))))
            {
                IEnumerable<KeyValuePair<Vector2, TerrainFeature>> enumerable = Game1.currentLocation.terrainFeatures.Pairs;
                if (enumerable != null)
                {
                    List<HoeDirt> list = new List<HoeDirt>();
                    this.FarmHasSunflowers = false;
                    foreach (KeyValuePair<Vector2, TerrainFeature> pair in enumerable)
                    {
                        if (pair.Value is HoeDirt)
                        {
                            list.Add((HoeDirt)pair.Value);
                        }
                    }
                    foreach (HoeDirt dirt in list)
                    {
                        if (dirt.crop != null)
                        {
                            if (dirt.crop.indexOfHarvest.Value != 0x1a5)
                            {
                                if (config.EnableFlowers)
                                {
                                    dirt.crop.harvestMethod.Value = 1;
                                }
                                else if ((((dirt.crop.indexOfHarvest.Value != 0x24f) && (dirt.crop.indexOfHarvest.Value != 0x251)) && ((dirt.crop.indexOfHarvest.Value != 0x253) && (dirt.crop.indexOfHarvest.Value != 0x255))) && (dirt.crop.indexOfHarvest.Value != 0x178))
                                {
                                    dirt.crop.harvestMethod.Value = 1;
                                }
                            }
                            else if (config.EnableSunflowers)
                            {
                                this.FarmHasSunflowers = true;
                                dirt.crop.harvestMethod.Value = 1;
                            }
                        }
                    }
                }
                if (enumerable != null)
                {
                    int num = (from x in from x in enumerable select x.Value
                               where x is HoeDirt
                               select (HoeDirt)x into x
                               where x.crop != null
                               where x.readyForHarvest()
                               select x).Count<HoeDirt>();
                    int num2 = Math.Max(0, this.CountOfCropsReadyForHarvest - num);
                    if ((num2 > 0) && (this.HoveredCrop != 0))
                    {
                        string str = Game1.objectInformation[this.HoveredCrop];
                        char[] separator = new char[] { '/' };
                        int num3 = Convert.ToInt32(str.Split(separator)[1]);
                        float num4 = (float)(16.0 * Math.Log((0.018 * num3) + 1.0, 2.71828182845905));
                        float num5 = num4 * num2;
                        if (num5 <= 0f)
                        {
                            num5 = 15 * num2;
                        }
                        Game1.player.gainExperience(0, (int)Math.Round((double)num5));
                        if ((this.HoveredCrop == 0x1a5) && this.FarmHasSunflowers)
                        {
                            int num6 = new Random().Next(1, 10);
                            if ((num6 >= 1) && (num6 <= 3))
                            {
                                this.CreateSunflowerSeeds(0x1af, this.HoveredX, this.HoveredY, 1);
                            }
                            else if ((num6 >= 4) && (num6 <= 6))
                            {
                                this.CreateSunflowerSeeds(0x1af, this.HoveredX, this.HoveredY, 2);
                            }
                            else if ((num6 >= 7) || (num6 <= 8))
                            {
                                this.CreateSunflowerSeeds(0x1af, this.HoveredX, this.HoveredY, 3);
                            }
                        }
                    }
                }
                this.CountCurrentHarvestableCrop();
            }
        }

        private IDictionary<int, int> GetHarvestMethod()
        {
            IDictionary<int, int> dictionary = new Dictionary<int, int>();
            foreach (KeyValuePair<int, string> pair in Game1.content.Load<Dictionary<int, string>>(@"Data\Crops"))
            {
                char[] separator = new char[] { '/' };
                string[] strArray = pair.Value.Split(separator);
                int index = 3;
                int key = Convert.ToInt32(strArray[index]);
                int num3 = 5;
                int num4 = Convert.ToInt32(strArray[num3]);
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, num4);
                }
            }
            return dictionary;
        }

        private void SetTargetAsXP()
        {
            Item currentItem = Game1.player.CurrentItem;
            if ((currentItem is MeleeWeapon) && currentItem.Name.Equals("Scythe"))
            {
                IEnumerable<KeyValuePair<Vector2, TerrainFeature>> source = Game1.currentLocation.terrainFeatures.Pairs;
                Vector2 toolLocation = Game1.player.GetToolLocation(false);
                if ((Game1.currentLocation.IsFarm || Game1.currentLocation.Name.Equals("Greenhouse")) && (source != null))
                {
                    int tx = ((int) toolLocation.X) / 0x40;
                    int ty = ((int) toolLocation.Y) / 0x40;
                    TerrainFeature feature = source.FirstOrDefault<KeyValuePair<Vector2, TerrainFeature>>(x => ((x.Key.X == tx) && (x.Key.Y == ty))).Value;
                    if (feature is HoeDirt)
                    {
                        HoeDirt dirt = (HoeDirt) feature;
                        if (dirt.crop != null)
                        {
                            if ((dirt.crop.currentPhase.Value >= (dirt.crop.phaseDays.Count<int>() - 1)) && (!dirt.crop.fullyGrown.Value || (dirt.crop.dayOfCurrentPhase.Value <= 0)))
                            {
                                this.HoveredCrop = dirt.crop.indexOfHarvest.Value;
                                this.HoveredX = tx;
                                this.HoveredY = ty;
                            }
                        }
                        else
                        {
                            this.HoveredCrop = 0;
                            this.HoveredX = 0;
                            this.HoveredY = 0;
                        }
                    }
                }
            }
        }

        private void Toogle(bool enabled)
        {
            if (config.EnableMod == enabled)
            {
                return;
            }
            if (enabled)
            {
                config.EnableMod = true;
                base.Helper.WriteConfig<ModConfig>(config);
            }
            else
            {
                config.EnableMod = false;
                base.Helper.WriteConfig<ModConfig>(config);
                IDictionary<int, int> harvestMethod = this.GetHarvestMethod();
                foreach (GameLocation location in Game1.locations)
                {
                    if (location.IsFarm || location.Name.Equals("Greenhouse"))
                    {
                        foreach (KeyValuePair<Vector2, TerrainFeature> pair in location.terrainFeatures.Pairs)
                        {
                            HoeDirt dirt;
                            int num = 0;
                            if ((((dirt = pair.Value as HoeDirt) != null) && (dirt.crop != null)) && harvestMethod.TryGetValue(dirt.crop.indexOfHarvest.Value, out num))
                            {
                                dirt.crop.harvestMethod.Value = num;
                            }
                        }
                    }
                }
            }
        }

        private int CountOfCropsReadyForHarvest { get; set; }

        private int HoveredCrop { get; set; }

        private int HoveredX { get; set; }

        private int HoveredY { get; set; }

        private bool FarmHasSunflowers { get; set; }

    }
}

