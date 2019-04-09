using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A preserves jar that accepts input and provides output.</summary>
    /// <remarks>See the game's machine logic in <see cref="SObject.performObjectDropInAction"/> and <see cref="SObject.checkForAction"/>.</remarks>
    internal class PreservesJarMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The recipes to process.</summary>
        private readonly IRecipe[] Recipes =
        {
            // fruit => jelly
            new Recipe(
                input: SObject.FruitsCategory,
                inputCount: 1,
                output: input =>
                {
                    SObject jelly = new SObject(Vector2.Zero, 344, input.Name + " Jelly", false, true, false, false)
                    {
                        Price = 50 + ((SObject) input).Price * 2,
                        name = input.Name + " Jelly"
                    };
                    jelly.preserve.Value = SObject.PreserveType.Jelly;
                    jelly.preservedParentSheetIndex.Value = input.ParentSheetIndex;
                    return jelly;
                },
                minutes: 4000
            ),

            // vegetable => pickled vegetable
            new Recipe(
                input: SObject.VegetableCategory,
                inputCount: 1,
                output: input =>
                {
                    SObject item = new SObject(Vector2.Zero, 342, "Pickled " + input.Name, false, true, false, false)
                    {
                        Price = 50 + ((SObject) input).Price * 2,
                        name = "Pickled " + input.Name
                    };
                    item.preserve.Value = SObject.PreserveType.Pickle;
                    item.preservedParentSheetIndex.Value = input.ParentSheetIndex;
                    return item;

                },
                minutes: 4000
            )
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public PreservesJarMachine(SObject machine, GameLocation location, Vector2 tile)
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
