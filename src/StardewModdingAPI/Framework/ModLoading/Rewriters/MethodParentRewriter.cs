using System;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites method references from one parent type to another if the signatures match.</summary>
    internal class MethodParentRewriter : IInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose methods to remap.</summary>
        private readonly Type FromType;

        /// <summary>The type with methods to map to.</summary>
        private readonly Type ToType;

        /// <summary>Whether to only rewrite references if loading the assembly on a different platform than it was compiled on.</summary>
        private readonly bool OnlyIfPlatformChanged;


        /*********
        ** Accessors
        *********/
        /// <summary>A brief noun phrase indicating what the instruction finder matches.</summary>
        public string NounPhrase { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fromType">The type whose methods to remap.</param>
        /// <param name="toType">The type with methods to map to.</param>
        /// <param name="onlyIfPlatformChanged">Whether to only rewrite references if loading the assembly on a different platform than it was compiled on.</param>
        public MethodParentRewriter(Type fromType, Type toType, bool onlyIfPlatformChanged = false)
        {
            this.FromType = fromType;
            this.ToType = toType;
            this.NounPhrase = $"{fromType.Name} methods";
            this.OnlyIfPlatformChanged = onlyIfPlatformChanged;
        }

        /// <summary>Perform the predefined logic for a method if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="method">The method definition containing the instruction.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public InstructionHandleResult Handle(ModuleDefinition module, MethodDefinition method, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            return InstructionHandleResult.None;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public InstructionHandleResult Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            if (!this.IsMatch(instruction, platformChanged))
                return InstructionHandleResult.None;

            MethodReference methodRef = (MethodReference)instruction.Operand;
            methodRef.DeclaringType = module.ImportReference(this.ToType);
            return InstructionHandleResult.Rewritten;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        protected bool IsMatch(Instruction instruction, bool platformChanged)
        {
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            return
                methodRef != null
                && (platformChanged || !this.OnlyIfPlatformChanged)
                && methodRef.DeclaringType.FullName == this.FromType.FullName
                && RewriteHelper.HasMatchingSignature(this.ToType, methodRef);
        }
    }
}
