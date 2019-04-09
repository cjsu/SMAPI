using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SFarmer = StardewValley.Farmer;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A crab pot that accepts input and provides output.</summary>
    /// <remarks>See the game's machine logic in <see cref="CrabPot.DayUpdate"/> and <see cref="CrabPot.performObjectDropInAction"/>.</remarks>
    internal class CrabPotMachine : GenericObjectMachine<CrabPot>
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private game code.</summary>
        private readonly IReflectionHelper Reflection;

        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>The fish IDs for which any crabpot has logged an 'invalid fish data' error.</summary>
        private static readonly ISet<int> LoggedInvalidDataErrors = new HashSet<int>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public CrabPotMachine(CrabPot machine, GameLocation location, Vector2 tile, IMonitor monitor, IReflectionHelper reflection)
            : base(machine, location, tile)
        {
            this.Monitor = monitor;
            this.Reflection = reflection;
        }

        /// <summary>Get the machine's processing state.</summary>
        public override MachineState GetState()
        {
            if (this.Machine.heldObject.Value == null)
            {
                bool hasBait = this.Machine.bait.Value != null || Game1.player.professions.Contains(11); // no bait needed if luremaster
                return hasBait
                    ? MachineState.Processing
                    : MachineState.Empty;
            }
            return this.Machine.readyForHarvest.Value
                ? MachineState.Done
                : MachineState.Processing;
        }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            return new TrackedItem(this.Machine.heldObject.Value, onEmpty: this.Reset);
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            // get bait
            if (input.TryGetIngredient(SObject.baitCategory, 1, out IConsumable bait))
            {
                this.Machine.bait.Value = (SObject)bait.Take();
                this.Reflection.GetField<bool>(this.Machine, "lidFlapping").SetValue(true);
                this.Reflection.GetField<float>(this.Machine, "lidFlapTimer").SetValue(60);
                return true;
            }

            return false;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Reset the machine so it's ready to accept a new input.</summary>
        /// <param name="item">The output item that was taken.</param>
        /// <remarks>XP and achievement logic based on <see cref="CrabPot.checkForAction"/>.</remarks>
        private void Reset(Item item)
        {
            CrabPot pot = this.Machine;

            // add fishing XP
            Game1.player.gainExperience(SFarmer.fishingSkill, 5);

            // mark fish caught for achievements and stats
            IDictionary<int, string> fishData = Game1.content.Load<Dictionary<int, string>>("Data\\Fish");
            if (fishData.TryGetValue(item.ParentSheetIndex, out string fishRow))
            {
                int size = 0;
                try
                {
                    string[] fields = fishRow.Split('/');
                    int lowerSize = fields.Length > 5 ? Convert.ToInt32(fields[5]) : 1;
                    int upperSize = fields.Length > 5 ? Convert.ToInt32(fields[6]) : 10;
                    size = Game1.random.Next(lowerSize, upperSize + 1);
                }
                catch (Exception ex)
                {
                    // The fish length stats don't affect anything, so it's not worth notifying the
                    // user; just log one trace message per affected fish for troubleshooting.
                    if (CrabPotMachine.LoggedInvalidDataErrors.Add(item.ParentSheetIndex))
                        this.Monitor.Log($"The game's fish data has an invalid entry (#{item.ParentSheetIndex}: {fishData[item.ParentSheetIndex]}). Automated crabpots won't track fish length stats for that fish.\n{ex}", LogLevel.Trace);
                }

                Game1.player.caughtFish(item.ParentSheetIndex, size);
            }

            // reset pot
            pot.readyForHarvest.Value = false;
            pot.heldObject.Value = null;
            pot.tileIndexToShow = 710;
            pot.bait.Value = null;
            this.Reflection.GetField<bool>(pot, "lidFlapping").SetValue(true);
            this.Reflection.GetField<float>(pot, "lidFlapTimer").SetValue(60f);
            this.Reflection.GetField<Vector2>(pot, "shake").SetValue(Vector2.Zero);
            this.Reflection.GetField<float>(pot, "shakeTimer").SetValue(0f);
        }
    }
}
