using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites references to one field with another.</summary>
    internal class FieldReplaceRewriter : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The new field to reference.</summary>
        private readonly FieldInfo ToField;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fromFieldName">The field name to rewrite.</param>
        /// <param name="toFieldName">The new field name to reference.</param>
        public FieldReplaceRewriter(Type type, string fromFieldName, string toFieldName)
            : base(type.FullName, fromFieldName, InstructionHandleResult.None)
        {
            this.ToField = type.GetField(toFieldName);
            if (this.ToField == null)
                throw new InvalidOperationException($"The {type.FullName} class doesn't have a {toFieldName} field.");
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

            FieldReference newRef = module.ImportReference(this.ToField);
            cil.Replace(instruction, cil.Create(instruction.OpCode, newRef));
            return InstructionHandleResult.Rewritten;
        }
    }
}
