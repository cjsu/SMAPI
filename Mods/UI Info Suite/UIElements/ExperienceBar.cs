using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ExperienceBar : IDisposable
    {

        public interface LevelExtenderEvents
        {
            event EventHandler OnXPChanged;
        }

        private const int MaxBarWidth = 175;

        private int[] _currentExperience = new int[5];
        private int[] _currentLevelExtenderExperience = new int[5];
        private readonly List<ExperiencePointDisplay> _experiencePointDisplays = new List<ExperiencePointDisplay>();
        private readonly TimeSpan _levelUpPauseTime = TimeSpan.FromSeconds(2);
        private readonly Color _iconColor = Color.White;
        private Color _experienceFillColor = Color.Blue;
        private Rectangle _experienceIconPosition = new Rectangle(10, 428, 10, 10);
        private Item _previousItem = null;
        private bool _experienceBarShouldBeVisible = false;
        private bool _shouldDrawLevelUp = false;
        private System.Timers.Timer _timeToDisappear = new System.Timers.Timer();
        private readonly TimeSpan _timeBeforeExperienceBarFades = TimeSpan.FromSeconds(8);
        //private SoundEffectInstance _soundEffect;
        private Rectangle _levelUpIconRectangle = new Rectangle(120, 428, 10, 10);
        private bool _allowExperienceBarToFadeOut = true;
        private bool _showExperienceGain = true;
        private bool _showLevelUpAnimation = true;
        private bool _showExperienceBar = true;
        private readonly IModHelper _helper;
        private SoundPlayer _player;

        private LevelExtenderInterface _levelExtenderAPI;

        private int _currentSkillLevel = 0;
        private int _experienceRequiredToLevel = -1;
        private int _experienceFromPreviousLevels = -1;
        private int _experienceEarnedThisLevel = -1;

        public ExperienceBar(IModHelper helper)
        {
            this._helper = helper;
            string path = string.Empty;
            try
            {
                path = Path.Combine(this._helper.DirectoryPath, "LevelUp.wav");
                this._player = new SoundPlayer(path);
                //path = path.Replace(Environment.CurrentDirectory, "");
                //path = path.TrimStart(Path.DirectorySeparatorChar);
                //_soundEffect = SoundEffect.FromStream(TitleContainer.OpenStream(path)).CreateInstance();
                //_soundEffect.Volume = 1f;
            }
            catch (Exception ex)
            {
                ModEntry.MonitorObject.Log("Error loading sound file from " + path + ": " + ex.Message + Environment.NewLine + ex.StackTrace, LogLevel.Error);
            }
            this._timeToDisappear.Elapsed += this.StopTimerAndFadeBarOut;
            helper.Events.Display.RenderingHud += this.OnRenderingHud;
            helper.Events.Player.Warped += this.OnWarped_RemoveAllExperiencePointDisplays;

            object something = this._helper.ModRegistry.GetApi("DevinLematty.LevelExtender");
            try
            {
                this._levelExtenderAPI = this._helper.ModRegistry.GetApi<LevelExtenderInterface>("DevinLematty.LevelExtender");
            }
            catch (Exception ex)
            {
                int j = 4;
            }
            int f = 3;

            //if (something != null)
            //{
            //    try
            //    {
            //        var methods = something.GetType().GetMethods();
            //        var currentXPMethod = something.GetType().GetMethod("currentXP");

            //        foreach (var method in methods)
            //        {

            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        int f = 3;
            //    }
            //}
        }

        private void LoadModApis(object sender, EventArgs e)
        {

        }

        public void Dispose()
        {
            this._helper.Events.Player.LevelChanged -= this.OnLevelChanged;
            this._helper.Events.Display.RenderingHud -= this.OnRenderingHud;
            this._helper.Events.Player.Warped -= this.OnWarped_RemoveAllExperiencePointDisplays;
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            this._timeToDisappear.Elapsed -= this.StopTimerAndFadeBarOut;
            this._timeToDisappear.Stop();
            this._timeToDisappear.Dispose();
            this._timeToDisappear = null;
        }

        public void ToggleLevelUpAnimation(bool showLevelUpAnimation)
        {
            this._showLevelUpAnimation = showLevelUpAnimation;
            this._helper.Events.Player.LevelChanged -= this.OnLevelChanged;

            if (this._showLevelUpAnimation)
            {
                this._helper.Events.Player.LevelChanged += this.OnLevelChanged;
            }
        }

        public void ToggleExperienceBarFade(bool allowExperienceBarToFadeOut)
        {
            this._allowExperienceBarToFadeOut = allowExperienceBarToFadeOut;
        }

        public void ToggleShowExperienceGain(bool showExperienceGain)
        {
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            for (int i = 0; i < this._currentExperience.Length; ++i)
                this._currentExperience[i] = Game1.player.experiencePoints[i];
            this._showExperienceGain = showExperienceGain;

            if (this._levelExtenderAPI != null)
            {
                for (int i = 0; i < this._currentLevelExtenderExperience.Length; ++i)
                    this._currentLevelExtenderExperience[i] = this._levelExtenderAPI.currentXP()[i];
            }

            if (showExperienceGain)
            {
                this._helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            }
        }


        public void ToggleShowExperienceBar(bool showExperienceBar)
        {
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            //GraphicsEvents.OnPreRenderHudEvent -= OnPreRenderHudEvent;
            //PlayerEvents.Warped -= RemoveAllExperiencePointDisplays;
            this._showExperienceBar = showExperienceBar;
            if (showExperienceBar)
            {
                //GraphicsEvents.OnPreRenderHudEvent += OnPreRenderHudEvent;
                //PlayerEvents.Warped += RemoveAllExperiencePointDisplays;
                this._helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked_DetermineIfExperienceHasBeenGained;
            }
        }

        /// <summary>Raised after a player skill level changes. This happens as soon as they level up, not when the game notifies the player after their character goes to bed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnLevelChanged(object sender, LevelChangedEventArgs e)
        {
            if (this._showLevelUpAnimation && e.IsLocalPlayer)
            {
                switch (e.Skill)
                {
                    case SkillType.Combat: this._levelUpIconRectangle.X = 120; break;
                    case SkillType.Farming: this._levelUpIconRectangle.X = 10; break;
                    case SkillType.Fishing: this._levelUpIconRectangle.X = 20; break;
                    case SkillType.Foraging: this._levelUpIconRectangle.X = 60; break;
                    case SkillType.Mining: this._levelUpIconRectangle.X = 30; break;
                }
                this._shouldDrawLevelUp = true;
                this.ShowExperienceBar();

                float previousAmbientVolume = Game1.options.ambientVolumeLevel;
                float previousMusicVolume = Game1.options.musicVolumeLevel;

                //if (_soundEffect != null)
                //    _soundEffect.Volume = previousMusicVolume <= 0.01f ? 0 : Math.Min(1, previousMusicVolume + 0.3f);

                //Task.Factory.StartNew(() =>
                //{
                //    Thread.Sleep(100);
                //    Game1.musicCategory.SetVolume((float)Math.Max(0, Game1.options.musicVolumeLevel - 0.3));
                //    Game1.ambientCategory.SetVolume((float)Math.Max(0, Game1.options.ambientVolumeLevel - 0.3));
                //    if (_soundEffect != null)
                //        _soundEffect.Play();
                //});

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(100);
                    this._player.Play();
                });

                Task.Factory.StartNew(() =>
                {
                    Thread.Sleep(this._levelUpPauseTime);
                    this._shouldDrawLevelUp = false;
                    //Game1.musicCategory.SetVolume(previousMusicVolume);
                    //Game1.ambientCategory.SetVolume(previousAmbientVolume);
                });
            }
        }

        private void StopTimerAndFadeBarOut(object sender, ElapsedEventArgs e)
        {
            this._timeToDisappear?.Stop();
            this._experienceBarShouldBeVisible = false;
        }

        /// <summary>Raised after a player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped_RemoveAllExperiencePointDisplays(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
                this._experiencePointDisplays.Clear();
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked_DetermineIfExperienceHasBeenGained(object sender, UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(15)) // quarter second
                return;

            Item currentItem = Game1.player.CurrentItem;

            int currentLevelIndex = -1;

            int[] levelExtenderExperience = null;
            if (this._levelExtenderAPI != null)
                levelExtenderExperience = this._levelExtenderAPI.currentXP();

            for (int i = 0; i < this._currentExperience.Length; ++i)
            {
                if (this._currentExperience[i] != Game1.player.experiencePoints[i] ||
                    (this._levelExtenderAPI != null &&
                    this._currentLevelExtenderExperience[i] != levelExtenderExperience[i]))
                {
                    currentLevelIndex = i;
                    break;
                }
            }

            if (currentLevelIndex > -1)
            {
                switch (currentLevelIndex)
                {
                    case 0:
                        {
                            this._experienceFillColor = new Color(255, 251, 35, 0.38f);
                            this._experienceIconPosition.X = 10;
                            this._currentSkillLevel = Game1.player.farmingLevel.Value;
                            break;
                        }

                    case 1:
                        {
                            this._experienceFillColor = new Color(17, 84, 252, 0.63f);
                            this._experienceIconPosition.X = 20;
                            this._currentSkillLevel = Game1.player.fishingLevel.Value;
                            break;
                        }

                    case 2:
                        {
                            this._experienceFillColor = new Color(0, 234, 0, 0.63f);
                            this._experienceIconPosition.X = 60;
                            this._currentSkillLevel = Game1.player.foragingLevel.Value;
                            break;
                        }

                    case 3:
                        {
                            this._experienceFillColor = new Color(145, 104, 63, 0.63f);
                            this._experienceIconPosition.X = 30;
                            this._currentSkillLevel = Game1.player.miningLevel.Value;
                            break;
                        }

                    case 4:
                        {
                            this._experienceFillColor = new Color(204, 0, 3, 0.63f);
                            this._experienceIconPosition.X = 120;
                            this._currentSkillLevel = Game1.player.combatLevel.Value;
                            break;
                        }
                }

                this._experienceRequiredToLevel = this.GetExperienceRequiredToLevel(this._currentSkillLevel);
                this._experienceFromPreviousLevels = this.GetExperienceRequiredToLevel(this._currentSkillLevel - 1);
                this._experienceEarnedThisLevel = Game1.player.experiencePoints[currentLevelIndex] - this._experienceFromPreviousLevels;
                int experiencePreviouslyEarnedThisLevel = this._currentExperience[currentLevelIndex] - this._experienceFromPreviousLevels;

                if (this._experienceRequiredToLevel <= 0 &&
                    this._levelExtenderAPI != null)
                {
                    this._experienceEarnedThisLevel = this._levelExtenderAPI.currentXP()[currentLevelIndex];
                    this._experienceFromPreviousLevels = this._currentExperience[currentLevelIndex] - this._experienceEarnedThisLevel;
                    this._experienceRequiredToLevel = this._levelExtenderAPI.requiredXP()[currentLevelIndex] + this._experienceFromPreviousLevels;
                }

                this.ShowExperienceBar();
                if (this._showExperienceGain &&
                    this._experienceRequiredToLevel > 0)
                {
                    int currentExperienceToUse = Game1.player.experiencePoints[currentLevelIndex];
                    int previousExperienceToUse = this._currentExperience[currentLevelIndex];
                    if (this._levelExtenderAPI != null &&
                        this._currentSkillLevel > 9)
                    {
                        currentExperienceToUse = this._levelExtenderAPI.currentXP()[currentLevelIndex];
                        previousExperienceToUse = this._currentLevelExtenderExperience[currentLevelIndex];
                    }

                    int experienceGain = currentExperienceToUse - previousExperienceToUse;

                    if (experienceGain > 0)
                    {
                        this._experiencePointDisplays.Add(
                            new ExperiencePointDisplay(
                                experienceGain,
                                Game1.player.getLocalPosition(Game1.viewport)));
                    }
                }

                this._currentExperience[currentLevelIndex] = Game1.player.experiencePoints[currentLevelIndex];

                if (this._levelExtenderAPI != null)
                    this._currentLevelExtenderExperience[currentLevelIndex] = this._levelExtenderAPI.currentXP()[currentLevelIndex];

            }
            else if (this._previousItem != currentItem)
            {
                if (currentItem is FishingRod)
                {
                    this._experienceFillColor = new Color(17, 84, 252, 0.63f);
                    currentLevelIndex = 1;
                    this._experienceIconPosition.X = 20;
                    this._currentSkillLevel = Game1.player.fishingLevel.Value;
                }
                else if (currentItem is Pickaxe)
                {
                    this._experienceFillColor = new Color(145, 104, 63, 0.63f);
                    currentLevelIndex = 3;
                    this._experienceIconPosition.X = 30;
                    this._currentSkillLevel = Game1.player.miningLevel.Value;
                }
                else if (currentItem is MeleeWeapon &&
                    currentItem.Name != "Scythe")
                {
                    this._experienceFillColor = new Color(204, 0, 3, 0.63f);
                    currentLevelIndex = 4;
                    this._experienceIconPosition.X = 120;
                    this._currentSkillLevel = Game1.player.combatLevel.Value;
                }
                else if (Game1.currentLocation is Farm &&
                    !(currentItem is Axe))
                {
                    this._experienceFillColor = new Color(255, 251, 35, 0.38f);
                    currentLevelIndex = 0;
                    this._experienceIconPosition.X = 10;
                    this._currentSkillLevel = Game1.player.farmingLevel.Value;
                }
                else
                {
                    this._experienceFillColor = new Color(0, 234, 0, 0.63f);
                    currentLevelIndex = 2;
                    this._experienceIconPosition.X = 60;
                    this._currentSkillLevel = Game1.player.foragingLevel.Value;
                }

                this._experienceRequiredToLevel = this.GetExperienceRequiredToLevel(this._currentSkillLevel);
                this._experienceFromPreviousLevels = this.GetExperienceRequiredToLevel(this._currentSkillLevel - 1);
                this._experienceEarnedThisLevel = Game1.player.experiencePoints[currentLevelIndex] - this._experienceFromPreviousLevels;

                if (this._experienceRequiredToLevel <= 0 &&
                    this._levelExtenderAPI != null)
                {
                    this._experienceEarnedThisLevel = this._levelExtenderAPI.currentXP()[currentLevelIndex];
                    this._experienceFromPreviousLevels = this._currentExperience[currentLevelIndex] - this._experienceEarnedThisLevel;
                    this._experienceRequiredToLevel = this._levelExtenderAPI.requiredXP()[currentLevelIndex] + this._experienceFromPreviousLevels;
                }

                this.ShowExperienceBar();
                this._previousItem = currentItem;
            }

        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            if (!Game1.eventUp)
            {
                if (this._shouldDrawLevelUp)
                {
                    Vector2 playerLocalPosition = Game1.player.getLocalPosition(Game1.viewport);
                    Game1.spriteBatch.Draw(
                        Game1.mouseCursors,
                        new Vector2(
                            playerLocalPosition.X - 74,
                            playerLocalPosition.Y - 130),
                        this._levelUpIconRectangle,
                        this._iconColor,
                        0,
                        Vector2.Zero,
                        Game1.pixelZoom,
                        SpriteEffects.None,
                        0.85f);

                    Game1.drawWithBorder(
                        this._helper.SafeGetString(
                            LanguageKeys.LevelUp),
                        Color.DarkSlateGray,
                        Color.PaleTurquoise,
                        new Vector2(
                            playerLocalPosition.X - 28,
                            playerLocalPosition.Y - 130));
                }

                for (int i = this._experiencePointDisplays.Count - 1; i >= 0; --i)
                {
                    if (this._experiencePointDisplays[i].IsInvisible)
                    {
                        this._experiencePointDisplays.RemoveAt(i);
                    }
                    else
                    {
                        this._experiencePointDisplays[i].Draw();
                    }
                }

                if (this._experienceRequiredToLevel > 0 &&
                    this._experienceBarShouldBeVisible &&
                    this._showExperienceBar)
                {
                    int experienceDifferenceBetweenLevels = this._experienceRequiredToLevel - this._experienceFromPreviousLevels;
                    int barWidth = (int)((double)this._experienceEarnedThisLevel / experienceDifferenceBetweenLevels * MaxBarWidth);

                    this.DrawExperienceBar(barWidth, this._experienceEarnedThisLevel, experienceDifferenceBetweenLevels, this._currentSkillLevel);

                }

            }
        }

        private int GetExperienceRequiredToLevel(int currentLevel)
        {
            int amount = 0;

            //if (currentLevel < 10)
            //{
                switch (currentLevel)
                {
                    case 0: amount = 100; break;
                    case 1: amount = 380; break;
                    case 2: amount = 770; break;
                    case 3: amount = 1300; break;
                    case 4: amount = 2150; break;
                    case 5: amount = 3300; break;
                    case 6: amount = 4800; break;
                    case 7: amount = 6900; break;
                    case 8: amount = 10000; break;
                    case 9: amount = 15000; break;
                }
            //}
            //else if (_levelExtenderAPI != null &&
            //    currentLevel < 100)
            //{
            //    var requiredXP = _levelExtenderAPI.requiredXP();
            //    amount = requiredXP[currentLevel];
            //}
            return amount;
        }

        private void ShowExperienceBar()
        {
            if (this._timeToDisappear != null)
            {
                if (this._allowExperienceBarToFadeOut)
                {
                    this._timeToDisappear.Interval = this._timeBeforeExperienceBarFades.TotalMilliseconds;
                    this._timeToDisappear.Start();
                }
                else
                {
                    this._timeToDisappear.Stop();
                }
            }

            this._experienceBarShouldBeVisible = true;
        }

        private void DrawExperienceBar(int barWidth, int experienceGainedThisLevel, int experienceRequiredForNextLevel, int currentLevel)
        {
            float leftSide = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Left;

            if (Game1.isOutdoorMapSmallerThanViewport())
            {
                int num3 = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
                leftSide += (Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - num3) / 2;
            }

            Game1.drawDialogueBox(
                (int)leftSide,
                Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 160,
                240,
                160,
                false,
                true);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    barWidth,
                    31),
                this._experienceFillColor);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    Math.Min(4, barWidth),
                    31),
                this._experienceFillColor);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 63,
                    barWidth,
                    4),
                this._experienceFillColor);

            Game1.spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)leftSide + 32,
                    Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 36,
                    barWidth,
                    4),
                this._experienceFillColor);

            ClickableTextureComponent textureComponent =
                new ClickableTextureComponent(
                    "",
                    new Rectangle(
                        (int)leftSide - 36,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 80,
                        260,
                        100),
                    "",
                    "",
                    Game1.mouseCursors,
                    new Rectangle(0, 0, 0, 0),
                    Game1.pixelZoom);

            if (textureComponent.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                Game1.drawWithBorder(
                    experienceGainedThisLevel + "/" + experienceRequiredForNextLevel,
                    Color.Black,
                    Color.Black,
                    new Vector2(
                        leftSide + 33,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70));
            }
            else
            {
                Game1.spriteBatch.Draw(
                    Game1.mouseCursors,
                    new Vector2(
                        leftSide + 54,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 62),
                    this._experienceIconPosition,
                    this._iconColor,
                    0,
                    Vector2.Zero,
                    2.9f,
                    SpriteEffects.None,
                    0.85f);

                Game1.drawWithBorder(
                    currentLevel.ToString(),
                    Color.Black * 0.6f,
                    Color.Black,
                    new Vector2(
                        leftSide + 33,
                        Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - 70));
            }
        }

    }
}
