namespace Pathoschild.Stardew.Automate
{
    /// <summary>A machine processing state.</summary>
    public enum MachineState
    {
        /// <summary>The machine is not currently enabled (e.g. out of season or needs to be started manually).</summary>
        Disabled,

        /// <summary>The machine has no input.</summary>
        Empty,

        /// <summary>The machine is processing an input.</summary>
        Processing,

        /// <summary>The machine finished processing an input and has an output item ready.</summary>
        Done
    }
}
