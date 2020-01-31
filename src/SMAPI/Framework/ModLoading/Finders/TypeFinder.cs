using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds incompatible CIL instructions that reference a given type.</summary>
    internal class TypeFinder : IInstructionHandler
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The full type name for which to find references.</summary>
        private readonly string FullTypeName;

        /// <summary>The result to return for matching instructions.</summary>
        private readonly InstructionHandleResult Result;

        /// <summary>A lambda which overrides a matched type.</summary>
        protected readonly Func<TypeReference, bool> ShouldIgnore;


        /*********
        ** Accessors
        *********/
        /// <summary>A brief noun phrase indicating what the instruction finder matches.</summary>
        public string NounPhrase { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fullTypeName">The full type name to match.</param>
        /// <param name="result">The result to return for matching instructions.</param>
        /// <param name="shouldIgnore">A lambda which overrides a matched type.</param>
        public TypeFinder(string fullTypeName, InstructionHandleResult result, Func<TypeReference, bool> shouldIgnore = null)
        {
            this.FullTypeName = fullTypeName;
            this.Result = result;
            this.NounPhrase = $"{fullTypeName} type";
            this.ShouldIgnore = shouldIgnore ?? (p => false);
        }

        /// <summary>Perform the predefined logic for a method if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="method">The method definition containing the instruction.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public virtual InstructionHandleResult Handle(ModuleDefinition module, MethodDefinition method, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            return this.IsMatch(method)
                ? this.Result
                : InstructionHandleResult.None;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public virtual InstructionHandleResult Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            return this.IsMatch(instruction)
                ? this.Result
                : InstructionHandleResult.None;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="method">The method definition.</param>
        protected bool IsMatch(MethodDefinition method)
        {
            if (this.IsMatch(method.ReturnType))
                return true;

            foreach (VariableDefinition variable in method.Body.Variables)
            {
                if (this.IsMatch(variable.VariableType))
                    return true;
            }

            return false;
        }

        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        protected bool IsMatch(Instruction instruction)
        {
            // field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null)
            {
                return
                    this.IsMatch(fieldRef.DeclaringType) // field on target class
                    || this.IsMatch(fieldRef.FieldType); // field value is target class
            }

            // method reference
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (methodRef != null)
            {
                return
                    this.IsMatch(methodRef.DeclaringType) // method on target class
                    || this.IsMatch(methodRef.ReturnType) // method returns target class
                    || methodRef.Parameters.Any(p => this.IsMatch(p.ParameterType)); // method parameters
            }

            return false;
        }

        /// <summary>Get whether a type reference matches the expected type.</summary>
        /// <param name="type">The type to check.</param>
        protected bool IsMatch(TypeReference type)
        {
            // root type
            if (type.FullName == this.FullTypeName && !this.ShouldIgnore(type))
                return true;

            // generic arguments
            if (type is GenericInstanceType genericType)
            {
                if (genericType.GenericArguments.Any(this.IsMatch))
                    return true;
            }

            // generic parameters (e.g. constraints)
            if (type.GenericParameters.Any(this.IsMatch))
                return true;

            return false;
        }
    }
}
