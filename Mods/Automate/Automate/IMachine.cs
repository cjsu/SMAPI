namespace Pathoschild.Stardew.Automate
{
    /// <summary>A machine that accepts input and provides output.</summary>
    public interface IMachine : IAutomatable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>A unique ID for the machine type.</summary>
        /// <remarks>This value should be identical for two machines if they have the exact same behavior and input logic. For example, if one machine in a group can't process input due to missing items, Automate will skip any other empty machines of that type in the same group since it assumes they need the same inputs.</remarks>
        string MachineTypeID { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Get the machine's processing state.</summary>
        MachineState GetState();

        /// <summary>Get the output item.</summary>
        ITrackedStack GetOutput();

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        bool SetInput(IStorage input);
    }
}
