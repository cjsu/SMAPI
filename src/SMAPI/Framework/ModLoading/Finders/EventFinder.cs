using Mono.Cecil;
using Mono.Cecil.Cil;
using StardewModdingAPI.Framework.ModLoading.Framework;

namespace StardewModdingAPI.Framework.ModLoading.Finders
{
    /// <summary>Finds incompatible CIL instructions that reference a given event.</summary>
    internal class EventFinder : BaseInstructionHandler
    {
        /*********
        ** Fields
        *********/
        /// <summary>The full type name for which to find references.</summary>
        private readonly string FullTypeName;

        /// <summary>The event name for which to find references.</summary>
        private readonly string EventName;

        /// <summary>The result to return for matching instructions.</summary>
        private readonly InstructionHandleResult Result;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="fullTypeName">The full type name for which to find references.</param>
        /// <param name="eventName">The event name for which to find references.</param>
        /// <param name="result">The result to return for matching instructions.</param>
        public EventFinder(string fullTypeName, string eventName, InstructionHandleResult result)
            : base(defaultPhrase: $"{fullTypeName}.{eventName} event")
        {
            this.FullTypeName = fullTypeName;
            this.EventName = eventName;
            this.Result = result;
        }

        /// <inheritdoc />
        public override bool Handle(ModuleDefinition module, ILProcessor cil, Instruction instruction)
        {
            if (!this.Flags.Contains(this.Result) && this.IsMatch(instruction))
                this.MarkFlag(this.Result);

            return false;
        }


        /*********
        ** Protected methods
        *********/
        /// <summary>Get whether a CIL instruction matches.</summary>
        /// <param name="instruction">The IL instruction.</param>
        protected bool IsMatch(Instruction instruction)
        {
            MethodReference methodRef = RewriteHelper.AsMethodReference(instruction);
            return
                methodRef != null
                && methodRef.DeclaringType.FullName == this.FullTypeName
                && (methodRef.Name == "add_" + this.EventName || methodRef.Name == "remove_" + this.EventName);
        }
    }
}
