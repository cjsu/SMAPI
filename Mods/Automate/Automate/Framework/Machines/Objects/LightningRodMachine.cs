using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A lightning rod that accepts input and provides output.</summary>
    internal class LightningRodMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public LightningRodMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            SObject heldObject = this.Machine.heldObject.Value;
            return new TrackedItem(heldObject.getOne(), onEmpty: this.GenericReset);
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
