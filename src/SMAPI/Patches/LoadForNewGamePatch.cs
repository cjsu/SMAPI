using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using Harmony;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework.Patching;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;
using StardewValley.Menus;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="Game1.loadForNewGame"/> which notifies SMAPI for save creation load stages.</summary>
    /// <remarks>This patch hooks into <see cref="Game1.loadForNewGame"/>, checks if <c>TitleMenu.transitioningCharacterCreationMenu</c> is true (which means the player is creating a new save file), then raises <see cref="LoadStage.CreatedBasicInfo"/> after the location list is cleared twice (the second clear happens right before locations are created), and <see cref="LoadStage.CreatedLocations"/> when the method ends.</remarks>
    internal class LoadForNewGamePatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private code.</summary>
        private static Reflector Reflection;

        /// <summary>A callback to invoke when the load stage changes.</summary>
        private static Action<LoadStage> OnStageChanged;

        /// <summary>Whether <see cref="Game1.loadForNewGame"/> was called as part of save creation.</summary>
        private static bool IsCreating;

        /// <summary>The number of times that <see cref="Game1.locations"/> has been cleared since <see cref="Game1.loadForNewGame"/> started.</summary>
        private static int TimesLocationsCleared = 0;


        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(LoadForNewGamePatch)}";


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="onStageChanged">A callback to invoke when the load stage changes.</param>
        public LoadForNewGamePatch(Reflector reflection, Action<LoadStage> onStageChanged)
        {
            LoadForNewGamePatch.Reflection = reflection;
            LoadForNewGamePatch.OnStageChanged = onStageChanged;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            MethodInfo method = AccessTools.Method(typeof(Game1), nameof(Game1.loadForNewGame));
            MethodInfo prefix = AccessTools.Method(this.GetType(), nameof(LoadForNewGamePatch.Prefix));
            MethodInfo postfix = AccessTools.Method(this.GetType(), nameof(LoadForNewGamePatch.Postfix));

            harmony.Patch(method, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="Game1.loadForNewGame"/>.</summary>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static bool Prefix()
        {
            LoadForNewGamePatch.IsCreating = Game1.activeClickableMenu is TitleMenu menu && LoadForNewGamePatch.Reflection.GetField<bool>(menu, "transitioningCharacterCreationMenu").GetValue();
            LoadForNewGamePatch.TimesLocationsCleared = 0;
            if (LoadForNewGamePatch.IsCreating)
            {
                // raise CreatedBasicInfo after locations are cleared twice
                ObservableCollection<GameLocation> locations = (ObservableCollection<GameLocation>)Game1.locations;
                locations.CollectionChanged += LoadForNewGamePatch.OnLocationListChanged;
            }

            return true;
        }

        /// <summary>The method to call instead after <see cref="Game1.loadForNewGame"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static void Postfix()
        {
            if (LoadForNewGamePatch.IsCreating)
            {
                // clean up
                ObservableCollection<GameLocation> locations = (ObservableCollection<GameLocation>) Game1.locations;
                locations.CollectionChanged -= LoadForNewGamePatch.OnLocationListChanged;

                // raise stage changed
                LoadForNewGamePatch.OnStageChanged(LoadStage.CreatedLocations);
            }
        }

        /// <summary>Raised when <see cref="Game1.locations"/> changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnLocationListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (++LoadForNewGamePatch.TimesLocationsCleared == 2)
                LoadForNewGamePatch.OnStageChanged(LoadStage.CreatedBasicInfo);
        }
    }
}
