using System;
using System.Linq;
using StardewModdingAPI.Utilities;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Common.DataParsers
{
    /// <summary>Analyses crop data for a tile.</summary>
    internal class CropDataParser
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The crop.</summary>
        public Crop Crop { get; }

        /// <summary>The seasons in which the crop grows.</summary>
        public string[] Seasons { get; }

        /// <summary>The phase index in <see cref="StardewValley.Crop.phaseDays"/> when the crop can be harvested.</summary>
        public int HarvestablePhase { get; }

        /// <summary>The number of days needed between planting and first harvest.</summary>
        public int DaysToFirstHarvest { get; }

        /// <summary>The number of days needed between harvests, after the first harvest.</summary>
        public int DaysToSubsequentHarvest { get; }

        /// <summary>Whether the crop can be harvested multiple times.</summary>
        public bool HasMultipleHarvests { get; }

        /// <summary>Whether the crop is ready to harvest now.</summary>
        public bool CanHarvestNow { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="crop">The crop.</param>
        public CropDataParser(Crop crop)
        {
            this.Crop = crop;
            if (crop != null)
            {
                this.Seasons = crop.seasonsToGrowIn.ToArray();
                this.HasMultipleHarvests = crop.regrowAfterHarvest.Value == -1;
                this.HarvestablePhase = crop.phaseDays.Count - 1;
                this.CanHarvestNow = (crop.currentPhase.Value >= this.HarvestablePhase) && (!crop.fullyGrown.Value || crop.dayOfCurrentPhase.Value <= 0);
                this.DaysToFirstHarvest = crop.phaseDays.Take(crop.phaseDays.Count - 1).Sum(); // ignore harvestable phase
                this.DaysToSubsequentHarvest = crop.regrowAfterHarvest.Value;
            }
        }

        /// <summary>Get the date when the crop will next be ready to harvest.</summary>
        public SDate GetNextHarvest()
        {
            // get crop
            Crop crop = this.Crop;
            if (crop == null)
                throw new InvalidOperationException("Can't get the harvest date because there's no crop.");

            // ready now
            if (this.CanHarvestNow)
                return SDate.Now();

            // growing: days until next harvest
            if (!crop.fullyGrown.Value)
            {
                int daysUntilLastPhase = this.DaysToFirstHarvest - this.Crop.dayOfCurrentPhase.Value - crop.phaseDays.Take(crop.currentPhase.Value).Sum();
                return SDate.Now().AddDays(daysUntilLastPhase);
            }

            // regrowable crop harvested today
            if (crop.dayOfCurrentPhase.Value >= crop.regrowAfterHarvest.Value)
                return SDate.Now().AddDays(crop.regrowAfterHarvest.Value);

            // regrowable crop
            // dayOfCurrentPhase decreases to 0 when fully grown, where <=0 is harvestable
            return SDate.Now().AddDays(crop.dayOfCurrentPhase.Value);
        }

        /// <summary>Get a sample item acquired by harvesting the crop.</summary>
        public Item GetSampleDrop()
        {
            if (this.Crop == null)
                throw new InvalidOperationException("Can't get a sample drop because there's no crop.");

            return new SObject(this.Crop.indexOfHarvest.Value, 1);
        }
    }
}
