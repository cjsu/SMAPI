using System;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>Invokes callbacks for mod hooks provided by the game.</summary>
    internal class SModHooks : ModHooks
    {
        /*********
        ** Fields
        *********/
        /// <summary>A callback to invoke before <see cref="Game1.newDayAfterFade"/> runs.</summary>
        private readonly Action BeforeNewDayAfterFade;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="beforeNewDayAfterFade">A callback to invoke before <see cref="Game1.newDayAfterFade"/> runs.</param>
        public SModHooks(Action beforeNewDayAfterFade)
        {
            this.BeforeNewDayAfterFade = beforeNewDayAfterFade;
        }

        /// <summary>A hook invoked when <see cref="Game1.newDayAfterFade"/> is called.</summary>
        /// <param name="action">The vanilla <see cref="Game1.newDayAfterFade"/> logic.</param>
        public override void OnGame1_NewDayAfterFade(Action action)
        {
            this.BeforeNewDayAfterFade?.Invoke();
            action();
        }
    }
}
