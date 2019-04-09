using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A tapper that accepts input and provides output.</summary>
    /// <remarks>See the game's machine logic in <see cref="SObject.performDropDownAction"/> and <see cref="SObject.checkForAction"/>.</remarks>
    internal class WormBinMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public WormBinMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            SObject bin = this.Machine;
            return new TrackedItem(bin.heldObject.Value, item =>
            {
                bin.heldObject.Value = new SObject(685, Game1.random.Next(2, 6));
                bin.MinutesUntilReady = 2600 - Game1.timeOfDay;
                bin.readyForHarvest.Value = false;
                bin.showNextIndex.Value = false;
            });
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            return false; // no input
        }
    }
}
