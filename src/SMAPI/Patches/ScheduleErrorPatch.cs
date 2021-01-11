using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Framework.Patching;
using StardewValley;
#if HARMONY_2
using System;
using HarmonyLib;
using StardewModdingAPI.Framework;
#else
using System.Reflection;
using Harmony;
#endif

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


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
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

        /// <inheritdoc />
#if HARMONY_2
        public void Apply(Harmony harmony)
#else
        public void Apply(HarmonyInstance harmony)
#endif
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.parseMasterSchedule)),
#if HARMONY_2
                finalizer: new HarmonyMethod(this.GetType(), nameof(ScheduleErrorPatch.Finalize_NPC_parseMasterSchedule))
#else
                prefix: new HarmonyMethod(this.GetType(), nameof(ScheduleErrorPatch.Before_NPC_parseMasterSchedule))
#endif
            );
        }


        /*********
        ** Private methods
        *********/
#if HARMONY_2
        /// <summary>The method to call instead of <see cref="NPC.parseMasterSchedule"/>.</summary>
        /// <param name="rawData">The raw schedule data to parse.</param>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The patched method's return value.</param>
        /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
        /// <returns>Returns the exception to throw, if any.</returns>
        private static Exception Finalize_NPC_parseMasterSchedule(string rawData, NPC __instance, ref Dictionary<int, SchedulePathDescription> __result, Exception __exception)
        {
            if (__exception != null)
            {
                ScheduleErrorPatch.MonitorForGame.Log($"Failed parsing schedule for NPC {__instance.Name}:\n{rawData}\n{__exception.GetLogSummary()}", LogLevel.Error);
                __result = new Dictionary<int, SchedulePathDescription>();
            }

            return null;
        }
#else
        /// <summary>The method to call instead of <see cref="NPC.parseMasterSchedule"/>.</summary>
        /// <param name="rawData">The raw schedule data to parse.</param>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The patched method's return value.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_NPC_parseMasterSchedule(string rawData, NPC __instance, ref Dictionary<int, SchedulePathDescription> __result, MethodInfo __originalMethod)
        {
            const string key = nameof(Before_NPC_parseMasterSchedule);
            if (!PatchHelper.StartIntercept(key))
                return true;

            try
            {
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
                PatchHelper.StopIntercept(key);
            }
        }
#endif
    }
}
