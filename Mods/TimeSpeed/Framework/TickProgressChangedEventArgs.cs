using System;

namespace TimeSpeed.Framework
{
    /// <summary>Contains information about a change to the <see cref="TimeHelper.TickProgress"/> value.</summary>
    internal class TickProgressChangedEventArgs : EventArgs
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The previous progress value.</summary>
        public double PreviousProgress { get; }

        /// <summary>The new progress value.</summary>
        public double NewProgress { get; }

        /// <summary>Whether a new tick occurred since the last check.</summary>
        public bool TimeChanged => this.NewProgress < this.PreviousProgress;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="previousProgess">The previous progress value.</param>
        /// <param name="newProgress">The new progress value.</param>
        public TickProgressChangedEventArgs(double previousProgess, double newProgress)
        {
            this.PreviousProgress = previousProgess;
            this.NewProgress = newProgress;
        }
    }
}
