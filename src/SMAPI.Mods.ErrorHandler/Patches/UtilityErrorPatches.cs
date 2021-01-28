#if HARMONY_2
using System;
using HarmonyLib;
#else
using Harmony;
#endif
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Mods.ErrorHandler.Patches
{
    /// <summary>A Harmony patch for <see cref="Utility"/> methods to log more detailed errors.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class UtilityErrorPatches : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
        public string Name => nameof(UtilityErrorPatches);


        /*********
        ** Public methods
        *********/
        /// <inheritdoc />
#if HARMONY_2
        public void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), nameof(Utility.getItemFromStandardTextDescription)),
                finalizer: new HarmonyMethod(this.GetType(), nameof(UtilityErrorPatches.Finalize_Utility_GetItemFromStandardTextDescription))
            );
        }
#else
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Utility), nameof(Utility.getItemFromStandardTextDescription)),
                prefix: new HarmonyMethod(this.GetType(), nameof(UtilityErrorPatches.Before_Utility_GetItemFromStandardTextDescription))
            );
        }
#endif


        /*********
        ** Private methods
        *********/
#if HARMONY_2
        /// <summary>The method to call instead of <see cref="Utility.getItemFromStandardTextDescription"/>.</summary>
        /// <param name="description">The item text description to parse.</param>
        /// <param name="delimiter">The delimiter by which to split the text description.</param>
        /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
        /// <returns>Returns the exception to throw, if any.</returns>
        private static Exception Finalize_Utility_GetItemFromStandardTextDescription(string description, char delimiter, ref Exception __exception)
        {
            return __exception != null
                ? new FormatException($"Failed to parse item text description \"{description}\" with delimiter \"{delimiter}\".", __exception)
                : null;
        }
#else
        /// <summary>The method to call instead of <see cref="Utility.getItemFromStandardTextDescription"/>.</summary>
        /// <param name="__result">The return value of the original method.</param>
        /// <param name="description">The item text description to parse.</param>
        /// <param name="who">The player for which the item is being parsed.</param>
        /// <param name="delimiter">The delimiter by which to split the text description.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_Utility_GetItemFromStandardTextDescription(ref Item __result, string description, Farmer who, char delimiter, MethodInfo __originalMethod)
        {
            const string key = nameof(UtilityErrorPatches.Before_Utility_GetItemFromStandardTextDescription);
            if (!PatchHelper.StartIntercept(key))
                return true;

            try
            {
                __result = (Item)__originalMethod.Invoke(null, new object[] { description, who, delimiter });
                return false;
            }
            catch (TargetInvocationException ex)
            {
                throw new FormatException($"Failed to parse item text description \"{description}\" with delimiter \"{delimiter}\".", ex.InnerException);
            }
            finally
            {
                PatchHelper.StopIntercept(key);
            }
        }
#endif
    }
}
