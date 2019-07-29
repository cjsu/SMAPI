using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using StardewValley;

namespace StardewModdingAPI.Mods.SaveBackup
{
    /// <summary>The main entry point for the mod.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /// <summary>The number of backups to keep.</summary>
        private readonly int BackupsToKeep = 10;

        /// <summary>The absolute path to the folder in which to store save backups.</summary>
        private readonly string BackupFolder = Path.Combine(Constants.ExecutionPath, "save-backups");

        /// <summary>A unique label for the save backup to create.</summary>
        private readonly string BackupLabel = $"{DateTime.UtcNow:yyyy-MM-dd} - SMAPI {Constants.ApiVersion} with Stardew Valley {Game1.version}";

        /// <summary>The name of the save archive to create.</summary>
        private string FileName => $"{this.BackupLabel}.zip";


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            try
            {
                // init backup folder
                DirectoryInfo backupFolder = new DirectoryInfo(this.BackupFolder);
                backupFolder.Create();

                // back up & prune saves
                Task
                    .Run(() => this.CreateBackup(backupFolder))
                    .ContinueWith(backupTask => this.PruneBackups(backupFolder, this.BackupsToKeep));
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Error backing up saves: {ex}", LogLevel.Error);
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Back up the current saves.</summary>
        /// <param name="backupFolder">The folder containing save backups.</param>
        private void CreateBackup(DirectoryInfo backupFolder)
        {
            try
            {
                // get target path
                FileInfo targetFile = new FileInfo(Path.Combine(backupFolder.FullName, this.FileName));
                DirectoryInfo fallbackDir = new DirectoryInfo(Path.Combine(backupFolder.FullName, this.BackupLabel));
                if (targetFile.Exists || fallbackDir.Exists)
                    return;

                // create zip
                // due to limitations with the bundled Mono on Mac, we can't reference System.IO.Compression.
                this.Monitor.Log($"Backing up saves to {targetFile.FullName}...", LogLevel.Trace);
                switch (Constants.TargetPlatform)
                {
                    case GamePlatform.Linux:
                    case GamePlatform.Windows:
                        {
                            try
                            {
                                // create compressed backup
                                Assembly coreAssembly = Assembly.Load("System.IO.Compression, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") ?? throw new InvalidOperationException("Can't load System.IO.Compression assembly.");
                                Assembly fsAssembly = Assembly.Load("System.IO.Compression.FileSystem, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") ?? throw new InvalidOperationException("Can't load System.IO.Compression assembly.");
                                Type compressionLevelType = coreAssembly.GetType("System.IO.Compression.CompressionLevel") ?? throw new InvalidOperationException("Can't load CompressionLevel type.");
                                Type zipFileType = fsAssembly.GetType("System.IO.Compression.ZipFile") ?? throw new InvalidOperationException("Can't load ZipFile type.");
                                MethodInfo createMethod = zipFileType.GetMethod("CreateFromDirectory", new[] { typeof(string), typeof(string), compressionLevelType, typeof(bool) }) ?? throw new InvalidOperationException("Can't load ZipFile.CreateFromDirectory method.");
                                createMethod.Invoke(null, new object[] { Constants.SavesPath, targetFile.FullName, CompressionLevel.Fastest, false });
                            }
                            catch (Exception ex) when (ex is TypeLoadException || ex.InnerException is TypeLoadException)
                            {
                                // create uncompressed backup if compression fails
                                this.Monitor.Log("Couldn't zip the save backup, creating uncompressed backup instead.", LogLevel.Debug);
                                this.Monitor.Log(ex.ToString(), LogLevel.Trace);
                                this.RecursiveCopy(new DirectoryInfo(Constants.SavesPath), fallbackDir, copyRoot: false);
                            }
                        }
                        break;

                    case GamePlatform.Mac:
                        {
                            DirectoryInfo saveFolder = new DirectoryInfo(Constants.SavesPath);
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = "zip",
                                Arguments = $"-rq \"{targetFile.FullName}\" \"{saveFolder.Name}\" -x \"*.DS_Store\" -x \"__MACOSX\"",
                                WorkingDirectory = $"{Constants.SavesPath}/../",
                                CreateNoWindow = true
                            };
                            new Process { StartInfo = startInfo }.Start();
                        }
                        break;
                }
                this.Monitor.Log("Backup done!", LogLevel.Trace);
            }
            catch (Exception ex)
            {
                this.Monitor.Log("Couldn't back up save files (see log file for details).", LogLevel.Warn);
                this.Monitor.Log(ex.ToString(), LogLevel.Trace);
            }
        }

        /// <summary>Remove old backups if we've exceeded the limit.</summary>
        /// <param name="backupFolder">The folder containing save backups.</param>
        /// <param name="backupsToKeep">The number of backups to keep.</param>
        private void PruneBackups(DirectoryInfo backupFolder, int backupsToKeep)
        {
            try
            {
                var oldBackups = backupFolder
                    .GetFileSystemInfos()
                    .OrderByDescending(p => p.CreationTimeUtc)
                    .Skip(backupsToKeep);

                foreach (FileSystemInfo entry in oldBackups)
                {
                    try
                    {
                        this.Monitor.Log($"Deleting {entry.Name}...", LogLevel.Trace);
                        if (entry is DirectoryInfo folder)
                            folder.Delete(recursive: true);
                        else
                            entry.Delete();
                    }
                    catch (Exception ex)
                    {
                        this.Monitor.Log($"Error deleting old save backup '{entry.Name}': {ex}", LogLevel.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log("Couldn't remove old backups (see log file for details).", LogLevel.Warn);
                this.Monitor.Log(ex.ToString(), LogLevel.Trace);
            }
        }

        /// <summary>Recursively copy a directory or file.</summary>
        /// <param name="source">The file or folder to copy.</param>
        /// <param name="targetFolder">The folder to copy into.</param>
        /// <param name="copyRoot">Whether to copy the root folder itself, or <c>false</c> to only copy its contents.</param>
        /// <remarks>Derived from the SMAPI installer code.</remarks>
        private void RecursiveCopy(FileSystemInfo source, DirectoryInfo targetFolder, bool copyRoot = true)
        {
            if (!targetFolder.Exists)
                targetFolder.Create();

            switch (source)
            {
                case FileInfo sourceFile:
                    sourceFile.CopyTo(Path.Combine(targetFolder.FullName, sourceFile.Name));
                    break;

                case DirectoryInfo sourceDir:
                    DirectoryInfo targetSubfolder = copyRoot ? new DirectoryInfo(Path.Combine(targetFolder.FullName, sourceDir.Name)) : targetFolder;
                    foreach (var entry in sourceDir.EnumerateFileSystemInfos())
                        this.RecursiveCopy(entry, targetSubfolder);
                    break;

                default:
                    throw new NotSupportedException($"Unknown filesystem info type '{source.GetType().FullName}'.");
            }
        }
    }
}
