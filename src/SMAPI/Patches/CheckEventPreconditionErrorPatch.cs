using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Harmony;

using StardewModdingAPI.Framework.Patching;
using StardewModdingAPI.Framework.Reflection;

using StardewValley;

namespace StardewModdingAPI.Patches {
    /// <summary>A Harmony patch for the <see cref="Dialogue"/> constructor which intercepts invalid dialogue lines and logs an error instead of crashing.</summary>
    internal class CheckEventPreconditionErrorPatch : IHarmonyPatch {
        /*********
        ** Private methods
        *********/
        /// <summary>Writes messages to the console and log file on behalf of the game.</summary>
        private static IMonitor MonitorForGame;

        /// <summary>Local variable to store the original method.</summary>
        private static MethodInfo originalMethod;
        /// <summary>Local variable to check if the method was already arbitrated.</summary>
        private static bool isArbitrated;


        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(CheckEventPreconditionErrorPatch)}";


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        /// <param name="reflector">Simplifies access to private code.</param>
        public CheckEventPreconditionErrorPatch(IMonitor monitorForGame, Reflector reflector) {
            CheckEventPreconditionErrorPatch.MonitorForGame = monitorForGame;
        }


        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony) {
            originalMethod = AccessTools.Method(typeof(GameLocation), "checkEventPrecondition");
            harmony.Patch(originalMethod, new HarmonyMethod(AccessTools.Method(this.GetType(), "Prefix")));
        }

        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of the GameLocation.CheckEventPrecondition.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The return value of the original method.</param>
        /// <param name="precondition">The precondition to be parsed.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static bool Prefix(GameLocation __instance, ref int __result, string precondition) {
            if (isArbitrated) {
                isArbitrated = false;
                return true;
            } else {
                isArbitrated = true;
                try {
                    object _ = originalMethod.Invoke(__instance, new object[] { precondition });
                    __result = _ is null ? -1 : (int)_;
                } catch (System.Exception ex) {
                    __result = -1;
                    CheckEventPreconditionErrorPatch.MonitorForGame.Log($"Failed parsing event info. Event precondition: {precondition}\n{ex}", LogLevel.Error);
                }

                return false;
            }
        }
    }
}
