using Microsoft.Xna.Framework;
using StardewValley;

namespace Pathoschild.Stardew.Automate
{
    /// <summary>An automatable entity, which can implement a more specific type like <see cref="IMachine"/> or <see cref="IContainer"/>. If it doesn't implement a more specific type, it's treated as a connector with no additional logic.</summary>
    public interface IAutomatable
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The location which contains the machine.</summary>
        GameLocation Location { get; }

        /// <summary>The tile area covered by the machine.</summary>
        Rectangle TileArea { get; }
    }
}
