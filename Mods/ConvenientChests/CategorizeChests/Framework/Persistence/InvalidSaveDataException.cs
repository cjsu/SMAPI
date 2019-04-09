using System;

namespace ConvenientChests.CategorizeChests.Framework.Persistence
{
    /// <summary>
    /// An exception to be raised when save data is malformed or fails to
    /// correspond to the state of the game world.
    /// </summary>
    class InvalidSaveDataException : Exception
    {
        public InvalidSaveDataException(string message) : base(message) {}
        public InvalidSaveDataException(string message, Exception inner) : base(message, inner) {}
    }
}