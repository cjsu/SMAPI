using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Common;
using StardewModdingAPI;
using StardewValley;

namespace Pathoschild.Stardew.Automate.Framework
{
    /// <summary>The API which lets other mods interact with Automate.</summary>
    public class AutomateAPI : IAutomateAPI
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Constructs machine groups.</summary>
        private readonly MachineGroupFactory MachineGroupFactory;

        /// <summary>The active machine groups recognised by Automate.</summary>
        private readonly IDictionary<GameLocation, MachineGroup[]> ActiveMachineGroups;

        /// <summary>The disabled machine groups recognised by Automate (e.g. machines not connected to a chest).</summary>
        private readonly IDictionary<GameLocation, MachineGroup[]> DisabledMachineGroups;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="machineGroupFactory">Constructs machine groups.</param>
        /// <param name="activeMachineGroups">The active machine groups recognised by Automate.</param>
        /// <param name="disabledMachineGroups">The disabled machine groups recognised by Automate (e.g. machines not connected to a chest).</param>
        internal AutomateAPI(IMonitor monitor, MachineGroupFactory machineGroupFactory, IDictionary<GameLocation, MachineGroup[]> activeMachineGroups, IDictionary<GameLocation, MachineGroup[]> disabledMachineGroups)
        {
            this.Monitor = monitor;
            this.MachineGroupFactory = machineGroupFactory;
            this.ActiveMachineGroups = activeMachineGroups;
            this.DisabledMachineGroups = disabledMachineGroups;
        }

        /// <summary>Add an automation factory.</summary>
        /// <param name="factory">An automation factory which construct machines, containers, and connectors.</param>
        public void AddFactory(IAutomationFactory factory)
        {
            this.Monitor.Log($"Adding automation factory: {factory.GetType().AssemblyQualifiedName}", LogLevel.Trace);
            this.MachineGroupFactory.Add(factory);
        }

        /// <summary>Get the status of machines in a tile area. This is a specialised API for Data Layers and similar mods.</summary>
        /// <param name="location">The location for which to display data.</param>
        /// <param name="tileArea">The tile area for which to display data.</param>
        public IDictionary<Vector2, int> GetMachineStates(GameLocation location, Rectangle tileArea)
        {
            IDictionary<Vector2, int> data = new Dictionary<Vector2, int>();
            foreach (IMachine machine in this.GetMachineGroups(location).SelectMany(group => group.Machines))
            {
                if (machine.TileArea.Intersects(tileArea))
                {
                    int state = (int)machine.GetState();
                    foreach (Vector2 tile in machine.TileArea.GetTiles())
                    {
                        if (tileArea.Contains((int)tile.X, (int)tile.Y))
                            data[tile] = state;
                    }
                }
            }

            return data;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get all machines in a location.</summary>
        /// <param name="location">The location whose maches to fetch.</param>
        private IEnumerable<MachineGroup> GetMachineGroups(GameLocation location)
        {
            // active groups
            if (this.ActiveMachineGroups.TryGetValue(location, out MachineGroup[] activeGroups))
            {
                foreach (MachineGroup machineGroup in activeGroups)
                    yield return machineGroup;
            }

            // disabled groups
            if (this.DisabledMachineGroups.TryGetValue(location, out MachineGroup[] disabledGroups))
            {
                foreach (MachineGroup machineGroup in disabledGroups)
                    yield return machineGroup;
            }
        }
    }
}
