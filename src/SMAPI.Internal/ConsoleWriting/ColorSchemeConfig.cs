using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Internal.ConsoleWriting
{
    /// <summary>The console color scheme options.</summary>
    internal class ColorSchemeConfig
    {
        /// <summary>The default color scheme ID to use, or <see cref="MonitorColorScheme.AutoDetect"/> to select one automatically.</summary>
        public MonitorColorScheme UseScheme { get; set; }

        /// <summary>The available console color schemes.</summary>
        public IDictionary<MonitorColorScheme, IDictionary<ConsoleLogLevel, ConsoleColor>> Schemes { get; set; }
    }
}
