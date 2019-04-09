using System;
using System.Collections.Generic;
using System.Linq;
using Pathoschild.Stardew.Automate.Framework;
using Pathoschild.Stardew.Automate.Framework.Models;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.Utilities;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Pathoschild.Stardew.Automate
{
    /// <summary>The mod entry point.</summary>
    internal class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mod configuration.</summary>
        private ModConfig Config;

        /// <summary>Constructs machine groups.</summary>
        private MachineGroupFactory Factory;

        /// <summary>Whether to enable automation for the current save.</summary>
        private bool EnableAutomation => Context.IsMainPlayer;

        /// <summary>The machines to process.</summary>
        private readonly IDictionary<GameLocation, MachineGroup[]> ActiveMachineGroups = new Dictionary<GameLocation, MachineGroup[]>(new ObjectReferenceComparer<GameLocation>());

        /// <summary>The disabled machine groups (e.g. machines not connected to a chest).</summary>
        private readonly IDictionary<GameLocation, MachineGroup[]> DisabledMachineGroups = new Dictionary<GameLocation, MachineGroup[]>(new ObjectReferenceComparer<GameLocation>());

        /// <summary>The locations that should be reloaded on the next update tick.</summary>
        private readonly HashSet<GameLocation> ReloadQueue = new HashSet<GameLocation>(new ObjectReferenceComparer<GameLocation>());

        /// <summary>The number of ticks until the next automation cycle.</summary>
        private int AutomateCountdown;

        /// <summary>The current overlay being displayed, if any.</summary>
        private OverlayMenu CurrentOverlay;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
        public override void Entry(IModHelper helper)
        {
            // toggle mod compatibility
            bool hasBetterJunimos = helper.ModRegistry.IsLoaded("hawkfalcon.BetterJunimos");
            bool hasDeluxeAutoGrabber = helper.ModRegistry.IsLoaded("stokastic.DeluxeGrabber");

            // init
            this.Config = helper.ReadConfig<ModConfig>();
            this.Factory = new MachineGroupFactory();
            this.Factory.Add(new AutomationFactory(this.Config.Connectors, this.Config.AutomateShippingBin, this.Monitor, helper.Reflection, hasBetterJunimos, hasDeluxeAutoGrabber));

            // hook events
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.Player.Warped += this.OnWarped;
            helper.Events.World.LocationListChanged += this.World_LocationListChanged;
            helper.Events.World.ObjectListChanged += this.World_ObjectListChanged;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;

            if (this.Config.Connectors.Any(p => p.Type == ObjectType.Floor))
                helper.Events.World.TerrainFeatureListChanged += this.World_TerrainFeatureListChanged;

            // log info
            this.Monitor.VerboseLog($"Initialised with automation every {this.Config.AutomationInterval} ticks.");
        }

        /// <summary>Get an API that other mods can access. This is always called after <see cref="Entry" />.</summary>
        public override object GetApi()
        {
            return new AutomateAPI(this.Monitor, this.Factory, this.ActiveMachineGroups, this.DisabledMachineGroups);
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Event handlers
        ****/
        /// <summary>The method invoked when the player loads a save.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            // disable if secondary player
            if (!this.EnableAutomation)
                this.Monitor.Log("Disabled automation (only the main player can automate machines in multiplayer mode).", LogLevel.Warn);

            // reset
            this.ActiveMachineGroups.Clear();
            this.DisabledMachineGroups.Clear();
            this.AutomateCountdown = this.Config.AutomationInterval;
            this.DisableOverlay();
            foreach (GameLocation location in CommonHelper.GetLocations())
                this.ReloadQueue.Add(location);
        }

        /// <summary>The method invoked when the player warps to a new location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnWarped(object sender, WarpedEventArgs e)
        {
            if (e.IsLocalPlayer)
                this.ResetOverlayIfShown();
        }

        /// <summary>The method invoked when a location is added or removed.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void World_LocationListChanged(object sender, LocationListChangedEventArgs e)
        {
            if (!this.EnableAutomation)
                return;

            this.Monitor.VerboseLog("Location list changed, reloading all machines.");

            try
            {
                this.ActiveMachineGroups.Clear();
                this.DisabledMachineGroups.Clear();
                foreach (GameLocation location in CommonHelper.GetLocations())
                    this.ReloadQueue.Add(location);
            }
            catch (Exception ex)
            {
                this.HandleError(ex, "updating locations");
            }
        }

        /// <summary>The method invoked when an object is added or removed to a location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void World_ObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            if (!this.EnableAutomation)
                return;

            this.Monitor.VerboseLog($"Object list changed in {e.Location.Name}, reloading machines in current location.");
            this.ReloadQueue.Add(e.Location);
        }

        /// <summary>The method invoked when a terrain feature is added or removed to a location.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void World_TerrainFeatureListChanged(object sender, TerrainFeatureListChangedEventArgs e)
        {
            if (!this.EnableAutomation)
                return;

            this.Monitor.VerboseLog($"Terrain feature list changed in {e.Location.Name}, reloading machines in current location.");
            this.ReloadQueue.Add(e.Location);
        }

        /// <summary>The method invoked when the in-game clock time changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !this.EnableAutomation)
                return;

            try
            {
                // handle delay
                this.AutomateCountdown--;
                if (this.AutomateCountdown > 0)
                    return;
                this.AutomateCountdown = this.Config.AutomationInterval;

                // reload machines if needed
                if (this.ReloadQueue.Any())
                {
                    foreach (GameLocation location in this.ReloadQueue)
                        this.ReloadMachinesIn(location);
                    this.ReloadQueue.Clear();

                    this.ResetOverlayIfShown();
                }

                // process machines
                foreach (MachineGroup group in this.GetActiveMachineGroups())
                    group.Automate();
            }
            catch (Exception ex)
            {
                this.HandleError(ex, "processing machines");
            }
        }

        /// <summary>The method invoked when the player presses a button.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            try
            {
                // toggle overlay
                if (Context.IsPlayerFree && this.Config.Controls.ToggleOverlay.Contains(e.Button))
                {
                    if (this.CurrentOverlay != null)
                        this.DisableOverlay();
                    else
                        this.EnableOverlay();
                }
            }
            catch (Exception ex)
            {
                this.HandleError(ex, "handling key input");
            }
        }

        /****
        ** Methods
        ****/
        /// <summary>Get the active machine groups in every location.</summary>
        private IEnumerable<MachineGroup> GetActiveMachineGroups()
        {
            foreach (KeyValuePair<GameLocation, MachineGroup[]> group in this.ActiveMachineGroups)
            {
                foreach (MachineGroup machineGroup in group.Value)
                    yield return machineGroup;
            }
        }

        /// <summary>Reload the machines in a given location.</summary>
        /// <param name="location">The location whose machines to reload.</param>
        private void ReloadMachinesIn(GameLocation location)
        {
            this.Monitor.VerboseLog($"Reloading machines in {location.Name}...");

            // get machine groups
            MachineGroup[] machineGroups = this.Factory.GetMachineGroups(location).ToArray();
            this.ActiveMachineGroups[location] = machineGroups.Where(p => p.HasInternalAutomation).ToArray();
            this.DisabledMachineGroups[location] = machineGroups.Where(p => !p.HasInternalAutomation).ToArray();

            // remove unneeded entries
            if (!this.ActiveMachineGroups[location].Any())
                this.ActiveMachineGroups.Remove(location);
            if (!this.DisabledMachineGroups[location].Any())
                this.DisabledMachineGroups.Remove(location);
        }

        /// <summary>Log an error and warn the user.</summary>
        /// <param name="ex">The exception to handle.</param>
        /// <param name="verb">The verb describing where the error occurred (e.g. "looking that up").</param>
        private void HandleError(Exception ex, string verb)
        {
            this.Monitor.Log($"Something went wrong {verb}:\n{ex}", LogLevel.Error);
            CommonHelper.ShowErrorMessage($"Huh. Something went wrong {verb}. The error log has the technical details.");
        }

        /// <summary>Disable the overlay, if shown.</summary>
        private void DisableOverlay()
        {
            this.CurrentOverlay?.Dispose();
            this.CurrentOverlay = null;
        }

        /// <summary>Enable the overlay.</summary>
        private void EnableOverlay()
        {
            if (this.CurrentOverlay == null)
                this.CurrentOverlay = new OverlayMenu(this.Helper.Events, this.Helper.Input, this.Factory.GetMachineGroups(Game1.currentLocation));
        }

        /// <summary>Reset the overlay if it's being shown.</summary>
        private void ResetOverlayIfShown()
        {
            if (this.CurrentOverlay != null)
            {
                this.DisableOverlay();
                this.EnableOverlay();
            }
        }
    }
}
