using System;

namespace Pathoschild.Stardew.LookupAnything.Framework.Data
{
    /// <summary>The context in which to override an object.</summary>
    [Flags]
    internal enum ObjectContext
    {
        /// <summary>Objects in the world.</summary>
        World = 1,

        /// <summary>Objects in an item inventory.</summary>
        Inventory = 2,

        /// <summary>Objects in any context.</summary>
        Any = ObjectContext.World | ObjectContext.Inventory
    }
}
