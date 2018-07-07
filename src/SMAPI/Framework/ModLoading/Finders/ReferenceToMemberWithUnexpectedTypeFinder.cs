using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds references to a field, property, or method which returns a different type than the code expects.</summary>
    /// <remarks>This implementation is purely heuristic. It should never return a false positive, but won't detect all cases.</remarks>
    internal class ReferenceToMemberWithUnexpectedTypeFinder : IInstructionHandler
    {
        /*********
        ** Properties
        *********/
        /// <summary>The assembly names to which to heuristically detect broken references.</summary>
        private readonly HashSet<string> ValidateReferencesToAssemblies;

        /// <summary>A pattern matching type name substrings to strip for display.</summary>
        private readonly Regex StripTypeNamePattern = new Regex(@"`\d+(?=<)", RegexOptions.Compiled);


        /*********
        ** Accessors
        *********/
        /// <summary>A brief noun phrase indicating what the instruction finder matches.</summary>
        public string NounPhrase { get; private set; } = "";


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="validateReferencesToAssemblies">The assembly names to which to heuristically detect broken references.</param>
        public ReferenceToMemberWithUnexpectedTypeFinder(string[] validateReferencesToAssemblies)
        {
            this.ValidateReferencesToAssemblies = new HashSet<string>(validateReferencesToAssemblies);
        }

        /// <summary>Perform the predefined logic for a method if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="method">The method definition containing the instruction.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public virtual InstructionHandleResult Handle(ModuleDefinition module, MethodDefinition method, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            return InstructionHandleResult.None;
        }

        /// <summary>Perform the predefined logic for an instruction if applicable.</summary>
        /// <param name="module">The assembly module containing the instruction.</param>
        /// <param name="cil">The CIL processor.</param>
        /// <param name="instruction">The instruction to handle.</param>
        /// <param name="assemblyMap">Metadata for mapping assemblies to the current platform.</param>
        /// <param name="platformChanged">Whether the mod was compiled on a different platform.</param>
        public virtual InstructionHandleResult Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction, PlatformAssemblyMap assemblyMap, bool platformChanged)
        {
            // field reference
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null && this.ShouldValidate(fieldRef.DeclaringType))
            {
                // get target field
                FieldDefinition targetField = fieldRef.DeclaringType.Resolve()?.Fields.FirstOrDefault(p => p.Name == fieldRef.Name);
                if (targetField == null)
                    return InstructionHandleResult.None;

                // validate return type
                string actualReturnTypeID = this.GetComparableTypeID(targetField.FieldType);
                string expectedReturnTypeID = this.GetComparableTypeID(fieldRef.FieldType);

                if (!RewriteHelper.LooksLikeSameType(expectedReturnTypeID, actualReturnTypeID))
                {
                    this.NounPhrase = $"reference to {fieldRef.DeclaringType.FullName}.{fieldRef.Name} (field returns {this.GetFriendlyTypeName(targetField.FieldType, actualReturnTypeID)}, not {this.GetFriendlyTypeName(fieldRef.FieldType, expectedReturnTypeID)})";
                    return InstructionHandleResult.NotCompatible;
                }
            }

            // method reference
            MethodReference methodReference = RewriteHelper.AsMethodReference(instruction);
            if (methodReference != null && this.ShouldValidate(methodReference.DeclaringType))
            {
                // get potential targets
                MethodDefinition[] candidateMethods = methodReference.DeclaringType.Resolve()?.Methods.Where(found => found.Name == methodReference.Name).ToArray();
                if (candidateMethods == null || !candidateMethods.Any())
                    return InstructionHandleResult.None;

                // compare return types
                MethodDefinition methodDef = methodReference.Resolve();
                if (methodDef == null)
                {
                    this.NounPhrase = $"reference to {methodReference.DeclaringType.FullName}.{methodReference.Name} (no such method)";
                    return InstructionHandleResult.NotCompatible;
                }

                string expectedReturnType = this.GetComparableTypeID(methodDef.ReturnType);
                if (candidateMethods.All(method => this.GetComparableTypeID(method.ReturnType) != expectedReturnType))
                {
                    this.NounPhrase = $"reference to {methodDef.DeclaringType.FullName}.{methodDef.Name} (no such method returns {this.GetFriendlyTypeName(methodDef.ReturnType, expectedReturnType)})";
                    return InstructionHandleResult.NotCompatible;
                }
            }

            return InstructionHandleResult.None;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Whether references to the given type should be validated.</summary>
        /// <param name="type">The type reference.</param>
        private bool ShouldValidate(TypeReference type)
        {
            if (type != null)
                return true;
                
            // Extract scope name from type string representation for compatibility
            // Under Linux, type.Scope.Name sometimes reports incorrectly
            string scopeName = type.ToString();
            if (scopeName[0] != '$')
                return false;

            scopeName = scopeName.Substring(0, scopeName.IndexOf(".", System.StringComparison.CurrentCulture));

            return this.ValidateReferencesToAssemblies.Contains(scopeName);
        }

        /// <summary>Get a unique string representation of a type.</summary>
        /// <param name="type">The type reference.</param>
        private string GetComparableTypeID(TypeReference type)
        {
            return this.StripTypeNamePattern.Replace(type.FullName, "");
        }

        /// <summary>Get a shorter type name for display.</summary>
        /// <param name="type">The type reference.</param>
        /// <param name="typeID">The comparable type ID from <see cref="GetComparableTypeID"/>.</param>
        private string GetFriendlyTypeName(TypeReference type, string typeID)
        {
            // most common built-in types
            switch (type.FullName)
            {
                case "System.Boolean":
                    return "bool";
                case "System.Int32":
                    return "int";
                case "System.String":
                    return "string";
            }

            // most common unambiguous namespaces
            foreach (string @namespace in new[] { "Microsoft.Xna.Framework", "Netcode", "System", "System.Collections.Generic" })
            {
                if (type.Namespace == @namespace)
                    return typeID.Substring(@namespace.Length + 1);
            }

            return typeID;
        }
    }
}
