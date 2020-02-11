using System;
using System.Collections;
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
    internal class LinqEnumerablePatch : IHarmonyPatch
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
        public string Name => nameof(LinqEnumerablePatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        public LinqEnumerablePatch(IMonitor monitor)
        {
            Monitor = monitor;
        }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            MethodInfo methodInfo = AccessTools.FirstMethod(typeof(System.Linq.Enumerable), method => method.Name == "FirstOrDefault" && method.GetParameters().Length == 2).MakeGenericMethod(new Type[] { typeof(Item) });
            harmony.Patch(
                original: methodInfo,
                prefix: new HarmonyMethod(this.GetType(), nameof(LinqEnumerablePatch.Before_LinqEnumerablePatch_FirstOrDefault))
            );
            methodInfo = AccessTools.FirstMethod(typeof(System.Linq.Enumerable), method => method.Name == "Any" && method.GetParameters().Length == 2).MakeGenericMethod(new Type[] { typeof(Item) });
            harmony.Patch(
                original: methodInfo,
                prefix: new HarmonyMethod(this.GetType(), nameof(LinqEnumerablePatch.Before_LinqEnumerablePatch_Any))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of <see cref="System.Linq.Enumerable.FirstOrDefault"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_LinqEnumerablePatch_FirstOrDefault(IEnumerable<Item> source, Func<Item, bool> predicate, ref Item __result)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (Item local in source)
            {
                try
                {
                    if (predicate(local))
                    {
                        __result = local;
                        return false;
                    }
                }
                catch { }
            }
            __result = default(Item);
            return false;
        }
        private static bool Before_LinqEnumerablePatch_Any(IEnumerable<Item> source, Func<Item, bool> predicate, ref Item __result)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (predicate == null)
            {
                throw new ArgumentNullException("predicate");
            }
            foreach (Item local in source)
            {
                try
                {
                    if (predicate(local))
                    {
                        __result = local;
                        return false;
                    }
                }
                catch { }
            }
            __result = null;
            return false;
        }
    }
}
