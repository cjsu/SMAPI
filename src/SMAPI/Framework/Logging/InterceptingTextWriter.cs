using System;
using System.IO;
using System.Text;

namespace StardewModdingAPI.Framework.Logging
{
    /// <summary>A text writer which allows intercepting output.</summary>
    internal class InterceptingTextWriter : TextWriter
    {
        /*********
        ** Fields
        *********/
        /// <summary>Prefixing a message with this character indicates that the console interceptor should write the string without intercepting it. (The character itself is not written.)</summary>
        private readonly char IgnoreChar;


        /*********
        ** Accessors
        *********/
        /// <summary>The underlying console output.</summary>
        public TextWriter Out { get; }

        /// <inheritdoc />
        public override Encoding Encoding => this.Out.Encoding;

        /// <summary>The event raised when a message is written to the console directly.</summary>
        public event Action<string> OnMessageIntercepted;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="output">The underlying output writer.</param>
        /// <param name="ignoreChar">Prefixing a message with this character indicates that the console interceptor should write the string without intercepting it. (The character itself is not written.)</param>
        public InterceptingTextWriter(TextWriter output, char ignoreChar)
        {
            this.Out = output;
            this.IgnoreChar = ignoreChar;
        }

        /// <inheritdoc />
        public override void Write(char[] buffer, int index, int count)
        {
            if (buffer.Length == 0)
                this.Out.Write(buffer, index, count);
            else if (buffer[0] == this.IgnoreChar)
                this.Out.Write(buffer, index + 1, count - 1);
            else if (this.IsEmptyOrNewline(buffer))
                this.Out.Write(buffer, index, count);
            else
                this.OnMessageIntercepted?.Invoke(new string(buffer, index, count).TrimEnd('\r', '\n'));
        }

        /// <inheritdoc />
        public override void Write(char ch)
        {
            this.Out.Write(ch);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            this.OnMessageIntercepted = null;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get whether a buffer represents a line break.</summary>
        /// <param name="buffer">The buffer to check.</param>
        private bool IsEmptyOrNewline(char[] buffer)
        {
            foreach (char ch in buffer)
            {
                if (ch != '\n' && ch != '\r')
                    return false;
            }

            return true;
        }
    }
}
