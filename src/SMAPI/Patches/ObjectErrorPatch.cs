using System.Diagnostics.CodeAnalysis;
using Harmony;
using StardewModdingAPI.Framework.Patching;
using StardewValley;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="SObject.getDescription"/> which intercepts crashes due to the item no longer existing.</summary>
    internal class ObjectErrorPatch : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(ObjectErrorPatch)}";


        /*********
        ** Public methods
        *********/
        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            // object.getDescription
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.getDescription)),
                prefix: new HarmonyMethod(this.GetType(), nameof(ObjectErrorPatch.Before_Object_GetDescription))
            );

            // IClickableMenu.drawToolTip
            harmony.Patch(
                original: AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.drawToolTip)),
                prefix: new HarmonyMethod(this.GetType(), nameof(ObjectErrorPatch.Before_IClickableMenu_DrawTooltip))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="StardewValley.Object.getDescription"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The patched method's return value.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static bool Before_Object_GetDescription(SObject __instance, ref string __result)
        {
            // invalid bigcraftables crash instead of showing '???' like invalid non-bigcraftables
            if (!__instance.IsRecipe && __instance.bigCraftable.Value && !Game1.bigCraftablesInformation.ContainsKey(__instance.ParentSheetIndex))
            {
                __result = "???";
                return false;
            }

            return true;
        }

        /// <summary>The method to call instead of <see cref="IClickableMenu.drawToolTip"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="hoveredItem">The item for which to draw a tooltip.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static bool Before_IClickableMenu_DrawTooltip(IClickableMenu __instance, Item hoveredItem)
        {
            // invalid edible item cause crash when drawing tooltips
            if (hoveredItem is SObject obj && obj.Edibility != -300 && !Game1.objectInformation.ContainsKey(obj.ParentSheetIndex))
                return false;

            return true;
        }
    }
}
