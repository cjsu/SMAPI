using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    internal class TypeFieldToAnotherTypeFieldRewriter : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The type whose field to which references should be rewritten to.</summary>
        private readonly Type ToType;

        /// <summary>The field name.</summary>
        private readonly string FieldName;

        /// <summary>The property name.</summary>
        private readonly string PropertyName;

        private readonly string TestName;

        private readonly IMonitor Monitor;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="propertyName">The property name (if different).</param>
        public TypeFieldToAnotherTypeFieldRewriter(Type type, Type toType, string fieldName, string propertyName, IMonitor monitor, string testName = null)
            : base(type.FullName, fieldName, InstructionHandleResult.None)
        {
            this.Monitor = monitor;
            this.Type = type;
            this.ToType = toType;
            this.FieldName = fieldName;
            this.PropertyName = propertyName;
            this.TestName = testName;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        public TypeFieldToAnotherTypeFieldRewriter(Type type, Type toType, string fieldName, IMonitor monitor)
            : this(type, toType, fieldName, fieldName, monitor) { }

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

            //string methodPrefix = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld ? "get";
            try
            {
                MethodReference method = module.ImportReference(this.ToType.GetMethod($"get_{this.PropertyName}"));
                FieldReference field = module.ImportReference(this.ToType.GetField(this.FieldName));

                cil.InsertAfter(instruction, cil.Create(OpCodes.Ldfld, field));
                cil.Replace(instruction, cil.Create(OpCodes.Call, method));
            }
            catch (Exception e)
            {
                this.Monitor.Log(e.Message);
                this.Monitor.Log(e.StackTrace);
            }

            return InstructionHandleResult.Rewritten;
        }
    }
}
