using Microsoft.Xna.Framework;
using StardewValley;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Automate.Framework
{
    /// <summary>A generic machine instance.</summary>
    internal abstract class GenericObjectMachine<TMachine> : BaseMachine<TMachine> where TMachine : SObject
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get the machine's processing state.</summary>
        public override MachineState GetState()
        {
            if (this.Machine.heldObject.Value == null)
                return MachineState.Empty;

            return this.Machine.readyForHarvest.Value
                ? MachineState.Done
                : MachineState.Processing;
        }

        /// <summary>Get the output item.</summary>
        public override ITrackedStack GetOutput()
        {
            return new TrackedItem(this.Machine.heldObject.Value, onEmpty: this.GenericReset);
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="machine">The underlying machine.</param>
        /// <param name="location">The in-game location.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        protected GenericObjectMachine(TMachine machine, GameLocation location, Vector2 tile)
            : base(machine, location, BaseMachine.GetTileAreaFor(tile)) { }

        /// <summary>Reset the machine so it's ready to accept a new input.</summary>
        /// <param name="item">The output item that was taken.</param>
        protected void GenericReset(Item item)
        {
            this.Machine.heldObject.Value = null;
            this.Machine.readyForHarvest.Value = false;
        }

        /// <summary>Generic logic to pull items from storage based on the given recipes.</summary>
        /// <param name="storage">The available items.</param>
        /// <param name="recipes">The recipes to match.</param>
        protected bool GenericPullRecipe(IStorage storage, IRecipe[] recipes)
        {
            if (storage.TryGetIngredient(recipes, out IConsumable consumable, out IRecipe recipe))
            {
                this.Machine.heldObject.Value = recipe.Output(consumable.Take());
                this.Machine.MinutesUntilReady = recipe.Minutes;
                return true;
            }
            return false;
        }
    }
}
