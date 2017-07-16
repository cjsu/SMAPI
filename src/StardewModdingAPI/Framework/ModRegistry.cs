using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StardewModdingAPI.Framework
{
    /// <summary>Tracks the installed mods.</summary>
    internal class ModRegistry
    {
        /*********
        ** Properties
        *********/
        /// <summary>The registered mod data.</summary>
        private readonly List<IModMetadata> Mods = new List<IModMetadata>();

        /// <summary>The friendly mod names treated as deprecation warning sources (assembly full name => mod name).</summary>
        private readonly IDictionary<string, string> ModNamesByAssembly = new Dictionary<string, string>();


        /*********
        ** Public methods
        *********/
        /****
        ** Basic metadata
        ****/
        /// <summary>Get metadata for all loaded mods.</summary>
        public IEnumerable<IManifest> GetAll()
        {
            return this.Mods.Select(p => p.Manifest);
        }

        /// <summary>Get metadata for a loaded mod.</summary>
        /// <param name="uniqueID">The mod's unique ID.</param>
        /// <returns>Returns the matching mod's metadata, or <c>null</c> if not found.</returns>
        public IManifest Get(string uniqueID)
        {
            // normalise search ID
            if (string.IsNullOrWhiteSpace(uniqueID))
                return null;
            uniqueID = uniqueID.Trim();

            // find match
            return this.GetAll().FirstOrDefault(p =>
#if SMAPI_1_x
            p.UniqueID != null &&
#endif
            p.UniqueID.Trim().Equals(uniqueID, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>Get whether a mod has been loaded.</summary>
        /// <param name="uniqueID">The mod's unique ID.</param>
        public bool IsLoaded(string uniqueID)
        {
            return this.Get(uniqueID) != null;
        }

        /****
        ** Mod data
        ****/
        /// <summary>Register a mod as a possible source of deprecation warnings.</summary>
        /// <param name="metadata">The mod metadata.</param>
        public void Add(IModMetadata metadata)
        {
            this.Mods.Add(metadata);
            this.ModNamesByAssembly[metadata.Mod.GetType().Assembly.FullName] = metadata.DisplayName;
        }

        /// <summary>Get all enabled mods.</summary>
        public IEnumerable<IModMetadata> GetMods()
        {
            return (from mod in this.Mods select mod);
        }

        /// <summary>Get the friendly mod name which defines a type.</summary>
        /// <param name="type">The type to check.</param>
        /// <returns>Returns the mod name, or <c>null</c> if the type isn't part of a known mod.</returns>
        public string GetModFrom(Type type)
        {
            // null
            if (type == null)
                return null;

            // known type
            string assemblyName = type.Assembly.FullName;
            if (this.ModNamesByAssembly.ContainsKey(assemblyName))
                return this.ModNamesByAssembly[assemblyName];

            // not found
            return null;
        }

        /// <summary>Get the friendly name for the closest assembly registered as a source of deprecation warnings.</summary>
        /// <returns>Returns the source name, or <c>null</c> if no registered assemblies were found.</returns>
        public string GetModFromStack()
        {
            // get stack frames
            StackTrace stack = new StackTrace();
            StackFrame[] frames = stack.GetFrames();
            if (frames == null)
                return null;

            // search stack for a source assembly
            foreach (StackFrame frame in frames)
            {
                MethodBase method = frame.GetMethod();
                string name = this.GetModFrom(method.ReflectedType);
                if (name != null)
                    return name;
            }

            // no known assembly found
            return null;
        }
    }
}
