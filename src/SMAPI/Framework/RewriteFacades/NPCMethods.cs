using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class NPCMethods : NPC
    {
        public void checkSchedule(int timeOfDay)
        {
            base.checkSchedule(timeOfDay, Game1.emergencyLoading);
        }
    }
}
