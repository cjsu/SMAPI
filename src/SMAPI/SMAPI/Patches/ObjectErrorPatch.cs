using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Harmony;
using StardewModdingAPI.Framework.Patching;
using StardewValley;
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
            MethodInfo method = AccessTools.Method(typeof(SObject), nameof(SObject.getDescription));
            MethodInfo prefix = AccessTools.Method(this.GetType(), nameof(ObjectErrorPatch.Prefix));

            harmony.Patch(method, new HarmonyMethod(prefix), null);
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
        private static bool Prefix(SObject __instance, ref string __result)
        {
            // invalid bigcraftables crash instead of showing '???' like invalid non-bigcraftables
            if (!__instance.IsRecipe && __instance.bigCraftable.Value && !Game1.bigCraftablesInformation.ContainsKey(__instance.ParentSheetIndex))
            {
                __result = "???";
                return false;
            }

            return true;
        }
    }
}
