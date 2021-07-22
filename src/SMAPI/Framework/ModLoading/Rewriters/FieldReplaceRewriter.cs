using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites references to one field with another.</summary>
    internal class FieldReplaceRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The type containing the field to which references should be rewritten.</summary>
        private readonly Type Type;

        /// <summary>The field name to which references should be rewritten.</summary>
        private readonly string FromFieldName;

        /// <summary>The new field to reference.</summary>
        private readonly FieldInfo ToField;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fromType">The type whose field to rewrite.</param>
        /// <param name="fromFieldName">The field name to rewrite.</param>
        /// <param name="toType">The new type which will have the field.</param>
        /// <param name="toFieldName">The new field name to reference.</param>
        public FieldReplaceRewriter(Type fromType, string fromFieldName, Type toType, string toFieldName)
            : base(defaultPhrase: $"{fromType.FullName}.{fromFieldName} field")
        {
            this.Type = fromType;
            this.FromFieldName = fromFieldName;
            this.ToField = toType.GetField(toFieldName);
            if (this.ToField == null)
                throw new InvalidOperationException($"The {toType.FullName} class doesn't have a {toFieldName} field.");
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="type">The type whose field to rewrite.</param>
        /// <param name="fromFieldName">The field name to rewrite.</param>
        /// <param name="toFieldName">The new field name to reference.</param>
        public FieldReplaceRewriter(Type type, string fromFieldName, string toFieldName)
            : this(type, fromFieldName, type, toFieldName)
        {
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            // get field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (!RewriteHelper.IsFieldReferenceTo(fieldRef, this.Type.FullName, this.FromFieldName))
                return false;

            // replace with new field
            instruction.Operand = module.ImportReference(this.ToField);

            return this.MarkRewritten();
        }
    }
}
