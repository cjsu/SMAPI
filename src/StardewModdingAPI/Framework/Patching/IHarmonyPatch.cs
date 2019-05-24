using Harmony;

namespace StardewModdingAPI.Framework.Patching
{
    /// <summary>A Harmony patch to apply.</summary>
    internal interface IHarmonyPatch
    {
        /// <summary>A unique name for this patch.</summary>
        string Name { get; }

        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        void Apply(HarmonyInstance harmony);
    }
}
