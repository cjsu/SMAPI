using System;
using Android.OS;
using Harmony;
using MonoMod.RuntimeDetour;

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
            if (Build.VERSION.SdkInt < BuildVersionCodes.M)
                return;
            if (!HarmonyDetourBridge.Initialized)
            {
                HarmonyDetourBridge.Init();
            }

            HarmonyInstance harmony = HarmonyInstance.Create("io.smapi");
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

            //Keeping for reference
            //if (Build.VERSION.SdkInt > BuildVersionCodes.LollipopMr1)
            //{
            //}
        }
    }
}
