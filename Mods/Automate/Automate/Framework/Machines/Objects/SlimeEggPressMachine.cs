using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A slime egg-press that accepts input and provides output.</summary>
    internal class SlimeEggPressMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public SlimeEggPressMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            SObject heldObject = this.Machine.heldObject.Value;
            return new TrackedItem(heldObject.getOne(), this.GenericReset);
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            // slime => slime egg
            if (input.TryConsume(766, 100))
            {
                int parentSheetIndex = 680;
                if (Game1.random.NextDouble() < 0.05)
                    parentSheetIndex = 439;
                else if (Game1.random.NextDouble() < 0.1)
                    parentSheetIndex = 437;
                else if (Game1.random.NextDouble() < 0.25)
                    parentSheetIndex = 413;
                this.Machine.heldObject.Value = new SObject(parentSheetIndex, 1);
                this.Machine.MinutesUntilReady = 1200;
                return true;
            }

            return false;
        }
    }
}
