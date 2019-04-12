using StardewModdingAPI;
using System;

namespace JsonAssets
{
    class Log
    {
        public static void trace(String str)
        {
            Mod.instance.Monitor.Log(str, LogLevel.Trace);
        }

        public static void debug(String str)
        {
            Mod.instance.Monitor.Log(str, LogLevel.Debug);
        }

        public static void info(String str)
        {
            Mod.instance.Monitor.Log(str, LogLevel.Info);
        }

        public static void warn(String str)
        {
            Mod.instance.Monitor.Log(str, LogLevel.Warn);
        }

        public static void error(String str)
        {
            Mod.instance.Monitor.Log(str, LogLevel.Error);
        }
    }
}
