using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Buildings
{
    /// <summary>A shipping bin that accepts input and provides output.</summary>
    internal class ShippingBinMachine : BaseMachine
    {
        /*********
        ** Fields
        *********/
        /// <summary>The farm to automate.</summary>
        private readonly Farm Farm;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="farm">The farm containing the shipping bin.</param>
        /// <param name="tileArea">The tile area covered by the machine.</param>
        public ShippingBinMachine(Farm farm, Rectangle tileArea)
            : base(farm, tileArea)
        {
            this.Farm = farm;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="bin">The constructed shipping bin.</param>
        /// <param name="location">The location which contains the machine.</param>
        /// <param name="farm">The farm which has the shipping bin data.</param>
        public ShippingBinMachine(ShippingBin bin, GameLocation location, Farm farm)
            : base(location, BaseMachine.GetTileAreaFor(bin))
        {
            this.Farm = farm;
        }

        /// <summary>Get the machine's processing state.</summary>
        public override MachineState GetState()
        {
            return MachineState.Empty; // always accepts items
        }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            return null; // no output
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            ITrackedStack tracker = input.GetItems().Where(p => p.Sample is SObject obj && obj.canBeShipped()).Take(1).FirstOrDefault();
            if (tracker != null)
            {
                SObject item = (SObject)tracker.Take(tracker.Count);
                this.Farm.shippingBin.Add(item);
                this.Farm.lastItemShipped = item;
                this.Farm.showShipment(item, false);
                return true;
            }
            return false;
        }
    }
}
