using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Harmony;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Framework.Patching;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="StardewValley.Character.JunimoHarvesterPatch"/> which intercepts crashes due to invalid schedule data.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class JunimoHarvesterPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/

        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(JunimoHarvesterPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public JunimoHarvesterPatch(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.DeclaredConstructor(typeof(JunimoHarvester), new System.Type[] { typeof(Vector2), typeof(JunimoHut), typeof(int), typeof(Color?)}),
                prefix: new HarmonyMethod(this.GetType(), nameof(JunimoHarvesterPatch.Before_JunimoHarvester_create)),
                transpiler: new HarmonyMethod(this.GetType(), nameof(JunimoHarvesterPatch.Modify_JunimoHarvester_create))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="JunimoHarvester.ctor"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static void Before_JunimoHarvester_create(JunimoHarvester __instance, Vector2 position, JunimoHut myHome, int whichJunimoNumberFromThisHut, Color? c, MethodInfo __originalMethod)
        {
            try
            {
                Netcode.NetGuid guid = new Netcode.NetGuid(Game1.getFarm().buildings.GuidOf(myHome));
                Netcode.INetSerializable netFields = guid.NetFields;
                AccessTools.Field(typeof(JunimoHarvester), "netHome").SetValue(__instance, guid);
            }
            catch (TargetInvocationException ex)
            {
                JunimoHarvesterPatch.Monitor.Log($"Failed prepare home for JunimoHarvester {__instance.Name}:\n{ex.InnerException ?? ex}", LogLevel.Error);
            }
        }
        /// <summary>The method to call instead of <see cref="JunimoHarvester.ctor"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static IEnumerable<CodeInstruction> Modify_JunimoHarvester_create(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            for (int i = 0; i < codes.Count ; i++)
            {
                if (codes[i].opcode == OpCodes.Stfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(JunimoHarvester), "netHome"))
                {
                    codes.RemoveRange(i - 2, 3);
                    break;
                }
            }
            return codes.AsEnumerable();
        }
    }
}
