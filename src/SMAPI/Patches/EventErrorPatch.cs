using System.Diagnostics.CodeAnalysis;
#if HARMONY_2
using System;
using HarmonyLib;
#else
using System.Reflection;
using Harmony;
#endif
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="GameLocation.checkEventPrecondition"/> which intercepts invalid preconditions and logs an error instead of crashing.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class EventErrorPatch : IHarmonyPatch
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
        public string Name => nameof(EventErrorPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public EventErrorPatch(IMonitor monitorForGame)
        {
            EventErrorPatch.MonitorForGame = monitorForGame;
        }

        /// <inheritdoc />
#if HARMONY_2
        public void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "checkEventPrecondition"),
                finalizer: new HarmonyMethod(this.GetType(), nameof(EventErrorPatch.Finalize_GameLocation_CheckEventPrecondition))
            );
        }
#else
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), "checkEventPrecondition"),
                prefix: new HarmonyMethod(this.GetType(), nameof(EventErrorPatch.Before_GameLocation_CheckEventPrecondition))
            );
        }
#endif


        /*********
        ** Private methods
        *********/
#if HARMONY_2
        /// <summary>The method to call instead of GameLocation.checkEventPrecondition.</summary>
        /// <param name="__result">The return value of the original method.</param>
        /// <param name="precondition">The precondition to be parsed.</param>
        /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
        /// <returns>Returns the exception to throw, if any.</returns>
        private static Exception Finalize_GameLocation_CheckEventPrecondition(ref int __result, string precondition, Exception __exception)
        {
            if (__exception != null)
            {
                __result = -1;
                EventErrorPatch.MonitorForGame.Log($"Failed parsing event precondition ({precondition}):\n{__exception.InnerException}", LogLevel.Error);
            }

            return null;
        }
#else
        /// <summary>The method to call instead of GameLocation.checkEventPrecondition.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The return value of the original method.</param>
        /// <param name="precondition">The precondition to be parsed.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_GameLocation_CheckEventPrecondition(GameLocation __instance, ref int __result, string precondition, MethodInfo __originalMethod)
        {
            const string key = nameof(Before_GameLocation_CheckEventPrecondition);
            if (!PatchHelper.StartIntercept(key))
                return true;

            try
            {
                __result = (int)__originalMethod.Invoke(__instance, new object[] { precondition });
                return false;
            }
            catch (TargetInvocationException ex)
            {
                __result = -1;
                EventErrorPatch.MonitorForGame.Log($"Failed parsing event precondition ({precondition}):\n{ex.InnerException}", LogLevel.Error);
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
