using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework.Machines.Objects
{
    /// <summary>A crystalarium that accepts input and provides output.</summary>
    internal class CrystalariumMachine : GenericObjectMachine<SObject>
    {
        /*********
        ** Fields
        *********/
        /// <summary>Simplifies access to private game code.</summary>
        private readonly IReflectionHelper Reflection;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The location containing the machine.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public CrystalariumMachine(SObject machine, GameLocation location, Vector2 tile, IReflectionHelper reflection)
            : base(machine, location, tile)
        {
            this.Reflection = reflection;
        }

        /// <summary>Get the machine's processing state.</summary>
        public override MachineState GetState()
        {
            if (this.Machine.heldObject.Value == null)
                return MachineState.Disabled;

            return this.Machine.readyForHarvest.Value
                ? MachineState.Done
                : MachineState.Processing;
        }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            SObject machine = this.Machine;
            SObject heldObject = machine.heldObject.Value;
            return new TrackedItem(heldObject.getOne(), item =>
            {
                machine.MinutesUntilReady = this.Reflection.GetMethod(machine, "getMinutesForCrystalarium").Invoke<int>(heldObject.ParentSheetIndex);
                machine.readyForHarvest.Value = false;
            });
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public override bool SetInput(IStorage input)
        {
            return false; // started manually
        }
    }
}
