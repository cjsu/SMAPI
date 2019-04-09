using SMDroid.Options;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoFish
{
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private bool catching = false;

        public override void Entry(IModHelper helper)
        {
            this.Config = this.Helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.UpdateTicked += this.UpdateTick;
        }
        public override List<OptionsElement> GetConfigMenuItems()
        {
            List<OptionsElement> options = new List<OptionsElement>();
            ModOptionsCheckbox _optionsCheckboxAutoHit = new ModOptionsCheckbox("自动起钩", 0x8765, delegate (bool value) {
                this.Config.autoHit = value;
                this.Helper.WriteConfig<ModConfig>(this.Config);
            }, -1, -1);
            _optionsCheckboxAutoHit.isChecked = this.Config.autoHit;
            options.Add(_optionsCheckboxAutoHit);
            ModOptionsCheckbox _optionsCheckboxMaxCastPower = new ModOptionsCheckbox("最大抛竿", 0x8765, delegate (bool value) {
                this.Config.maxCastPower = value;
                this.Helper.WriteConfig<ModConfig>(this.Config);
            }, -1, -1);
            _optionsCheckboxMaxCastPower.isChecked = this.Config.maxCastPower;
            options.Add(_optionsCheckboxMaxCastPower);
            ModOptionsCheckbox _optionsCheckboxFastBite = new ModOptionsCheckbox("快速咬钩", 0x8765, delegate (bool value) {
                this.Config.fastBite = value;
                this.Helper.WriteConfig<ModConfig>(this.Config);
            }, -1, -1);
            _optionsCheckboxFastBite.isChecked = this.Config.fastBite;
            options.Add(_optionsCheckboxFastBite);
            ModOptionsCheckbox _optionsCheckboxCatchTreasure = new ModOptionsCheckbox("钓取宝箱", 0x8765, delegate (bool value) {
                this.Config.catchTreasure = value;
                this.Helper.WriteConfig<ModConfig>(this.Config);
            }, -1, -1);
            _optionsCheckboxCatchTreasure.isChecked = this.Config.catchTreasure;
            options.Add(_optionsCheckboxCatchTreasure);
            return options;
        }


        private void UpdateTick(object sender, EventArgs e)
        {
            if (Game1.player == null)
                return;

            if (Game1.player.CurrentTool is FishingRod)
            {
                FishingRod currentTool = Game1.player.CurrentTool as FishingRod;
                if (this.Config.fastBite && currentTool.timeUntilFishingBite > 0)
                    currentTool.timeUntilFishingBite /= 2; // 快速咬钩

                if (this.Config.autoHit && currentTool.isNibbling && !currentTool.isReeling && !currentTool.hit && !currentTool.pullingOutOfWater && !currentTool.fishCaught)
                    currentTool.DoFunction(Game1.player.currentLocation, 1, 1, 1, Game1.player); // 自动咬钩

                if (this.Config.maxCastPower)
                    currentTool.castingPower = 1;
            }

            if (Game1.activeClickableMenu is BobberBar) // 自动小游戏
            {
                BobberBar bar = Game1.activeClickableMenu as BobberBar;
                float barPos = this.Helper.Reflection.GetField<float>(bar, "bobberBarPos").GetValue();
                float barHeight = this.Helper.Reflection.GetField<int>(bar, "bobberBarHeight").GetValue();
                float fishPos = this.Helper.Reflection.GetField<float>(bar, "bobberPosition").GetValue();
                float treasurePos = this.Helper.Reflection.GetField<float>(bar, "treasurePosition").GetValue();
                float distanceFromCatching = this.Helper.Reflection.GetField<float>(bar, "distanceFromCatching").GetValue();

                bool treasureCaught = this.Helper.Reflection.GetField<bool>(bar, "treasureCaught").GetValue();
                bool hasTreasure = this.Helper.Reflection.GetField<bool>(bar, "treasure").GetValue();
                float treasureScale = this.Helper.Reflection.GetField<float>(bar, "treasureScale").GetValue();
                float bobberBarSpeed = this.Helper.Reflection.GetField<float>(bar, "bobberBarSpeed").GetValue();
                float barPosMax = 568 - barHeight;

                float min = barPos + barHeight / 4,
                    max = barPos + barHeight / 1.5f;

                if (this.Config.catchTreasure && hasTreasure && !treasureCaught && (distanceFromCatching > 0.75 || this.catching))
                {
                    this.catching = true;
                    fishPos = treasurePos;
                }
                if (this.catching && distanceFromCatching < 0.15)
                {
                    this.catching = false;
                    fishPos = this.Helper.Reflection.GetField<float>(bar, "bobberPosition").GetValue();
                }

                if (fishPos < min)
                {
                    bobberBarSpeed -= 0.35f + (min - fishPos) / 20;
                    this.Helper.Reflection.GetField<float>(bar, "bobberBarSpeed").SetValue(bobberBarSpeed);
                } else if (fishPos > max)
                {
                    bobberBarSpeed += 0.35f + (fishPos - max) / 20;
                    this.Helper.Reflection.GetField<float>(bar, "bobberBarSpeed").SetValue(bobberBarSpeed);
                } else
                {
                    float target = 0.1f;
                    if (bobberBarSpeed > target)
                    {
                        bobberBarSpeed -= 0.1f + (bobberBarSpeed - target) / 25;
                        if (barPos + bobberBarSpeed > barPosMax)
                            bobberBarSpeed /= 2; // 减小触底反弹
                        if (bobberBarSpeed < target)
                            bobberBarSpeed = target;
                    } else
                    {
                        bobberBarSpeed += 0.1f + (target - bobberBarSpeed) / 25;
                        if (barPos + bobberBarSpeed < 0)
                            bobberBarSpeed /= 2; // 减小触顶反弹
                        if (bobberBarSpeed > target)
                            bobberBarSpeed = target;
                    }
                    this.Helper.Reflection.GetField<float>(bar, "bobberBarSpeed").SetValue(bobberBarSpeed);
                }
            }
            else
            {
                this.catching = false;
            }
        }
    }
}
