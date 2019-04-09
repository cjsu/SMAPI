using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A bee house that accepts input and provides output.</summary>
    /// <remarks>See the game's machine logic in <see cref="SObject.performDropDownAction"/>, <see cref="SObject.checkForAction"/>, and <see cref="SObject.minutesElapsed"/>.</remarks>
    internal class BeeHouseMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The honey types produced by this beehouse indexed by input ID.</summary>
        private readonly IDictionary<int, SObject.HoneyType> HoneyTypes = new Dictionary<int, SObject.HoneyType>
        {
            [376] = SObject.HoneyType.Poppy,
            [591] = SObject.HoneyType.Tulip,
            [593] = SObject.HoneyType.SummerSpangle,
            [595] = SObject.HoneyType.FairyRose,
            [597] = SObject.HoneyType.BlueJazz
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public BeeHouseMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Get the machine's processing state.</summary>
        public override MachineState GetState()
        {
            return Game1.currentSeason == "winter"
                ? MachineState.Disabled
                : base.GetState();
        }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            // get raw output
            SObject output = this.Machine.heldObject.Value;
            if (output == null)
                return null;

            // get flower data
            SObject.HoneyType type = SObject.HoneyType.Wild;
            string prefix = type.ToString();
            int addedPrice = 0;
            Crop flower = Utility.findCloseFlower(this.Location, this.Machine.TileLocation);
            if (flower != null)
            {
                string[] flowerData = Game1.objectInformation[flower.indexOfHarvest.Value].Split('/');
                prefix = flowerData[0];
                addedPrice = Convert.ToInt32(flowerData[1]) * 2;
                if (!this.HoneyTypes.TryGetValue(flower.indexOfHarvest.Value, out type))
                    type = SObject.HoneyType.Wild;
            }

            // build object
            SObject result = new SObject(output.ParentSheetIndex, output.Stack)
            {
                name = $"{prefix} Honey",
                Price = output.Price + addedPrice
            };
            result.honeyType.Value = type;

            // yield
            return new TrackedItem(result, onEmpty: this.Reset);
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            return false; // no input needed
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Reset the machine so it's ready to accept a new input.</summary>
        /// <param name="item">The output item that was taken.</param>
        private void Reset(Item item)
        {
            SObject machine = this.Machine;

            machine.heldObject.Value = new SObject(Vector2.Zero, 340, null, false, true, false, false);
            machine.MinutesUntilReady = 2400 - Game1.timeOfDay + 4320;
            machine.readyForHarvest.Value = false;
            machine.showNextIndex.Value = false;
        }
    }
}
