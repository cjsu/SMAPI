using StardewModdingAPI.Framework.Reflection;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewValley;

namespace StardewModdingAPI.Framework
{
    /// <summary>The static state to use while <see cref="Game1"/> is initialising, which happens before the <see cref="SGame"/> constructor runs.</summary>
    internal class SGameConstructorHack
    {
        /*********
        ** Accessors
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        public IMonitor Monitor { get; }

        /// <summary>Simplifies access to private game code.</summary>
        public Reflector Reflection { get; }

        /// <summary>Encapsulates SMAPI's JSON file parsing.</summary>
        public JsonHelper JsonHelper { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="jsonHelper">Encapsulates SMAPI's JSON file parsing.</param>
        public SGameConstructorHack(IMonitor monitor, Reflector reflection, JsonHelper jsonHelper)
        {
            this.Monitor = monitor;
            this.Reflection = reflection;
            this.JsonHelper = jsonHelper;
        }
    }
}
