using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
    internal class LoadContextPatch : IHarmonyPatch
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
        private static int TimesLocationsCleared;


        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(LoadContextPatch)}";


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="onStageChanged">A callback to invoke when the load stage changes.</param>
        public LoadContextPatch(Reflector reflection, Action<LoadStage> onStageChanged)
        {
            LoadContextPatch.Reflection = reflection;
            LoadContextPatch.OnStageChanged = onStageChanged;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.loadForNewGame)),
                prefix: new HarmonyMethod(this.GetType(), nameof(LoadContextPatch.Before_Game1_LoadForNewGame)),
                postfix: new HarmonyMethod(this.GetType(), nameof(LoadContextPatch.After_Game1_LoadForNewGame))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="Game1.loadForNewGame"/>.</summary>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static bool Before_Game1_LoadForNewGame()
        {
            LoadContextPatch.IsCreating = Game1.activeClickableMenu is TitleMenu menu && LoadContextPatch.Reflection.GetField<bool>(menu, "transitioningCharacterCreationMenu").GetValue();
            LoadContextPatch.TimesLocationsCleared = 0;
            if (LoadContextPatch.IsCreating)
            {
                // raise CreatedBasicInfo after locations are cleared twice
                ObservableCollection<GameLocation> locations = (ObservableCollection<GameLocation>)Game1.locations;
                locations.CollectionChanged += LoadContextPatch.OnLocationListChanged;
            }

            return true;
        }

        /// <summary>The method to call instead after <see cref="Game1.loadForNewGame"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static void After_Game1_LoadForNewGame()
        {
            if (LoadContextPatch.IsCreating)
            {
                // clean up
                ObservableCollection<GameLocation> locations = (ObservableCollection<GameLocation>)Game1.locations;
                locations.CollectionChanged -= LoadContextPatch.OnLocationListChanged;

                // raise stage changed
                LoadContextPatch.OnStageChanged(LoadStage.CreatedLocations);
            }
        }

        /// <summary>Raised when <see cref="Game1.locations"/> changes.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnLocationListChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (++LoadContextPatch.TimesLocationsCleared == 2)
                LoadContextPatch.OnStageChanged(LoadStage.CreatedBasicInfo);
        }
    }
}
