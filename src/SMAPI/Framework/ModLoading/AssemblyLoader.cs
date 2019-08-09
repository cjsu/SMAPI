using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.Exceptions;
using StardewModdingAPI.Metadata;
using StardewModdingAPI.Toolkit.Framework.ModData;
using StardewModdingAPI.Toolkit.Utilities;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>Preprocesses and loads mod assemblies.</summary>
    internal class AssemblyLoader : IDisposable
    {
        /*********
        ** Fields
        *********/
        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;

        /// <summary>Whether to detect paranoid mode issues.</summary>
        private readonly bool ParanoidMode;

        /// <summary>Metadata for mapping assemblies to the current platform.</summary>
        private readonly PlatformAssemblyMap AssemblyMap;

        /// <summary>A type => assembly lookup for types which should be rewritten.</summary>
        private readonly IDictionary<string, Assembly> TypeAssemblies;

        /// <summary>A minimal assembly definition resolver which resolves references to known loaded assemblies.</summary>
        private readonly AssemblyDefinitionResolver AssemblyDefinitionResolver;

        /// <summary>The objects to dispose as part of this instance.</summary>
        private readonly HashSet<IDisposable> Disposables = new HashSet<IDisposable>();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="targetPlatform">The current game platform.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        /// <param name="paranoidMode">Whether to detect paranoid mode issues.</param>
        public AssemblyLoader(Platform targetPlatform, IMonitor monitor, bool paranoidMode)
        {
            this.Monitor = monitor;
            this.ParanoidMode = paranoidMode;
            this.AssemblyMap = this.TrackForDisposal(Constants.GetAssemblyMap(targetPlatform));
            this.AssemblyDefinitionResolver = this.TrackForDisposal(new AssemblyDefinitionResolver());
            this.AssemblyDefinitionResolver.AddSearchDirectory(Constants.ExecutionPath);
            this.AssemblyDefinitionResolver.AddSearchDirectory(Constants.InternalFilesPath);

            // generate type => assembly lookup for types which should be rewritten
            this.TypeAssemblies = new Dictionary<string, Assembly>();
            foreach (Assembly assembly in this.AssemblyMap.Targets)
            {
                ModuleDefinition module = this.AssemblyMap.TargetModules[assembly];
                foreach (TypeDefinition type in module.GetTypes())
                {
                    if (!type.IsPublic)
                        continue; // no need to rewrite
                    if (type.Namespace.Contains("<"))
                        continue; // ignore assembly metadata
                    this.TypeAssemblies[type.FullName] = assembly;
                }
            }
        }

        /// <summary>Preprocess and load an assembly.</summary>
        /// <param name="mod">The mod for which the assembly is being loaded.</param>
        /// <param name="assemblyPath">The assembly file path.</param>
        /// <param name="assumeCompatible">Assume the mod is compatible, even if incompatible code is detected.</param>
        /// <returns>Returns the rewrite metadata for the preprocessed assembly.</returns>
        /// <exception cref="IncompatibleInstructionException">An incompatible CIL instruction was found while rewriting the assembly.</exception>
        public Assembly Load(IModMetadata mod, string assemblyPath, bool assumeCompatible)
        {
            // get referenced local assemblies
            AssemblyParseResult[] assemblies;
            {
                HashSet<string> visitedAssemblyNames = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(p => p.GetName().Name)); // don't try loading assemblies that are already loaded
                assemblies = this.GetReferencedLocalAssemblies(new FileInfo(assemblyPath), visitedAssemblyNames, this.AssemblyDefinitionResolver).ToArray();
            }

            // validate load
            if (!assemblies.Any() || assemblies[0].Status == AssemblyLoadStatus.Failed)
            {
                throw new SAssemblyLoadFailedException(!File.Exists(assemblyPath)
                    ? $"Could not load '{assemblyPath}' because it doesn't exist."
                    : $"Could not load '{assemblyPath}'."
                );
            }
            if (assemblies.Last().Status == AssemblyLoadStatus.AlreadyLoaded) // mod assembly is last in dependency order
                throw new SAssemblyLoadFailedException($"Could not load '{assemblyPath}' because it was already loaded. Do you have two copies of this mod?");

            // rewrite & load assemblies in leaf-to-root order
            bool oneAssembly = assemblies.Length == 1;
            Assembly lastAssembly = null;
            HashSet<string> loggedMessages = new HashSet<string>();
            foreach (AssemblyParseResult assembly in assemblies)
            {
                if (assembly.Status == AssemblyLoadStatus.AlreadyLoaded)
                    continue;

                // rewrite assembly
                bool changed = this.RewriteAssembly(mod, assembly.Definition, loggedMessages, logPrefix: "      ");

                // detect broken assembly reference
                foreach (AssemblyNameReference reference in assembly.Definition.MainModule.AssemblyReferences)
                {
                    if (!reference.Name.StartsWith("System.") && !this.IsAssemblyLoaded(reference))
                    {
                        this.Monitor.LogOnce(loggedMessages, $"      Broken code in {assembly.File.Name}: reference to missing assembly '{reference.FullName}'.");
                        if (!assumeCompatible)
                            throw new IncompatibleInstructionException($"Found a reference to missing assembly '{reference.FullName}' while loading assembly {assembly.File.Name}.");
                        mod.SetWarning(ModWarning.BrokenCodeLoaded);
                        break;
                    }
                }

                // load assembly
                if (changed)
                {
                    if (!oneAssembly)
                        this.Monitor.Log($"      Loading {assembly.File.Name} (rewritten in memory)...", LogLevel.Trace);
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        assembly.Definition.Write(outStream);
                        byte[] bytes = outStream.ToArray();
                        lastAssembly = Assembly.Load(bytes);
                    }
                }
                else
                {
                    if (!oneAssembly)
                        this.Monitor.Log($"      Loading {assembly.File.Name}...", LogLevel.Trace);
                    lastAssembly = Assembly.UnsafeLoadFrom(assembly.File.FullName);
                }

                // track loaded assembly for definition resolution
                this.AssemblyDefinitionResolver.Add(assembly.Definition);
            }

            // throw if incompatibilities detected
            if (!assumeCompatible && mod.Warnings.HasFlag(ModWarning.BrokenCodeLoaded))
                throw new IncompatibleInstructionException();

            // last assembly loaded is the root
            return lastAssembly;
        }

        /// <summary>Get whether an assembly is loaded.</summary>
        /// <param name="reference">The assembly name reference.</param>
        public bool IsAssemblyLoaded(AssemblyNameReference reference)
        {
            try
            {
                return this.AssemblyDefinitionResolver.Resolve(reference) != null;
            }
            catch (AssemblyResolutionException)
            {
                return false;
            }
        }

        /// <summary>Resolve an assembly by its name.</summary>
        /// <param name="name">The assembly name.</param>
        /// <remarks>
        /// This implementation returns the first loaded assembly which matches the short form of
        /// the assembly name, to resolve assembly resolution issues when rewriting
        /// assemblies (especially with Mono). Since this is meant to be called on <see cref="AppDomain.AssemblyResolve"/>,
        /// the implicit assumption is that loading the exact assembly failed.
        /// </remarks>
        public static Assembly ResolveAssembly(string name)
        {
            string shortName = name.Split(new[] { ',' }, 2).First(); // get simple name (without version and culture)
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .FirstOrDefault(p => p.GetName().Name == shortName);
        }

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        public void Dispose()
        {
            foreach (IDisposable instance in this.Disposables)
                instance.Dispose();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Track an object for disposal as part of the assembly loader.</summary>
        /// <typeparam name="T">The instance type.</typeparam>
        /// <param name="instance">The disposable instance.</param>
        private T TrackForDisposal<T>(T instance) where T : IDisposable
        {
            this.Disposables.Add(instance);
            return instance;
        }

        /****
        ** Assembly parsing
        ****/
        /// <summary>Get a list of referenced local assemblies starting from the mod assembly, ordered from leaf to root.</summary>
        /// <param name="file">The assembly file to load.</param>
        /// <param name="visitedAssemblyNames">The assembly names that should be skipped.</param>
        /// <param name="assemblyResolver">A resolver which resolves references to known assemblies.</param>
        /// <returns>Returns the rewrite metadata for the preprocessed assembly.</returns>
        private IEnumerable<AssemblyParseResult> GetReferencedLocalAssemblies(FileInfo file, HashSet<string> visitedAssemblyNames, IAssemblyResolver assemblyResolver)
        {
            // validate
            if (file.Directory == null)
                throw new InvalidOperationException($"Could not get directory from file path '{file.FullName}'.");
            if (!file.Exists)
                yield break; // not a local assembly

            // read assembly
            byte[] assemblyBytes = File.ReadAllBytes(file.FullName);
            Stream readStream = this.TrackForDisposal(new MemoryStream(assemblyBytes));
            AssemblyDefinition assembly = this.TrackForDisposal(AssemblyDefinition.ReadAssembly(readStream, new ReaderParameters(ReadingMode.Immediate) { AssemblyResolver = assemblyResolver, InMemory = true }));

            // skip if already visited
            if (visitedAssemblyNames.Contains(assembly.Name.Name))
            {
                yield return new AssemblyParseResult(file, null, AssemblyLoadStatus.AlreadyLoaded);
                yield break;
            }
            visitedAssemblyNames.Add(assembly.Name.Name);

            // yield referenced assemblies
            foreach (AssemblyNameReference dependency in assembly.MainModule.AssemblyReferences)
            {
                FileInfo dependencyFile = new FileInfo(Path.Combine(file.Directory.FullName, $"{dependency.Name}.dll"));
                foreach (AssemblyParseResult result in this.GetReferencedLocalAssemblies(dependencyFile, visitedAssemblyNames, assemblyResolver))
                    yield return result;
            }

            // yield assembly
            yield return new AssemblyParseResult(file, assembly, AssemblyLoadStatus.Okay);
        }

        /****
        ** Assembly rewriting
        ****/
        /// <summary>Rewrite the types referenced by an assembly.</summary>
        /// <param name="mod">The mod for which the assembly is being loaded.</param>
        /// <param name="assembly">The assembly to rewrite.</param>
        /// <param name="loggedMessages">The messages that have already been logged for this mod.</param>
        /// <param name="logPrefix">A string to prefix to log messages.</param>
        /// <returns>Returns whether the assembly was modified.</returns>
        /// <exception cref="IncompatibleInstructionException">An incompatible CIL instruction was found while rewriting the assembly.</exception>
        private bool RewriteAssembly(IModMetadata mod, AssemblyDefinition assembly, HashSet<string> loggedMessages, string logPrefix)
        {
            ModuleDefinition module = assembly.MainModule;
            string filename = $"{assembly.Name.Name}.dll";

            // swap assembly references if needed (e.g. XNA => MonoGame)
            bool platformChanged = false;
            for (int i = 0; i < module.AssemblyReferences.Count; i++)
            {
                // remove old assembly reference
                if (this.AssemblyMap.RemoveNames.Any(name => module.AssemblyReferences[i].Name == name))
                {
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Rewriting {filename} for OS...");
                    platformChanged = true;
                    module.AssemblyReferences.RemoveAt(i);
                    i--;
                }
            }
            if (platformChanged)
            {
                // add target assembly references
                foreach (AssemblyNameReference target in this.AssemblyMap.TargetReferences.Values)
                    module.AssemblyReferences.Add(target);

                // rewrite type scopes to use target assemblies
                IEnumerable<TypeReference> typeReferences = module.GetTypeReferences().OrderBy(p => p.FullName);
                foreach (TypeReference type in typeReferences)
                    this.ChangeTypeScope(type);
            }

            // find (and optionally rewrite) incompatible instructions
            bool anyRewritten = false;
            IInstructionHandler[] handlers = new InstructionMetadata().GetHandlers(this.ParanoidMode).ToArray();
            foreach (MethodDefinition method in this.GetMethods(module))
            {
                // check method definition
                foreach (IInstructionHandler handler in handlers)
                {
                    InstructionHandleResult result = handler.Handle(module, method, this.AssemblyMap, platformChanged);
                    this.ProcessInstructionHandleResult(mod, handler, result, loggedMessages, logPrefix, filename);
                    if (result == InstructionHandleResult.Rewritten)
                        anyRewritten = true;
                }

                // check CIL instructions
                ILProcessor cil = method.Body.GetILProcessor();
                var instructions = cil.Body.Instructions;
                // ReSharper disable once ForCanBeConvertedToForeach -- deliberate access by index so each handler sees replacements from previous handlers
                for (int offset = 0; offset < instructions.Count; offset++)
                {
                    foreach (IInstructionHandler handler in handlers)
                    {
                        Instruction instruction = instructions[offset];
                        InstructionHandleResult result = handler.Handle(module, cil, instruction, this.AssemblyMap, platformChanged);
                        this.ProcessInstructionHandleResult(mod, handler, result, loggedMessages, logPrefix, filename);
                        if (result == InstructionHandleResult.Rewritten)
                            anyRewritten = true;
                    }
                }
            }

            return platformChanged || anyRewritten;
        }

        /// <summary>Process the result from an instruction handler.</summary>
        /// <param name="mod">The mod being analyzed.</param>
        /// <param name="handler">The instruction handler.</param>
        /// <param name="result">The result returned by the handler.</param>
        /// <param name="loggedMessages">The messages already logged for the current mod.</param>
        /// <param name="logPrefix">A string to prefix to log messages.</param>
        /// <param name="filename">The assembly filename for log messages.</param>
        private void ProcessInstructionHandleResult(IModMetadata mod, IInstructionHandler handler, InstructionHandleResult result, HashSet<string> loggedMessages, string logPrefix, string filename)
        {
            switch (result)
            {
                case InstructionHandleResult.Rewritten:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Rewrote {filename} to fix {handler.NounPhrase}...");
                    break;

                case InstructionHandleResult.NotCompatible:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Broken code in {filename}: {handler.NounPhrase}.");
                    mod.SetWarning(ModWarning.BrokenCodeLoaded);
                    break;

                case InstructionHandleResult.DetectedGamePatch:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Detected game patcher ({handler.NounPhrase}) in assembly {filename}.");
                    mod.SetWarning(ModWarning.PatchesGame);
                    break;

                case InstructionHandleResult.DetectedSaveSerializer:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Detected possible save serializer change ({handler.NounPhrase}) in assembly {filename}.");
                    mod.SetWarning(ModWarning.ChangesSaveSerializer);
                    break;

                case InstructionHandleResult.DetectedUnvalidatedUpdateTick:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Detected reference to {handler.NounPhrase} in assembly {filename}.");
                    mod.SetWarning(ModWarning.UsesUnvalidatedUpdateTick);
                    break;

                case InstructionHandleResult.DetectedDynamic:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Detected 'dynamic' keyword ({handler.NounPhrase}) in assembly {filename}.");
                    mod.SetWarning(ModWarning.UsesDynamic);
                    break;

                case InstructionHandleResult.DetectedFilesystemAccess:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Detected filesystem access ({handler.NounPhrase}) in assembly {filename}.");
                    mod.SetWarning(ModWarning.AccessesFilesystem);
                    break;

                case InstructionHandleResult.DetectedShellAccess:
                    this.Monitor.LogOnce(loggedMessages, $"{logPrefix}Detected shell or process access ({handler.NounPhrase}) in assembly {filename}.");
                    mod.SetWarning(ModWarning.AccessesShell);
                    break;

                case InstructionHandleResult.None:
                    break;

                default:
                    throw new NotSupportedException($"Unrecognized instruction handler result '{result}'.");
            }
        }

        /// <summary>Get the correct reference to use for compatibility with the current platform.</summary>
        /// <param name="type">The type reference to rewrite.</param>
        private void ChangeTypeScope(TypeReference type)
        {
            // check skip conditions
            if (type == null || type.FullName.StartsWith("System."))
                return;

            // get assembly
            if (!this.TypeAssemblies.TryGetValue(type.FullName, out Assembly assembly))
                return;

            // replace scope
            AssemblyNameReference assemblyRef = this.AssemblyMap.TargetReferences[assembly];
            type.Scope = assemblyRef;
        }

        /// <summary>Get all methods in a module.</summary>
        /// <param name="module">The module to search.</param>
        private IEnumerable<MethodDefinition> GetMethods(ModuleDefinition module)
        {
            return (
                from type in module.GetTypes()
                where type.HasMethods
                from method in type.Methods
                where method.HasBody
                select method
            );
        }
    }
}
