using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A seed maker that accepts input and provides output.</summary>
    internal class SeedMakerMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>A crop ID => seed ID lookup.</summary>
        private static readonly IDictionary<int, int> CropSeedIDs = SeedMakerMachine.GetCropSeedIDs();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public SeedMakerMachine(SObject machine, GameLocation location, Vector2 tile)
            : base(machine, location, tile) { }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            SObject machine = this.Machine;

            // crop => seeds
            if (input.TryGetIngredient(this.IsValidCrop, 1, out IConsumable crop))
            {
                crop.Reduce();
                int seedID = SeedMakerMachine.CropSeedIDs[crop.Sample.ParentSheetIndex];

                Random random = new Random((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2 + (int)machine.TileLocation.X + (int)machine.TileLocation.Y * 77 + Game1.timeOfDay);
                machine.heldObject.Value = new SObject(seedID, random.Next(1, 4));
                if (random.NextDouble() < 0.005)
                    machine.heldObject.Value = new SObject(499, 1);
                else if (random.NextDouble() < 0.02)
                    machine.heldObject.Value = new SObject(770, random.Next(1, 5));
                machine.MinutesUntilReady = 20;
                return true;
            }

            return false;
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether a given item is a crop compatible with the seed marker.</summary>
        /// <param name="item">The item to check.</param>
        public bool IsValidCrop(ITrackedStack item)
        {
            return
                item.Sample.ParentSheetIndex != 433 // seed maker doesn't allow coffee beans
                && SeedMakerMachine.CropSeedIDs.ContainsKey(item.Sample.ParentSheetIndex);
        }

        /// <summary>Get a crop ID => seed ID lookup.</summary>
        public static IDictionary<int, int> GetCropSeedIDs()
        {
            IDictionary<int, int> lookup = new Dictionary<int, int>();

            IDictionary<int, string> cropData = Game1.content.Load<Dictionary<int, string>>("Data\\Crops");
            foreach (KeyValuePair<int, string> entry in cropData)
            {
                int dataCropID = Convert.ToInt32(entry.Value.Split('/')[3]);
                int dataSeedID = entry.Key;
                if (!lookup.ContainsKey(dataCropID)) // if multiple crops have the same seed, use the earliest one (which is more likely the vanilla seed)
                    lookup[dataCropID] = dataSeedID;
            }

            return lookup;
        }
    }
}
