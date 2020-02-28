using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using Android.OS;
using Harmony;
using MonoMod.RuntimeDetour;
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="StardewValley.MainActivity.OnCreate(Android.OS.Bundle)"/> to redirect bug report.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class AppCenterReportPatch : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(AppCenterReportPatch);

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(MainActivity), "OnCreate", new System.Type[] { typeof(Android.OS.Bundle)}),
                transpiler: new HarmonyMethod(this.GetType(), nameof(AppCenterReportPatch.Modify_MainActivity_OnCreate))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The patch logic <see cref="MainActivity.OnCreate"/>.</summary>
        private static IEnumerable<CodeInstruction> Modify_MainActivity_OnCreate(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count ; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && (string)codes[i].operand == "5677d40e-f7b3-4ccb-bee4-5dca56d86ade")
                {
                    codes[i].operand = Constants.MicrosoftAppSecret;
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        public static void ApplyPatch()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                HarmonyDetourBridge.Init();
                HarmonyInstance harmony = HarmonyInstance.Create("io.smapi.mainactivity");
                new AppCenterReportPatch().Apply(harmony);
            }
        }
    }
}
