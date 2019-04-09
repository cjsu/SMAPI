using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A coop incubator that accepts eggs and spawns chickens.</summary>
    internal class CoopIncubatorMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The recipes to process.</summary>
        private readonly IRecipe[] Recipes;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public CoopIncubatorMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile)
        {
            int minutesUntilReady = Game1.player.professions.Contains(2) ? 9000 : 18000;
            this.Recipes = new IRecipe[]
            {
                // egg => chicken
                new Recipe(
                    input: -5,
                    inputCount: 1,
                    output: item => new SObject(item.ParentSheetIndex, 1),
                    minutes: minutesUntilReady / 2
                ),

                // dinosaur egg => dinosaur
                new Recipe(
                    input: 107,
                    inputCount: 1,
                    output: item => new SObject(107, 1),
                    minutes: minutesUntilReady
                )
            };
        }

        /// <summary>Get the machine's processing state.</summary>
        /// <remarks>The coop incubator never produces an object output so it is never done.</remarks>
        public override MachineState GetState()
        {
            return this.Machine.heldObject.Value != null
                ? MachineState.Processing
                : MachineState.Empty;
        }

        /// <summary>Get the output item.</summary>
        /// <remarks>The coop incubator never produces an object output.</remarks>
        public override ITrackedStack GetOutput()
        {
            return null;
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            bool started = this.GenericPullRecipe(input, this.Recipes);
            if (started)
            {
                int eggID = this.Machine.heldObject.Value.ParentSheetIndex;
                this.Machine.ParentSheetIndex = eggID == 180 || eggID == 182 || eggID == 305
                    ? this.Machine.ParentSheetIndex + 2
                    : this.Machine.ParentSheetIndex + 1;
            }
            return started;
        }
    }
}
