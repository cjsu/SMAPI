using System;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;
using StardewModdingAPI.Framework.ModLoading.RewriteFacades;

namespace StardewModdingAPI.Framework.ModLoading.Rewriters
{
    /// <summary>Rewrites Harmony 1.x assembly references to work with Harmony 2.x.</summary>
    internal class Harmony1AssemblyRewriter : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>Whether any Harmony 1.x types were replaced.</summary>
        private bool ReplacedTypes;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public Harmony1AssemblyRewriter()
            : base(defaultPhrase: "Harmony 1.x") { }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, TypeReference type, Action<TypeReference> replaceWith)
        {
            // rewrite Harmony 1.x type to Harmony 2.0 type
            if (type.Scope is AssemblyNameReference scope && scope.Name == "0Harmony" && scope.Version.Major == 1)
            {
                Type targetType = this.GetMappedType(type);
                replaceWith(module.ImportReference(targetType));
                this.MarkRewritten();
                this.ReplacedTypes = true;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            // rewrite Harmony 1.x methods to Harmony 2.0
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            if (this.TryRewriteMethodsToFacade(module, methodRef))
                return true;

            // rewrite renamed fields
            FieldReference fieldRef = RewriteHelper.AsFieldReference(instruction);
            if (fieldRef != null)
            {
                if (fieldRef.DeclaringType.FullName == "HarmonyLib.HarmonyMethod" && fieldRef.Name == "prioritiy")
                    fieldRef.Name = nameof(HarmonyMethod.priority);
            }

            return false;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Rewrite methods to use Harmony facades if needed.</summary>
        /// <param name="module">The assembly module containing the method reference.</param>
        /// <param name="methodRef">The method reference to map.</param>
        private bool TryRewriteMethodsToFacade(ModuleDefinition module, MethodReference methodRef)
        {
            if (!this.ReplacedTypes)
                return false; // not Harmony (or already using Harmony 2.0)

            // get facade type
            Type toType;
            switch (methodRef?.DeclaringType.FullName)
            {
                case "HarmonyLib.Harmony":
                    toType = typeof(HarmonyInstanceFacade);
                    break;

                case "HarmonyLib.AccessTools":
                    toType = typeof(AccessToolsFacade);
                    break;

                case "HarmonyLib.HarmonyMethod":
                    toType = typeof(HarmonyMethodFacade);
                    break;

                default:
                    return false;
            }

            // map if there's a matching method
            if (RewriteHelper.HasMatchingSignature(toType, methodRef))
            {
                methodRef.DeclaringType = module.ImportReference(toType);
                return true;
            }

            return false;
        }

        /// <summary>Get an equivalent Harmony 2.x type.</summary>
        /// <param name="type">The Harmony 1.x method.</param>
        private Type GetMappedType(TypeReference type)
        {
            // main Harmony object
            if (type.FullName == "Harmony.HarmonyInstance")
                return typeof(Harmony);

            // other objects
            string fullName = type.FullName.Replace("Harmony.", "HarmonyLib.");
            string targetName = typeof(Harmony).AssemblyQualifiedName.Replace(typeof(Harmony).FullName, fullName);
            return Type.GetType(targetName, throwOnError: true);
        }
    }
}
