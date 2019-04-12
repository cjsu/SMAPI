using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Reflection;
using UIInfoSuite.Extensions;

namespace UIInfoSuite.UIElements
{
    class ShowQueenOfSauceIcon : IDisposable
    {
        private Dictionary<string, string> _recipesByDescription = new Dictionary<string, string>();
        private Dictionary<string, string> _recipes = new Dictionary<string, string>();
        private string _todaysRecipe;
        private NPC _gus;
        private bool _drawQueenOfSauceIcon = false;
        private bool _drawDishOfDayIcon = false;
        private ClickableTextureComponent _queenOfSauceIcon;
        private readonly IModHelper _helper;

        public void ToggleOption(bool showQueenOfSauceIcon)
        {
            this._helper.Events.Display.RenderingHud -= this.OnRenderingHud;
            this._helper.Events.Display.RenderedHud -= this.OnRenderedHud;
            this._helper.Events.GameLoop.DayStarted -= this.OnDayStarted;
            this._helper.Events.GameLoop.UpdateTicked -= this.OnUpdateTicked;

            if (showQueenOfSauceIcon)
            {
                this.LoadRecipes();
                this.CheckForNewRecipe();
                this._helper.Events.GameLoop.DayStarted += this.OnDayStarted;
                this._helper.Events.Display.RenderingHud += this.OnRenderingHud;
                this._helper.Events.Display.RenderedHud += this.OnRenderedHud;
                this._helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            }
        }

        /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            // check if learned recipe
            if (e.IsOneSecond && this._drawQueenOfSauceIcon && Game1.player.knowsRecipe(this._todaysRecipe))
                this._drawQueenOfSauceIcon = false;
        }

        public ShowQueenOfSauceIcon(IModHelper helper)
        {
            this._helper = helper;
        }

        private void LoadRecipes()
        {
            if (this._recipes.Count == 0)
            {
                this._recipes = Game1.content.Load<Dictionary<string, string>>("Data\\TV\\CookingChannel");

                foreach (KeyValuePair<string, string> next in this._recipes)
                {
                    string[] values = next.Value.Split('/');

                    if (values.Length > 1)
                    {
                        this._recipesByDescription[values[1]] = values[0];
                    }
                }
            }
        }

        private void FindGus()
        {
            foreach (GameLocation location in Game1.locations)
            {
                foreach (NPC npc in location.characters)
                {
                    if (npc.Name == "Gus")
                    {
                        this._gus = npc;
                        break;
                    }
                }
                if (this._gus != null)
                    break;
            }
        }

        private string[] GetTodaysRecipe()
        {
            string[] array1 = new string[2];
            int recipeNum = (int)(Game1.stats.DaysPlayed % 224 / 7);
            //var recipes = Game1.content.Load<Dictionary<String, String>>("Data\\TV\\CookingChannel");

            string recipeValue = this._recipes.SafeGet(recipeNum.ToString());
            string[] splitValues = null;
            string key = null;
            bool checkCraftingRecipes = true;
            
            if (string.IsNullOrEmpty(recipeValue))
            {
                recipeValue = this._recipes["1"];
                checkCraftingRecipes = false;
            }
            splitValues = recipeValue.Split('/');
            key = splitValues[0];

            ///Game code sets this to splitValues[1] to display the language specific
            ///recipe name. We are skipping a bunch of their steps to just get the
            ///english name needed to tell if the player knows the recipe or not
            array1[0] = key;
            if (checkCraftingRecipes)
            {
                string craftingRecipesValue = CraftingRecipe.cookingRecipes.SafeGet(key);
                if (!string.IsNullOrEmpty(craftingRecipesValue))
                    splitValues = craftingRecipesValue.Split('/');
            }

            string languageRecipeName = (this._helper.Content.CurrentLocaleConstant == LocalizedContentManager.LanguageCode.en) ?
                key : splitValues[splitValues.Length - 1];

            array1[1] = languageRecipeName;

            //String str = null;
            //if (!Game1.player.cookingRecipes.ContainsKey(key))
            //{
            //    str = Game1.content.LoadString(@"Strings\StringsFromCSFiles:TV.cs.13153", languageRecipeName);
            //}
            //else
            //{
            //    str = Game1.content.LoadString(@"Strings\StringsFromCSFiles:TV.cs.13151", languageRecipeName);
            //}
            //array1[1] = str;

            return array1;
        }

        /// <summary>Raised before drawing the HUD (item toolbar, clock, etc) to the screen. The vanilla HUD may be hidden at this point (e.g. because a menu is open).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // draw icon
            if (!Game1.eventUp)
            {
                if (this._drawQueenOfSauceIcon)
                {
                    Point iconPosition = IconHandler.Handler.GetNewIconPosition();

                    this._queenOfSauceIcon = new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.mouseCursors,
                        new Rectangle(609, 361, 28, 28),
                        1.3f);
                    this._queenOfSauceIcon.draw(Game1.spriteBatch);
                }

                if (this._drawDishOfDayIcon)
                {
                    Point iconLocation = IconHandler.Handler.GetNewIconPosition();
                    float scale = 2.9f;

                    Game1.spriteBatch.Draw(
                        Game1.objectSpriteSheet,
                        new Vector2(iconLocation.X, iconLocation.Y),
                        new Rectangle(306, 291, 14, 14),
                        Color.White,
                        0,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        1f);

                    ClickableTextureComponent texture =
                        new ClickableTextureComponent(
                            this._gus.Name,
                            new Rectangle(
                                iconLocation.X - 7,
                                iconLocation.Y - 2,
                                (int)(16.0 * scale),
                                (int)(16.0 * scale)),
                            null,
                            this._gus.Name,
                            this._gus.Sprite.Texture,
                            this._gus.GetHeadShot(),
                            2f);

                    texture.draw(Game1.spriteBatch);

                    if (texture.containsPoint((int)(Game1.getMouseX() * Game1.options.zoomLevel), (int)(Game1.getMouseY() * Game1.options.zoomLevel)))
                    {
                        IClickableMenu.drawHoverText(
                            Game1.spriteBatch,
                            "Gus is selling " + Game1.dishOfTheDay.DisplayName + " recipe today!",
                            Game1.dialogueFont);
                    }
                }
            }
        }

        /// <summary>Raised after drawing the HUD (item toolbar, clock, etc) to the sprite batch, but before it's rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (this._drawQueenOfSauceIcon &&
                this._queenOfSauceIcon.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    this._helper.SafeGetString(
                        LanguageKeys.TodaysRecipe) + this._todaysRecipe,
                    Game1.dialogueFont);
            }
        }

        public void Dispose()
        {
            this.ToggleOption(false);
        }

        /// <summary>Raised after the game begins a new day (including when the player loads a save).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            this.CheckForNewRecipe();
        }

        private void CheckForNewRecipe()
        {
            TV tv = new TV();
            int numRecipesKnown = Game1.player.cookingRecipes.Count();
            string[] recipes = typeof(TV).GetMethod("getWeeklyRecipe", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tv, null) as string[];
            //String[] recipe = GetTodaysRecipe();
            //_todaysRecipe = recipe[1];
            this._todaysRecipe = this._recipesByDescription.SafeGet(recipes[0]);

            if (Game1.player.cookingRecipes.Count() > numRecipesKnown)
                Game1.player.cookingRecipes.Remove(this._todaysRecipe);

            this._drawQueenOfSauceIcon = (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0) &&
                Game1.stats.DaysPlayed > 5 && 
                !Game1.player.knowsRecipe(this._todaysRecipe);
            //_drawDishOfDayIcon = !Game1.player.knowsRecipe(Game1.dishOfTheDay.Name);
        }
    }
}
