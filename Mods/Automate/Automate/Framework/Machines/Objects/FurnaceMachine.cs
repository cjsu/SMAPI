using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A furnace that accepts input and provides output.</summary>
    internal class FurnaceMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The recipes to process.</summary>
        /// <remarks>Derived from <see cref="SObject.performObjectDropInAction"/>.</remarks>
        private readonly IRecipe[] Recipes =
        {
            // copper => copper bar
            new Recipe(
                input: SObject.copper,
                inputCount: 5,
                output: input => new SObject(Vector2.Zero, SObject.copperBar, 1),
                minutes: 30
            ),

            // iron => iron bar
            new Recipe(
                input: SObject.iron,
                inputCount: 5,
                output: input => new SObject(Vector2.Zero, SObject.ironBar, 1),
                minutes: 120
            ),

            // gold => gold bar
            new Recipe(
                input: SObject.gold,
                inputCount: 5,
                output: input => new SObject(Vector2.Zero, SObject.goldBar, 1),
                minutes: 300
            ),

            // iridium => iridium bar
            new Recipe(
                input: SObject.iridium,
                inputCount: 5,
                output: input => new SObject(Vector2.Zero, SObject.iridiumBar, 1),
                minutes: 480
            ),

            // quartz => refined quartz
            new Recipe(
                input: SObject.quartzIndex,
                inputCount: 1,
                output: input => new SObject(Vector2.Zero, 338, 1),
                minutes: 90
            ),

            // refined quartz => refined quartz
            new Recipe(
                input: 82,
                inputCount: 1,
                output: input => new SObject(Vector2.Zero, 338, 3),
                minutes: 90
            )
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public FurnaceMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            if (input.TryGetIngredient(SObject.coal, 1, out IConsumable coal) && this.GenericPullRecipe(input, this.Recipes))
            {
                coal.Reduce();
                this.Machine.initializeLightSource(this.Machine.TileLocation);
                this.Machine.showNextIndex.Value = true;
                return true;
            }
            return false;
        }
    }
}
