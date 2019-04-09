using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>An oil maker that accepts input and provides output.</summary>
    internal class OilMakerMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The recipes to process.</summary>
        private readonly IRecipe[] Recipes =
        {
            // truffle => truffle oil
            new Recipe(
                input: 430,
                inputCount: 1,
                output: input => new SObject(Vector2.Zero, 432, null, false, true, false, false),
                minutes: 360
            ),

            // sunflower seed => oil
            new Recipe(
                input: 431,
                inputCount: 1,
                output: input => new SObject(247, 1),
                minutes: 3200
            ),

            // corn => oil
            new Recipe(
                input: 270,
                inputCount: 1,
                output: input => new SObject(Vector2.Zero, 247, null, false, true, false, false),
                minutes: 1000
            ), 

            // sunflower => oil
            new Recipe(
                input: 421,
                inputCount: 1,
                output: input => new SObject(Vector2.Zero, 247, null, false, true, false, false),
                minutes: 60
            )
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public OilMakerMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            return this.GenericPullRecipe(input, this.Recipes);
        }
    }
}
