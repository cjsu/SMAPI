#if !HARMONY_2
using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework.Patching
{
    /// <summary>Provides generic methods for implementing Harmony patches.</summary>
    internal class PatchHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The interception keys currently being intercepted.</summary>
        private static readonly HashSet<string> InterceptingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);


        /*********
        ** Public methods
        *********/
        /// <summary>Track a method that will be intercepted.</summary>
        /// <param name="key">The intercept key.</param>
        /// <returns>Returns false if the method was already marked for interception, else true.</returns>
        public static bool StartIntercept(string key)
        {
            return PatchHelper.InterceptingKeys.Add(key);
        }

        /// <summary>Track a method as no longer being intercepted.</summary>
        /// <param name="key">The intercept key.</param>
        public static void StopIntercept(string key)
        {
            PatchHelper.InterceptingKeys.Remove(key);
        }
    }
}
#endif
