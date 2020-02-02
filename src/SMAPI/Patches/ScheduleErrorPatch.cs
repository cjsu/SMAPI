using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Harmony;
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="NPC.parseMasterSchedule"/> which intercepts crashes due to invalid schedule data.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class ScheduleErrorPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file on behalf of the game.</summary>
        private static IMonitor MonitorForGame;

        /// <summary>Whether the target is currently being intercepted.</summary>
        private static bool IsIntercepting;


        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(ScheduleErrorPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public ScheduleErrorPatch(IMonitor monitorForGame)
        {
            ScheduleErrorPatch.MonitorForGame = monitorForGame;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "parseMasterSchedule"),
                prefix: new HarmonyMethod(this.GetType(), nameof(ScheduleErrorPatch.Before_NPC_parseMasterSchedule))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="NPC.parseMasterSchedule"/>.</summary>
        /// <param name="rawData">The raw schedule data to parse.</param>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The patched method's return value.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_NPC_parseMasterSchedule(string rawData, NPC __instance, ref Dictionary<int, SchedulePathDescription> __result, MethodInfo __originalMethod)
        {
            if (ScheduleErrorPatch.IsIntercepting)
                return true;

            try
            {
                ScheduleErrorPatch.IsIntercepting = true;
                __result = (Dictionary<int, SchedulePathDescription>)__originalMethod.Invoke(__instance, new object[] { rawData });
                return false;
            }
            catch (TargetInvocationException ex)
            {
                ScheduleErrorPatch.MonitorForGame.Log($"Failed parsing schedule for NPC {__instance.Name}:\n{rawData}\n{ex.InnerException ?? ex}", LogLevel.Error);
                __result = new Dictionary<int, SchedulePathDescription>();
                return false;
            }
            finally
            {
                ScheduleErrorPatch.IsIntercepting = false;
            }
        }
    }
}
