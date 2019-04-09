using StardewModdingAPI;

namespace TimeSpeed.Framework
{
    /// <summary>The keyboard bindings used to control the flow of time. See available keys at <a href="https://msdn.microsoft.com/en-us/library/microsoft.xna.framework.input.keys.aspx" />. Set a key to null to disable it.</summary>
    internal class ModControlsConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Freeze or unfreeze time. Freezing time will stay in effect until you unfreeze it; unfreezing time will stay in effect until you enter a new location with time settings.</summary>
        public SButton? FreezeTime { get; set; } = SButton.N;

        /// <summary>Slow down time by one second per 10-game-minutes. Combine with Control to increase by 100 seconds, Shift to increase by 10 seconds, or Alt to increase by 0.1 seconds.</summary>
        public SButton? IncreaseTickInterval { get; set; } = SButton.OemPeriod;

        /// <summary>Speed up time by one second per 10-game-minutes. Combine with Control to decrease by 100 seconds, Shift to decrease by 10 seconds, or Alt to decrease by 0.1 seconds.</summary>
        public SButton? DecreaseTickInterval { get; set; } = SButton.OemComma;

        /// <summary>Reload all values from the config file and apply them immediately. Time will stay frozen if it was frozen via hotkey.</summary>
        public SButton? ReloadConfig { get; set; } = SButton.B;
    }
}
