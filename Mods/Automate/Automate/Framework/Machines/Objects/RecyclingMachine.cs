using System;
using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A recycling maching that accepts input and provides output.</summary>
    /// <remarks>This differs slightly from the game implementation in that it uses a more random RNG, due to a C# limitation which prevents us from accessing machine info from the cached recipe output functions for use in the RNG seed.</remarks>
    internal class RecyclingMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The RNG to use for randomising output.</summary>
        private static readonly Random Random = new Random();

        /// <summary>The recipes to process.</summary>
        private readonly IRecipe[] Recipes =
        {
            // trash => coal/iron ore/stone
            new Recipe(
                input: 168,
                inputCount: 1,
                output: input => new SObject(RecyclingMachine.Random.NextDouble() < 0.3 ? 382 : (RecyclingMachine.Random.NextDouble() < 0.3 ? 380 : 390), RecyclingMachine.Random.Next(1, 4)),
                minutes: 60
            ),

            // driftwood => coal/wood
            new Recipe(
                input: 169,
                inputCount: 1,
                output: input => new SObject(RecyclingMachine.Random.NextDouble() < 0.25 ? 382 : 388, RecyclingMachine.Random.Next(1, 4)),
                minutes: 60
            ),

            // broken glasses or broken CD => refined quartz
            new Recipe(
                input: 170,
                inputCount: 1,
                output: input => new SObject(338, 1),
                minutes: 60
            ),
            new Recipe(
                input: 171,
                inputCount: 1,
                output: input => new SObject(338, 1),
                minutes: 60
            ),

            // soggy newspaper => cloth/torch
            new Recipe(
                input: 172,
                inputCount: 1,
                output: input => RecyclingMachine.Random.NextDouble() < 0.1 ? new SObject(428, 1) : new Torch(Vector2.Zero, 3),
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
        public RecyclingMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            //Random random = new Random((int)Game1.uniqueIDForThisGame / 2 + (int)Game1.stats.DaysPlayed + Game1.timeOfDay + (int)machine.tileLocation.X * 200 + (int)machine.tileLocation.Y);

            if (this.GenericPullRecipe(input, this.Recipes))
            {
                Game1.stats.PiecesOfTrashRecycled += 1;
                return true;
            }
            return false;
        }
    }
}
