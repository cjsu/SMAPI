using System;
using System.IO;
using System.Text;

namespace StardewModdingAPI.Framework.Logging
{
    /// <summary>A text writer which allows intercepting output.</summary>
    internal class InterceptingTextWriter : TextWriter
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The underlying console output.</summary>
        public TextWriter Out { get; }

        /// <summary>The character encoding in which the output is written.</summary>
        public override Encoding Encoding => this.Out.Encoding;

        /// <summary>Whether to intercept console output.</summary>
        public bool ShouldIntercept { get; set; }

        /// <summary>The event raised when a message is written to the console directly.</summary>
        public event Action<string> OnMessageIntercepted;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="output">The underlying output writer.</param>
        public InterceptingTextWriter(TextWriter output)
        {
            this.Out = output;
        }

        /// <summary>Writes a subarray of characters to the text string or stream.</summary>
        /// <param name="buffer">The character array to write data from.</param>
        /// <param name="index">The character position in the buffer at which to start retrieving data.</param>
        /// <param name="count">The number of characters to write.</param>
        public override void Write(char[] buffer, int index, int count)
        {
            if (this.ShouldIntercept)
                this.OnMessageIntercepted?.Invoke(new string(buffer, index, count).TrimEnd('\r', '\n'));
            else
                this.Out.Write(buffer, index, count);
        }

        /// <summary>Writes a character to the text string or stream.</summary>
        /// <param name="ch">The character to write to the text stream.</param>
        /// <remarks>Console log messages from the game should be caught by <see cref="Write(char[],int,int)"/>. This method passes through anything that bypasses that method for some reason, since it's better to show it to users than hide it from everyone.</remarks>
        public override void Write(char ch)
        {
            this.Out.Write(ch);
        }

        /// <summary>Releases the unmanaged resources used by the <see cref="T:System.IO.TextWriter" /> and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            this.OnMessageIntercepted = null;
        }
    }
}
