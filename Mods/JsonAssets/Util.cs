using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonAssets
{
    // Copied from SpaceCore
    // TODO: Add SC as a dependency instead
    public class Util
    {
        // Stolen from SMAPI
        public static void invokeEvent(string name, IEnumerable<Delegate> handlers, object sender)
        {
            EventArgs args = new EventArgs();
            foreach (EventHandler handler in handlers.Cast<EventHandler>())
            {
                try
                {
                    handler.Invoke(sender, args);
                }
                catch (Exception e)
                {
                    Log.error($"Exception while handling event {name}:\n{e}");
                }
            }
        }
    }
}
