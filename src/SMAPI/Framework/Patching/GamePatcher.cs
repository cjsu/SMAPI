using System;
#if HARMONY_2
using HarmonyLib;
#else
using Harmony;
#endif

namespace StardewModdingAPI.Framework.Patching
{
    /// <summary>Encapsulates applying Harmony patches to the game.</summary>
    internal class GamePatcher
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public GamePatcher(IMonitor monitor)
        {
            this.Monitor = monitor;
        }

        /// <summary>Apply all loaded patches to the game.</summary>
        /// <param name="patches">The patches to apply.</param>
        public void Apply(params IHarmonyPatch[] patches)
        {
#if HARMONY_2
            Harmony harmony = new Harmony("SMAPI");
#else
            HarmonyInstance harmony = HarmonyInstance.Create("SMAPI");
#endif
            foreach (IHarmonyPatch patch in patches)
            {
                try
                {
                    patch.Apply(harmony);
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Couldn't apply runtime patch '{patch.Name}' to the game. Some SMAPI features may not work correctly. See log file for details.", LogLevel.Error);
                    this.Monitor.Log(ex.GetLogSummary(), LogLevel.Trace);
                }
            }
        }
    }
}
