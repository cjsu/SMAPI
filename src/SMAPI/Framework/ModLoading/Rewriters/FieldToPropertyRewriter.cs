using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites field references into property references.</summary>
    internal class FieldToPropertyRewriter : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The property name.</summary>
        private readonly string PropertyName;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="propertyName">The property name (if different).</param>
        public FieldToPropertyRewriter(Type type, string fieldName, string propertyName)
            : base(type.FullName, fieldName, InstructionHandleResult.None)
        {
            this.Type = type;
            this.PropertyName = propertyName;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        public FieldToPropertyRewriter(Type type, string fieldName)
            : this(type, fieldName, fieldName) { }

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

            string methodPrefix = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld ? "get" : "set";
            MethodReference propertyRef = module.ImportReference(this.Type.GetMethod($"{methodPrefix}_{this.PropertyName}"));
            cil.Replace(instruction, cil.Create(OpCodes.Call, propertyRef));

            return InstructionHandleResult.Rewritten;
        }
    }
}
