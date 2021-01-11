using System.IO;
using System.Threading;

namespace StardewModdingAPI.Toolkit.Utilities
{
    /// <summary>Provides utilities for dealing with files.</summary>
    public static class FileUtilities
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Delete a file or folder regardless of file permissions, and block until deletion completes.</summary>
        /// <param name="entry">The file or folder to reset.</param>
        public static void ForceDelete(FileSystemInfo entry)
        {
            // ignore if already deleted
            entry.Refresh();
            if (!entry.Exists)
                return;

            // delete children
            if (entry is DirectoryInfo folder)
            {
                foreach (FileSystemInfo child in folder.GetFileSystemInfos())
                    FileUtilities.ForceDelete(child);
            }

            // reset permissions & delete
            entry.Attributes = FileAttributes.Normal;
            entry.Delete();

            // wait for deletion to finish
            for (int i = 0; i < 10; i++)
            {
                entry.Refresh();
                if (entry.Exists)
                    Thread.Sleep(500);
            }

            // throw exception if deletion didn't happen before timeout
            entry.Refresh();
            if (entry.Exists)
                throw new IOException($"Timed out trying to delete {entry.FullName}");
        }
    }
}
