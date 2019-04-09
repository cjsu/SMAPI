using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A slime incubator that accepts slime eggs and spawns slime monsters.</summary>
    internal class SlimeIncubatorMachine : GenericObjectMachine<SObject>
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
        public SlimeIncubatorMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile)
        {
            int minutesUntilReady = Game1.player.professions.Contains(2) ? 2000 : 4000;
            this.Recipes = new IRecipe[] {
                // blue slime egg => object with parentSheetIndex of blue slime egg
                new Recipe(
                    input: 413,
                    inputCount: 1,
                    output: input => new SObject(413,1),
                    minutes: minutesUntilReady
                ),

                // red slime egg => object with parentSheetIndex of red slime egg
                new Recipe(
                    input: 437,
                    inputCount: 1,
                    output: input => new SObject(437,1),
                    minutes: minutesUntilReady
                ),

                // purple slime egg => object with parentSheetIndex of purple slime egg
                new Recipe(
                    input: 439,
                    inputCount: 1,
                    output: input => new SObject(439,1),
                    minutes: minutesUntilReady
                ),

                // green slime egg => object with parentSheetIndex of green slime egg
                new Recipe(
                    input: 680,
                    inputCount: 1,
                    output: input => new SObject(680,1),
                    minutes: minutesUntilReady
                )
            };
        }

        /// <summary>Get the machine's processing state.</summary>
        /// <remarks>The slime incubator does not produce an output object, so it is never done.</remarks>
        public override MachineState GetState()
        {
            return this.Machine.heldObject.Value != null
                ? MachineState.Processing
                : MachineState.Empty;
        }

        /// <summary>Get the output item.</summary>
        /// <remarks>The slime incubator does not produce an output object.</remarks>
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
                this.Machine.ParentSheetIndex = 157;
            return started;
        }
    }
}
