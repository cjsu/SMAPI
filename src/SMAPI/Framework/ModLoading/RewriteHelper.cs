using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace StardewModdingAPI.Framework.ModLoading
{
    /// <summary>Provides helper methods for field rewriters.</summary>
    internal static class RewriteHelper
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get the field reference from an instruction if it matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        public static FieldReference AsFieldReference(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld
                ? (FieldReference)instruction.Operand
                : null;
        }

        /// <summary>Get the method reference from an instruction if it matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        public static MethodReference AsMethodReference(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt
                ? (MethodReference)instruction.Operand
                : null;
        }

        /// <summary>Get whether a type matches a type reference.</summary>
        /// <param name="type">The defined type.</param>
        /// <param name="reference">The type reference.</param>
        public static bool IsSameType(Type type, TypeReference reference)
        {
            // same namespace & name
            if (type.Namespace != reference.Namespace || type.Name != reference.Name)
                return false;

            // same generic parameters
            if (type.IsGenericType)
            {
                if (!reference.IsGenericInstance)
                    return false;

                Type[] defGenerics = type.GetGenericArguments();
                TypeReference[] refGenerics = ((GenericInstanceType)reference).GenericArguments.ToArray();
                if (defGenerics.Length != refGenerics.Length)
                    return false;
                for (int i = 0; i < defGenerics.Length; i++)
                {
                    if (!RewriteHelper.IsSameType(defGenerics[i], refGenerics[i]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>Get whether a method definition matches the signature expected by a method reference.</summary>
        /// <param name="definition">The method definition.</param>
        /// <param name="reference">The method reference.</param>
        public static bool HasMatchingSignature(MethodInfo definition, MethodReference reference)
        {
            // same name
            if (definition.Name != reference.Name)
                return false;

            // same arguments
            ParameterInfo[] definitionParameters = definition.GetParameters();
            ParameterDefinition[] referenceParameters = reference.Parameters.ToArray();
            if (referenceParameters.Length != definitionParameters.Length)
                return false;
            for (int i = 0; i < referenceParameters.Length; i++)
            {
                if (!RewriteHelper.IsSameType(definitionParameters[i].ParameterType, referenceParameters[i].ParameterType))
                    return false;
            }
            return true;
        }

        /// <summary>Get whether a type has a method whose signature matches the one expected by a method reference.</summary>
        /// <param name="type">The type to check.</param>
        /// <param name="reference">The method reference.</param>
        public static bool HasMatchingSignature(Type type, MethodReference reference)
        {
            return type
                .GetMethods(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public)
                .Any(method => RewriteHelper.HasMatchingSignature(method, reference));
        }

        /// <summary>Determine whether this type ID has a placeholder such as !0.</summary>
        /// <param name="typeID">The type to check.</param>
        /// <returns>true if the type ID contains a placeholder, false if not.</returns>
        public static bool HasPlaceholder(string typeID)
        {
            return typeID.Contains("!0");
        }

        /// <summary> returns whether this type ID is a placeholder, i.e., it begins with "!".</summary>
        /// <param name="symbol">The symbol to validate.</param>
        /// <returns>true if the symbol is a placeholder, false if not</returns>
        public static bool IsPlaceholder(string symbol)
        {
            return symbol.StartsWith("!");
        }

        /// <summary>Determine whether two type IDs look like the same type, accounting for placeholder values such as !0.</summary>
        /// <param name="typeA">The type ID to compare.</param>
        /// <param name="typeB">The other type ID to compare.</param>
        /// <returns>true if the type IDs look like the same type, false if not.</returns>
        public static bool LooksLikeSameType(string typeA, string typeB)
        {
            string placeholderType, actualType = "";

            if (RewriteHelper.HasPlaceholder(typeA))
            {
                placeholderType = typeA;
                actualType = typeB;
            } else if (RewriteHelper.HasPlaceholder(typeB))
            {
                placeholderType = typeB;
                actualType = typeA;
            } else
            {
                return typeA == typeB;
            }

            return RewriteHelper.PlaceholderTypeValidates(placeholderType, actualType);
        }

        protected class SymbolLocation
        {
            public string symbol;
            public int depth;

            public SymbolLocation(string symbol, int depth)
            {
                this.symbol = symbol;
                this.depth = depth;
            }
        }

        private static List<char> symbolBoundaries = new List<char>{'<', '>', ','};

        /// <summary> Traverses and parses out symbols from a type which does not contain placeholder values.</summary>
        /// <param name="type">The type to traverse.</param>
        /// <param name="typeSymbols">A List in which to store the parsed symbols.</param>
        private static void TraverseActualType(string type, List<SymbolLocation> typeSymbols)
        {
            int depth = 0;
            string symbol = "";

            foreach (char c in type)
            {
                if (RewriteHelper.symbolBoundaries.Contains(c))
                {
                    typeSymbols.Add(new SymbolLocation(symbol, depth));
                    symbol = "";
                    switch (c) {
                        case '<':
                            depth++;
                            break;
                        case '>':
                            depth--;
                            break;
                        default:
                            break;
                    }
                }
                else
                    symbol += c;
            }
        }

        /// <summary> Determines whether two symbols in a type ID match, accounting for placeholders such as !0.</summary>
        /// <param name="symbolA">A symbol in a typename which contains placeholders.</param>
        /// <param name="symbolB">A symbol in a typename which does not contain placeholders.</param>
        /// <param name="placeholderMap">A dictionary containing a mapping of placeholders to concrete types.</param>
        /// <returns>true if the symbols match, false if not.</returns>
        private static bool SymbolsMatch(SymbolLocation symbolA, SymbolLocation symbolB, Dictionary<string, string> placeholderMap)
        {
            System.Console.Write($"comparing {symbolA.symbol} at depth {symbolA.depth} to {symbolB.symbol} at {symbolB.depth}");
            if (symbolA.depth != symbolB.depth)
                return false;

            if (!RewriteHelper.IsPlaceholder(symbolA.symbol)) {
                return symbolA.symbol == symbolB.symbol;
            }

            if (placeholderMap.ContainsKey(symbolA.symbol))
            {
                return placeholderMap[symbolA.symbol] == symbolB.symbol;
            }

            placeholderMap[symbolA.symbol] = symbolB.symbol;

            return true;
        }

        /// <summary> Determines whether a type which has placeholders correctly resolves to the concrete type provided. </summary>
        /// <param name="type">A type containing placeholders such as !0.</param>
        /// <param name="typeSymbols">The list of symbols extracted from the concrete type.</param>
        /// <returns>true if the type resolves correctly, false if not.</returns>
        private static bool PlaceholderTypeResolvesToActualType(string type, List<SymbolLocation> typeSymbols)
        {
            Dictionary<string, string> placeholderMap = new Dictionary<string, string>();

            int depth = 0, symbolCount = 0;
            string symbol = "";

            foreach (char c in type)
            {
                if (symbolBoundaries.Contains(c))
                {
                    bool match = RewriteHelper.SymbolsMatch(new SymbolLocation(symbol, depth), typeSymbols[symbolCount], placeholderMap);
                    System.Console.Write($"match: {match}");
                    if (typeSymbols.Count <= symbolCount ||
                        !match)
                        return false;

                    symbolCount++;
                    symbol = "";
                    switch (c)
                    {
                        case '<':
                            depth++;
                            break;
                        case '>':
                            depth--;
                            break;
                        default:
                            break;
                    }
                }
                else
                    symbol += c;
            }

            return true;
        }

        /// <summary>Determines whether a type with placeholders in it matches a type without placeholders.</summary>
        /// <param name="placeholderType">The type with placeholders in it.</param>
        /// <param name="actualType">The type without placeholders.</param>
        /// <returns>true if the placeholder type can resolve to the actual type, false if not.</returns>
        private static bool PlaceholderTypeValidates(string placeholderType, string actualType) {
            List<SymbolLocation> typeSymbols = new List<SymbolLocation>();

            RewriteHelper.TraverseActualType(actualType, typeSymbols);
            return PlaceholderTypeResolvesToActualType(placeholderType, typeSymbols);
        }
    }
}
