using System.IO;
using Mono.Cecil;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>Metadata about a parsed assembly definition.</summary>
    internal class AssemblyParseResult
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The original assembly file.</summary>
        public readonly FileInfo File;

        /// <summary>The assembly definition.</summary>
        public readonly AssemblyDefinition Definition;

        /// <summary>The result of the assembly load.</summary>
        public AssemblyLoadStatus Status;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="file">The original assembly file.</param>
        /// <param name="assembly">The assembly definition.</param>
        /// <param name="status">The result of the assembly load.</param>
        public AssemblyParseResult(FileInfo file, AssemblyDefinition assembly, AssemblyLoadStatus status)
        {
            this.File = file;
            this.Definition = assembly;
            this.Status = status;
        }
    }
}
