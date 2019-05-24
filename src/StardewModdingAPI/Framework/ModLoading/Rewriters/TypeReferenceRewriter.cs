using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites all references to a type.</summary>
    internal class TypeReferenceRewriter : TypeFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The full type name to which to find references.</summary>
        private readonly string FromTypeName;

        /// <summary>The new type to reference.</summary>
        private readonly Type ToType;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fromTypeFullName">The full type name to which to find references.</param>
        /// <param name="toType">The new type to reference.</param>
        /// <param name="shouldIgnore">A lambda which overrides a matched type.</param>
        public TypeReferenceRewriter(string fromTypeFullName, Type toType, Func<TypeReference, bool> shouldIgnore = null)
            : base(fromTypeFullName, InstructionHandleResult.None, shouldIgnore)
        {
            this.FromTypeName = fromTypeFullName;
            this.ToType = toType;
        }

        /// <summary>Perform the predefined logic for a method if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="method">The method definition containing the instruction.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public override InstructionHandleResult Handle(ModuleDefinition module, MethodDefinition method, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            bool rewritten = false;

            // return type
            if (this.IsMatch(method.ReturnType))
            {
                this.RewriteIfNeeded(module, method.ReturnType, newType => method.ReturnType = newType);
                rewritten = true;
            }

            // parameters
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                if (this.IsMatch(parameter.ParameterType))
                {
                    this.RewriteIfNeeded(module, parameter.ParameterType, newType => parameter.ParameterType = newType);
                    rewritten = true;
                }
            }

            // generic parameters
            for (int i = 0; i < method.GenericParameters.Count; i++)
            {
                var parameter = method.GenericParameters[i];
                if (this.IsMatch(parameter))
                {
                    this.RewriteIfNeeded(module, parameter, newType => method.GenericParameters[i] = new GenericParameter(parameter.Name, newType));
                    rewritten = true;
                }
            }

            // local variables
            foreach (VariableDefinition variable in method.Body.Variables)
            {
                if (this.IsMatch(variable.VariableType))
                {
                    this.RewriteIfNeeded(module, variable.VariableType, newType => variable.VariableType = newType);
                    rewritten = true;
                }
            }

            return rewritten
                ? InstructionHandleResult.Rewritten
                : InstructionHandleResult.None;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public override InstructionHandleResult Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            if (!this.IsMatch(instruction))
                return InstructionHandleResult.None;

            // field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null)
            {
                this.RewriteIfNeeded(module, fieldRef.DeclaringType, newType => fieldRef.DeclaringType = newType);
                this.RewriteIfNeeded(module, fieldRef.FieldType, newType => fieldRef.FieldType = newType);
            }

            // method reference
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (methodRef != null)
            {
                this.RewriteIfNeeded(module, methodRef.DeclaringType, newType => methodRef.DeclaringType = newType);
                this.RewriteIfNeeded(module, methodRef.ReturnType, newType => methodRef.ReturnType = newType);
                foreach (var parameter in methodRef.Parameters)
                    this.RewriteIfNeeded(module, parameter.ParameterType, newType => parameter.ParameterType = newType);
            }

            // type reference
            if (instruction.Operand is TypeReference typeRef)
                this.RewriteIfNeeded(module, typeRef, newType => cil.Replace(instruction, cil.Create(instruction.OpCode, newType)));

            return InstructionHandleResult.Rewritten;
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Change a type reference if needed.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="type">The type to replace if it matches.</param>
        /// <param name="set">Assign the new type reference.</param>
        private void RewriteIfNeeded(ModuleDefinition module, TypeReference type, Action<TypeReference> set)
        {
            // current type
            if (type.FullName == this.FromTypeName)
            {
                if (!this.ShouldIgnore(type))
                    set(module.ImportReference(this.ToType));
                return;
            }

            // recurse into generic arguments
            if (type is GenericInstanceType genericType)
            {
                for (int i = 0; i < genericType.GenericArguments.Count; i++)
                    this.RewriteIfNeeded(module, genericType.GenericArguments[i], typeRef => genericType.GenericArguments[i] = typeRef);
            }

            // recurse into generic parameters (e.g. constraints)
            for (int i = 0; i < type.GenericParameters.Count; i++)
                this.RewriteIfNeeded(module, type.GenericParameters[i], typeRef => type.GenericParameters[i] = new GenericParameter(typeRef));
        }
    }
}
