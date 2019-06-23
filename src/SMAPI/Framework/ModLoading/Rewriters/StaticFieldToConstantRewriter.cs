using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites static field references into constant values.</summary>
    /// <typeparam name="TValue">The constant value type.</typeparam>
    internal class StaticFieldToConstantRewriter<TValue> : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The constant value to replace with.</summary>
        private readonly TValue Value;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="value">The constant value to replace with.</param>
        public StaticFieldToConstantRewriter(Type type, string fieldName, TValue value)
            : base(type.FullName, fieldName, InstructionHandleResult.None)
        {
            this.Value = value;
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

            cil.Replace(instruction, this.CreateConstantInstruction(cil, this.Value));
            return InstructionHandleResult.Rewritten;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Create a CIL constant value instruction.</summary>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="value">The constant value to set.</param>
        private Instruction CreateConstantInstruction(ILProcessor cil, object value)
        {
            if (typeof(TValue) == typeof(int))
                return cil.Create(OpCodes.Ldc_I4, (int)value);
            if (typeof(TValue) == typeof(string))
                return cil.Create(OpCodes.Ldstr, (string)value);
            throw new NotSupportedException($"Rewriting to constant values of type {typeof(TValue)} isn't currently supported.");
        }
    }
}
