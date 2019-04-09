using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A loom that accepts input and provides output.</summary>
    internal class LoomMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The recipes to process.</summary>
        private readonly IRecipe[] Recipes =
        {
            // wool => cloth
            new Recipe(
                input: 440,
                inputCount: 1,
                output: item => new SObject(Vector2.Zero, 428, null, false, true, false, false),
                minutes: 240
            )
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public LoomMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            SObject machine = this.Machine;
            return new TrackedItem(machine.heldObject.Value, item =>
            {
                machine.heldObject.Value = null;
                machine.readyForHarvest.Value = false;
                machine.showNextIndex.Value = false;
            });
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            return this.GenericPullRecipe(input, this.Recipes);
        }
    }
}
