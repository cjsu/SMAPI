using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds references to a field, property, or method which no longer exists.</summary>
    /// <remarks>This implementation is purely heuristic. It should never return a false positive, but won't detect all cases.</remarks>
    internal class ReferenceToMissingMemberFinder : IInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assembly names to which to heuristically detect broken references.</summary>
        private readonly HashSet<string> ValidateReferencesToAssemblies;


        /*********
        ** Accessors
        *********/
        /// <summary>A brief noun phrase indicating what the instruction finder matches.</summary>
        public string NounPhrase { get; private set; } = "";


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="validateReferencesToAssemblies">The assembly names to which to heuristically detect broken references.</param>
        public ReferenceToMissingMemberFinder(string[] validateReferencesToAssemblies)
        {
            this.ValidateReferencesToAssemblies = new HashSet<string>(validateReferencesToAssemblies);
        }

        /// <summary>Perform the predefined logic for a method if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="method">The method definition containing the instruction.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public virtual InstructionHandleResult Handle(ModuleDefinition module, MethodDefinition method, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            return InstructionHandleResult.None;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public virtual InstructionHandleResult Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            // field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null && this.ShouldValidate(fieldRef.DeclaringType))
            {
                FieldDefinition target = fieldRef.DeclaringType.Resolve()?.Fields.FirstOrDefault(p => p.Name == fieldRef.Name);
                if (target == null)
                {
                    this.NounPhrase = $"reference to {fieldRef.DeclaringType.FullName}.{fieldRef.Name} (no such field)";
                    return InstructionHandleResult.NotCompatible;
                }
            }

            // method reference
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (methodRef != null && this.ShouldValidate(methodRef.DeclaringType) && !this.IsUnsupported(methodRef))
            {
                MethodDefinition target = methodRef.Resolve();
                if (target == null)
                {
                    if (this.IsProperty(methodRef))
                        this.NounPhrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name.Substring(4)} (no such property)";
                    else if (methodRef.Name == ".ctor")
                        this.NounPhrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no matching constructor)";
                    else
                        this.NounPhrase = $"reference to {methodRef.DeclaringType.FullName}.{methodRef.Name} (no such method)";
                    return InstructionHandleResult.NotCompatible;
                }
            }

            return InstructionHandleResult.None;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Whether references to the given type should be validated.</summary>
        /// <param name="type">The type reference.</param>
        private bool ShouldValidate(TypeReference type)
        {
            return type != null && this.ValidateReferencesToAssemblies.Contains(type.Scope.Name);
        }

        /// <summary>Get whether a method reference is a special case that's not currently supported (e.g. array methods).</summary>
        /// <param name="method">The method reference.</param>
        private bool IsUnsupported(MethodReference method)
        {
            return
                method.DeclaringType.Name.Contains("["); // array methods
        }

        /// <summary>Get whether a method reference is a property getter or setter.</summary>
        /// <param name="method">The method reference.</param>
        private bool IsProperty(MethodReference method)
        {
            return method.Name.StartsWith("get_") || method.Name.StartsWith("set_");
        }
    }
}
