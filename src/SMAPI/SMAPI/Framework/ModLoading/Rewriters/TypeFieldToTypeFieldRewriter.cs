using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Finders;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites field references into property references.</summary>
    internal class TypeFieldToTypeFieldRewriter : FieldFinder
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type whose field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The type whose field to which references should be rewritten to.</summary>
        private readonly Type ToType;

        /// <summary>The property name.</summary>
        private readonly string PropertyName;

        private readonly IMonitor Monitor;

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        /// <param name="propertyName">The property name (if different).</param>
        public TypeFieldToTypeFieldRewriter(Type type, Type toType, string fieldName, string propertyName, IMonitor monitor)
            : base(type.FullName, fieldName, InstructionHandleResult.None)
        {
            this.Monitor = monitor;
            this.Type = type;
            this.ToType = toType;
            this.PropertyName = propertyName;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to which references should be rewritten.</param>
        /// <param name="fieldName">The field name to rewrite.</param>
        public TypeFieldToTypeFieldRewriter(Type type, Type toType, string fieldName, IMonitor monitor)
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

            //Instruction: IL_0025: ldsfld StardewValley.GameLocation StardewValley.Game1::currentLocation
            string methodPrefix = instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldfld ? "get" : "set";
            try
            {
                //MethodReference propertyRef = module.ImportReference(this.ToType.GetMethod($"{methodPrefix}_{this.PropertyName}"));

                MethodReference method = module.ImportReference(this.ToType.GetMethod($"{methodPrefix}_{this.PropertyName}"));
                this.Monitor.Log("Method Ref: " + method.ToString());

                cil.Replace(instruction, cil.Create(OpCodes.Call, method));
            }
            catch (Exception e)
            {
                this.Monitor.Log(e.Message);
            }
            

            return InstructionHandleResult.Rewritten;
        }
    }
}
