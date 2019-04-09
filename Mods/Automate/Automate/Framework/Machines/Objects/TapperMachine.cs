using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A tapper that accepts input and provides output.</summary>
    internal class TapperMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The tree type.</summary>
        private readonly int TreeType;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location to search.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        /// <param name="treeType">The tree type being tapped.</param>
        public TapperMachine(SObject machine, GameLocation location, Vector2 tile, int treeType)
            : base(machine, location, tile)
        {
            this.TreeType = treeType;
        }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            SObject heldObject = this.Machine.heldObject.Value;
            return new TrackedItem(heldObject.getOne(), onEmpty: this.Reset);
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            return false; // no input
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Reset the machine so it's ready to accept a new input.</summary>
        /// <param name="item">The output item that was taken.</param>
        private void Reset(Item item)
        {
            SObject tapper = this.Machine;

            switch (this.TreeType)
            {
                case 1:
                    tapper.heldObject.Value = new SObject(725, 1);
                    tapper.MinutesUntilReady = 13000 - Game1.timeOfDay;
                    break;
                case 2:
                    tapper.heldObject.Value = new SObject(724, 1);
                    tapper.MinutesUntilReady = 16000 - Game1.timeOfDay;
                    break;
                case 3:
                    tapper.heldObject.Value = new SObject(726, 1);
                    tapper.MinutesUntilReady = 10000 - Game1.timeOfDay;
                    break;
                case 7:
                    tapper.heldObject.Value = new SObject(420, 1);
                    tapper.MinutesUntilReady = 3000 - Game1.timeOfDay;
                    if (!Game1.currentSeason.Equals("fall"))
                    {
                        tapper.heldObject.Value = new SObject(404, 1);
                        tapper.MinutesUntilReady = 6000 - Game1.timeOfDay;
                    }
                    break;
            }
            tapper.heldObject.Value = (SObject)tapper.heldObject.Value.getOne();
            tapper.readyForHarvest.Value = false;
        }
    }
}
