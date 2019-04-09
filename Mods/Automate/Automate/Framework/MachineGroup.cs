using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;

namespace Pathoschild.Stardew.Automate.Framework
{
    /// <summary>A collection of machines and storage which work as one unit.</summary>
    internal class MachineGroup
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The location containing the group.</summary>
        public GameLocation Location { get; }

        /// <summary>The machines in the group.</summary>
        public IMachine[] Machines { get; }

        /// <summary>The containers in the group.</summary>
        public IContainer[] Containers { get; }

        /// <summary>The storage manager for the group.</summary>
        public IStorage StorageManager { get; }

        /// <summary>The tiles comprising the group.</summary>
        public Vector2[] Tiles { get; }

        /// <summary>Whether the group has the minimum requirements to enable internal automation (i.e., at least one chest and one machine).</summary>
        public bool HasInternalAutomation => this.Machines.Length > 0 && this.Containers.Length > 0;


        /*********
        ** Public methods
        *********/
        /// <summary>Create an instance.</summary>
        /// <param name="location">The location containing the group.</param>
        /// <param name="machines">The machines in the group.</param>
        /// <param name="containers">The containers in the group.</param>
        /// <param name="tiles">The tiles comprising the group.</param>
        public MachineGroup(GameLocation location, IMachine[] machines, IContainer[] containers, Vector2[] tiles)
        {
            this.Location = location;
            this.Machines = machines;
            this.Containers = containers;
            this.Tiles = tiles;
            this.StorageManager = new StorageManager(containers);
        }

        /// <summary>Automate the machines inside the group.</summary>
        public void Automate()
        {
            // get machines ready for input/output
            IList<IMachine> outputReady = new List<IMachine>();
            IList<IMachine> inputReady = new List<IMachine>();
            foreach (IMachine machine in this.Machines)
            {
                switch (machine.GetState())
                {
                    case MachineState.Done:
                        outputReady.Add(machine);
                        break;

                    case MachineState.Empty:
                        inputReady.Add(machine);
                        break;
                }
            }
            if (!outputReady.Any() && !inputReady.Any())
                return;

            // process output
            foreach (IMachine machine in outputReady)
            {
                if (this.StorageManager.TryPush(machine.GetOutput()) && machine.GetState() == MachineState.Empty)
                    inputReady.Add(machine);
            }

            // process input
            HashSet<string> ignoreMachines = new HashSet<string>();
            foreach (IMachine machine in inputReady)
            {
                if (ignoreMachines.Contains(machine.MachineTypeID))
                    continue;

                if (!machine.SetInput(this.StorageManager))
                    ignoreMachines.Add(machine.MachineTypeID); // if the machine can't process available input, no need to ask every instance of its type
            }
        }
    }
}
