using ConvenientChests.CategorizeChests;
using ConvenientChests.StackToNearbyChests;
using StardewModdingAPI;

namespace ConvenientChests {
    /// <summary>The mod entry class loaded by SMAPI.</summary>
    public class ModEntry : StardewModdingAPI.Mod
    {
        public static   Config     Config        { get; private set; }
        internal static IModHelper StaticHelper  { get; private set; }
        internal static IMonitor   StaticMonitor { get; private set; }

        internal static void Log(string s, LogLevel l = LogLevel.Trace) => StaticMonitor.Log(s, l);

        public static StashToNearbyChestsModule StashNearby;
        public static CategorizeChestsModule    CategorizeChests;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper) {
            Config = helper.ReadConfig<Config>();
            StaticMonitor = this.Monitor;
            StaticHelper  = this.Helper;

            helper.Events.GameLoop.SaveLoaded      += (sender, e) => this.LoadModules();
            helper.Events.GameLoop.ReturnedToTitle += (sender, e) => this.UnloadModules();
        }

        private void LoadModules() {
            StashNearby = new StashToNearbyChestsModule(this);
            if (Config.StashToNearbyChests)
                StashNearby.Activate();

            CategorizeChests = new CategorizeChestsModule(this);
            if (Config.CategorizeChests)
                CategorizeChests.Activate();
        }

        private void UnloadModules() {
            StashNearby.Deactivate();
            StashNearby = null;
            
            CategorizeChests.Deactivate();
            CategorizeChests = null;
        }
    }
}
