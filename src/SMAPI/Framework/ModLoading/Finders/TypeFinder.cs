using System;
using Mono.Cecil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds incompatible CIL instructions that reference a given type.</summary>
    internal class TypeFinder : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The full type name to match.</summary>
        private readonly string FullTypeName;

        /// <summary>The result to return for matching instructions.</summary>
        private readonly InstructionHandleResult Result;

        /// <summary>Get whether a matched type should be ignored.</summary>
        private readonly Func<TypeReference, bool> ShouldIgnore;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fullTypeName">The full type name to match.</param>
        /// <param name="result">The result to return for matching instructions.</param>
        /// <param name="shouldIgnore">Get whether a matched type should be ignored.</param>
        public TypeFinder(string fullTypeName, InstructionHandleResult result, Func<TypeReference, bool> shouldIgnore = null)
            : base(defaultPhrase: $"{fullTypeName} type")
        {
            this.FullTypeName = fullTypeName;
            this.Result = result;
            this.ShouldIgnore = shouldIgnore;
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
        {
            if (type.FullName == this.FullTypeName && this.ShouldIgnore?.Invoke(type) != true)
                this.MarkFlag(this.Result);

            return false;
        }
    }
}
