using System.Diagnostics.CodeAnalysis;
using Harmony;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Patching;
using StardewValley.Characters;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for <see cref="SpriteFont.MeasureString"/> which intercepts crashes due to invalid schedule data.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class SpriteFontPatch : IHarmonyPatch
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
        public string Name => nameof(SpriteFontPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public SpriteFontPatch(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(SpriteFont), "MeasureString", new System.Type[] { typeof(string)}),
                prefix: new HarmonyMethod(this.GetType(), nameof(SpriteFontPatch.Before_MeasureString_create))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="JunimoHarvester.ctor"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static void Before_MeasureString_create(SpriteFont __instance, ref string text)
        {
            if(text == null)
            {
                text = "";
            }
        }
    }
}
