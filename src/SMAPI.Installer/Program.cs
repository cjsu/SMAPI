using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace StardewModdingApi.Installer
{
    /// <summary>The entry point for SMAPI's install and uninstall console app.</summary>
    internal class Program
    {
        /*********
        ** Fields
        *********/
        /// <summary>The absolute path of the installer folder.</summary>
        [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute", Justification = "The assembly location is never null in this context.")]
        private static readonly string InstallerPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        /// <summary>The absolute path of the folder containing the unzipped installer files.</summary>
        private static readonly string ExtractedBundlePath = Path.Combine(Path.GetTempPath(), $"SMAPI-installer-{Guid.NewGuid():N}");

        /// <summary>The absolute path for referenced assemblies.</summary>
        private static readonly string InternalFilesPath = Path.Combine(Program.ExtractedBundlePath, "smapi-internal");

        /*********
        ** Public methods
        *********/
        /// <summary>Run the install or uninstall script.</summary>
        /// <param name="args">The command line arguments.</param>
        public static void Main(string[] args)
        {
            // find install bundle
            PlatformID platform = Environment.OSVersion.Platform;
            FileInfo zipFile = new FileInfo(Path.Combine(Program.InstallerPath, $"{(platform == PlatformID.Win32NT ? "windows" : "unix")}-install.dat"));
            if (!zipFile.Exists)
            {
                Console.WriteLine($"Oops! Some of the installer files are missing; try re-downloading the installer. (Missing file: {zipFile.FullName})");
                Console.ReadLine();
                return;
            }

            // unzip bundle into temp folder
            DirectoryInfo bundleDir = new DirectoryInfo(Program.ExtractedBundlePath);
            Console.WriteLine("Extracting install files...");
            ZipFile.ExtractToDirectory(zipFile.FullName, bundleDir.FullName);

            // set up assembly resolution
            AppDomain.CurrentDomain.AssemblyResolve += Program.CurrentDomain_AssemblyResolve;

            // launch installer
            var installer = new InteractiveInstaller(bundleDir.FullName);
            installer.Run(args);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Method called when assembly resolution fails, which may return a manually resolved assembly.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs e)
        {
            try
            {
                AssemblyName name = new AssemblyName(e.Name);
                foreach (FileInfo dll in new DirectoryInfo(Program.InternalFilesPath).EnumerateFiles("*.dll"))
                {
                    if (name.Name.Equals(AssemblyName.GetAssemblyName(dll.FullName).Name, StringComparison.OrdinalIgnoreCase))
                        return Assembly.LoadFrom(dll.FullName);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resolving assembly: {ex}");
                return null;
            }
        }
    }
}
