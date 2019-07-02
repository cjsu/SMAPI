using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Harmony;
using StardewModdingAPI.Framework;
using StardewModdingAPI.Framework.Events;
using StardewModdingAPI.Framework.Patching;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    internal class SaveBackupPatch : IHarmonyPatch
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => $"{nameof(SaveBackupPatch)}";

        /// <summary>An Instance of <see cref="EventManager"/>.</summary>
        private static EventManager Events;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="eventManager">SMAPI's EventManager Instance</param>

        public SaveBackupPatch(EventManager eventManager)
        {
            SaveBackupPatch.Events = eventManager;
        }



        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            MethodInfo makeFullBackup = AccessTools.Method(typeof(Game1), nameof(Game1.MakeFullBackup));
            MethodInfo saveWholeBackup = AccessTools.Method(typeof(Game1), nameof(Game1.saveWholeBackup));

            MethodInfo prefix = AccessTools.Method(this.GetType(), nameof(SaveBackupPatch.Prefix));
            MethodInfo postfix = AccessTools.Method(this.GetType(), nameof(SaveBackupPatch.PostFix));

            harmony.Patch(makeFullBackup, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
            harmony.Patch(saveWholeBackup, new HarmonyMethod(prefix), new HarmonyMethod(postfix));
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="StardewValley.Object.getDescription"/>.</summary>
        /// <remarks>This method must be static for Harmony to work correctly. See the Harmony documentation before renaming arguments.</remarks>
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony.")]
        private static void Prefix()
        {
            SaveBackupPatch.Events.Saving.RaiseEmpty();
        }

        private static void PostFix()
        {
            SaveBackupPatch.Events.Saved.RaiseEmpty();
        }
    }
}
