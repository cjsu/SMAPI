using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StardewValley;

namespace StardewModdingAPI.Framework.RewriteFacades
{
    public class NPCMethods : NPC
    {
        public Dictionary<int, SchedulePathDescription> getSchedule(int dayOfMonth)
        {
            return base.getSchedule(dayOfMonth, Game1.emergencyLoading);
        }

        public void checkSchedule(int timeOfDay)
        {
            base.checkSchedule(timeOfDay, Game1.emergencyLoading);
        }
    }
}
