using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.Integrations.CustomFarmingRedux;
using Pathoschild.Stardew.LookupAnything.Components;
using Pathoschild.Stardew.LookupAnything.Framework;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Subjects;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Pathoschild.Stardew.LookupAnything
{
    /// <summary>The mod entry point.</summary>
    internal class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /****
        ** Configuration
        ****/
        /// <summary>The mod configuration.</summary>
        private ModConfig Config;

        /// <summary>Provides metadata that's not available from the game data directly.</summary>
        private Metadata Metadata;

        /// <summary>The name of the file containing data for the <see cref="Metadata"/> field.</summary>
        private readonly string DatabaseFileName = "data.json";

        /****
        ** Validation
        ****/
        /// <summary>Whether the metadata validation passed.</summary>
        private bool IsDataValid;

        /****
        ** State
        ****/
        /// <summary>The previous menus shown before the current lookup UI was opened.</summary>
        private readonly Stack<IClickableMenu> PreviousMenus = new Stack<IClickableMenu>();

        /// <summary>Provides utility methods for interacting with the game code.</summary>
        private GameHelper GameHelper;

        /// <summary>Finds and analyses lookup targets in the world.</summary>
        private TargetFactory TargetFactory;

        /// <summary>Draws debug information to the screen.</summary>
        private DebugInterface DebugInterface;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
        public override void Entry(IModHelper helper)
        {
            // load config
            this.Config = this.Helper.ReadConfig<ModConfig>();

            // load & validate database
            this.LoadMetadata();
            this.IsDataValid = this.Metadata.LooksValid();
            if (!this.IsDataValid)
            {
                this.Monitor.Log("The data.json file seems to be missing or corrupt. Lookups will be disabled.", LogLevel.Error);
                this.IsDataValid = false;
            }

            // validate translations
            if (!helper.Translation.GetTranslations().Any())
                this.Monitor.Log("The translation files in this mod's i18n folder seem to be missing. The mod will still work, but you'll see 'missing translation' messages. Try reinstalling the mod to fix this.", LogLevel.Warn);

            // hook up events
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.Display.RenderedHud += this.OnRenderedHud;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            if (this.Config.HideOnKeyUp)
                helper.Events.Input.ButtonReleased += this.OnButtonReleased;
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Event handlers
        ****/
        /// <summary>The method invoked on the first update tick, once all mods are initialised.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if (!this.IsDataValid)
                return;

            // initialise functionality
            var customFarming = new CustomFarmingReduxIntegration(this.Helper.ModRegistry, this.Monitor);
            this.GameHelper = new GameHelper(customFarming);
            this.TargetFactory = new TargetFactory(this.Metadata, this.Helper.Translation, this.Helper.Reflection, this.GameHelper);
            this.DebugInterface = new DebugInterface(this.GameHelper, this.TargetFactory, this.Config, this.Monitor);
        }

        /// <summary>The method invoked when a new day starts.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            // reset low-level cache once per game day (used for expensive queries that don't change within a day)
            this.GameHelper.ResetCache(this.Metadata, this.Helper.Reflection, this.Helper.Translation, this.Monitor);
        }

        /// <summary>The method invoked when the player presses a button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            this.Monitor.InterceptErrors("handling your input", $"handling input '{e.Button}'", () =>
            {
                var controls = this.Config.Controls;

                if (controls.ToggleLookup.Contains(e.Button))
                    this.ToggleLookup(LookupMode.Cursor);
                else if (controls.ToggleLookupInFrontOfPlayer.Contains(e.Button) && Context.IsWorldReady)
                    this.ToggleLookup(LookupMode.FacingPlayer);
                else if (controls.ScrollUp.Contains(e.Button))
                    (Game1.activeClickableMenu as LookupMenu)?.ScrollUp();
                else if (controls.ScrollDown.Contains(e.Button))
                    (Game1.activeClickableMenu as LookupMenu)?.ScrollDown();
                else if (controls.ToggleDebug.Contains(e.Button) && Context.IsPlayerFree)
                    this.DebugInterface.Enabled = !this.DebugInterface.Enabled;
            });
        }

        /// <summary>The method invoked when the player releases a button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            // perform bound action
            this.Monitor.InterceptErrors("handling your input", $"handling input release '{e.Button}'", () =>
            {
                var controls = this.Config.Controls;

                if (controls.ToggleLookup.Contains(e.Button) || controls.ToggleLookupInFrontOfPlayer.Contains(e.Button))
                    this.HideLookup();
            });
        }

        /// <summary>The method invoked when the player closes a displayed menu.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // restore the previous menu if it was hidden to show the lookup UI
            this.Monitor.InterceptErrors("restoring the previous menu", () =>
            {
                if (e.NewMenu == null && e.OldMenu is LookupMenu && this.PreviousMenus.Any())
                    Game1.activeClickableMenu = this.PreviousMenus.Pop();
            });
        }

        /// <summary>The method invoked when the interface is rendering.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // render debug interface
            if (this.DebugInterface.Enabled)
                this.DebugInterface.Draw(Game1.spriteBatch);
        }

        /****
        ** Helpers
        ****/
        /// <summary>Show the lookup UI for the current target.</summary>
        /// <param name="lookupMode">The lookup target mode.</param>
        private void ToggleLookup(LookupMode lookupMode)
        {
            if (Game1.activeClickableMenu is LookupMenu)
                this.HideLookup();
            else
                this.ShowLookup(lookupMode);
        }

        /// <summary>Show the lookup UI for the current target.</summary>
        /// <param name="lookupMode">The lookup target mode.</param>
        private void ShowLookup(LookupMode lookupMode)
        {
            // disable lookups if metadata is invalid
            if (!this.IsDataValid)
            {
                this.GameHelper.ShowErrorMessage("The mod doesn't seem to be installed correctly: its data.json file is missing or corrupt.");
                return;
            }

            // show menu
            StringBuilder logMessage = new StringBuilder("Received a lookup request...");
            this.Monitor.InterceptErrors("looking that up", () =>
            {
                try
                {
                    // get target
                    ISubject subject = this.GetSubject(logMessage, lookupMode);
                    if (subject == null)
                    {
                        this.Monitor.Log($"{logMessage} no target found.", LogLevel.Trace);
                        return;
                    }

                    // show lookup UI
                    this.Monitor.Log(logMessage.ToString(), LogLevel.Trace);
                    this.ShowLookupFor(subject);
                }
                catch
                {
                    this.Monitor.Log($"{logMessage} an error occurred.", LogLevel.Trace);
                    throw;
                }
            });
        }

        /// <summary>Show a lookup menu for the given subject.</summary>
        /// <param name="subject">The subject to look up.</param>
        internal void ShowLookupFor(ISubject subject)
        {
            this.Monitor.InterceptErrors("looking that up", () =>
            {
                this.Monitor.Log($"Showing {subject.GetType().Name}::{subject.Type}::{subject.Name}.", LogLevel.Trace);

                LookupMenu lookupMenu = new LookupMenu(this.GameHelper, subject, this.Metadata, this.Monitor, this.Helper.Reflection, this.Config.ScrollAmount, this.Config.ShowDataMiningFields, this.ShowLookupFor);
                if (this.ShouldRestoreMenu(Game1.activeClickableMenu))
                {
                    this.PreviousMenus.Push(Game1.activeClickableMenu);
                    this.Helper.Reflection.GetField<IClickableMenu>(typeof(Game1), "_activeClickableMenu").SetValue(lookupMenu); // bypass Game1.activeClickableMenu, which disposes the previous menu
                }
                else
                    Game1.activeClickableMenu = lookupMenu;
            });
        }

        /// <summary>Get the most relevant subject under the player's cursor.</summary>
        /// <param name="logMessage">The log message to which to append search details.</param>
        /// <param name="lookupMode">The lookup target mode.</param>
        private ISubject GetSubject(StringBuilder logMessage, LookupMode lookupMode)
        {
            // menu under cursor
            if (lookupMode == LookupMode.Cursor)
            {
                Vector2 cursorPos = this.GameHelper.GetScreenCoordinatesFromCursor();

                // try menu
                if (Game1.activeClickableMenu != null)
                {
                    logMessage.Append($" searching the open '{Game1.activeClickableMenu.GetType().Name}' menu...");
                    return this.TargetFactory.GetSubjectFrom(Game1.activeClickableMenu, cursorPos);
                }

                // try HUD under cursor
                foreach (IClickableMenu menu in Game1.onScreenMenus)
                {
                    if (menu.isWithinBounds((int)cursorPos.X, (int)cursorPos.Y))
                    {
                        logMessage.Append($" searching the on-screen '{menu.GetType().Name}' menu...");
                        return this.TargetFactory.GetSubjectFrom(menu, cursorPos);
                    }
                }
            }

            // try world
            if (Game1.activeClickableMenu == null)
            {
                logMessage.Append(" searching the world...");
                return this.TargetFactory.GetSubjectFrom(Game1.player, Game1.currentLocation, lookupMode, this.Config.EnableTileLookups);
            }

            // not found
            return null;
        }

        /// <summary>Show the lookup UI for the current target.</summary>
        private void HideLookup()
        {
            this.Monitor.InterceptErrors("closing the menu", () =>
            {
                if (Game1.activeClickableMenu is LookupMenu)
                {
                    Game1.playSound("bigDeSelect"); // match default behaviour when closing a menu
                    Game1.activeClickableMenu = null;
                }
            });
        }

        /// <summary>Load the file containing metadata that's not available from the game directly.</summary>
        private void LoadMetadata()
        {
            this.Monitor.InterceptErrors("loading metadata", () =>
            {
                this.Metadata = this.Helper.Data.ReadJsonFile<Metadata>(this.DatabaseFileName);
            });
        }

        /// <summary>Get whether a given menu should be restored when the lookup ends.</summary>
        /// <param name="menu">The menu to check.</param>
        private bool ShouldRestoreMenu(IClickableMenu menu)
        {
            // no menu
            if (menu == null)
                return false;

            // if 'hide on key up' is enabled, all lookups should close on key up
            if (this.Config.HideOnKeyUp && menu is LookupMenu)
                return false;

            return true;
        }
    }
}
