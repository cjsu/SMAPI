namespace ContentPatcher.Framework.Conditions
{
    /// <summary>An in-game weather.</summary>
    internal enum Weather
    {
        /// <summary>The weather is sunny (including festival/wedding days). This is the default weather if no other value applies.</summary>
        Sun,

        /// <summary>Rain is falling, but without lightning.</summary>
        Rain,

        /// <summary>Rain is falling with lightning.</summary>
        Storm,

        /// <summary>Snow is falling.</summary>
        Snow,

        /// <summary>The wind is blowing with visible debris (e.g. flower petals in spring and leaves in fall).</summary>
        Wind
    }
}
