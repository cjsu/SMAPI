using System;
using System.Diagnostics.CodeAnalysis;
using Harmony;
using StardewModdingAPI.Enums;
using StardewModdingAPI.Framework.Patching;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace StardewModdingAPI.Patches
{
    /// <summary>Harmony patches which notify SMAPI for save creation load stages.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class LoadContextPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private code.</summary>
        private static Reflector Reflection;

        /// <summary>A callback to invoke when the load stage changes.</summary>
        private static Action<LoadStage> OnStageChanged;


        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(LoadContextPatch);


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
            // detect CreatedBasicInfo
            harmony.Patch(
                original: AccessTools.Method(typeof(TitleMenu), nameof(TitleMenu.createdNewCharacter)),
                prefix: new HarmonyMethod(this.GetType(), nameof(LoadContextPatch.Before_TitleMenu_CreatedNewCharacter))
            );

            // detect CreatedLocations
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), nameof(Game1.loadForNewGame)),
                postfix: new HarmonyMethod(this.GetType(), nameof(LoadContextPatch.After_Game1_LoadForNewGame))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Called before <see cref="TitleMenu.createdNewCharacter"/>.</summary>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static bool Before_TitleMenu_CreatedNewCharacter()
        {
            LoadContextPatch.OnStageChanged(LoadStage.CreatedBasicInfo);
            return true;
        }

        /// <summary>Called after <see cref="Game1.loadForNewGame"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        private static void After_Game1_LoadForNewGame()
        {
            bool creating =
                (Game1.currentMinigame is Intro) // creating save with intro
                || (Game1.activeClickableMenu is TitleMenu menu && LoadContextPatch.Reflection.GetField<bool>(menu, "transitioningCharacterCreationMenu").GetValue()); // creating save, skipped intro

            if (creating)
                LoadContextPatch.OnStageChanged(LoadStage.CreatedLocations);
        }
    }
}
