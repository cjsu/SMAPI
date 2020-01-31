using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Harmony;
using StardewModdingAPI.Framework.Patching;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;

namespace StardewModdingAPI.Patches
{
    /// <summary>A Harmony patch for the <see cref="Dialogue"/> constructor which intercepts invalid dialogue lines and logs an error instead of crashing.</summary>
    /// <remarks>Patch methods must be static for Harmony to work correctly. See the Harmony documentation before renaming patch arguments.</remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    [SuppressMessage("ReSharper", "IdentifierTypo", Justification = "Argument names are defined by Harmony and methods are named for clarity.")]
    internal class DialogueErrorPatch : IHarmonyPatch
    {
        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file on behalf of the game.</summary>
        private static IMonitor MonitorForGame;

        /// <summary>Simplifies access to private code.</summary>
        private static Reflector Reflection;

        /// <summary>Whether the <see cref="NPC.CurrentDialogue"/> getter is currently being intercepted.</summary>
        private static bool IsInterceptingCurrentDialogue;


        /*********
        ** Accessors
        *********/
        /// <summary>A unique name for this patch.</summary>
        public string Name => nameof(DialogueErrorPatch);


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="monitorForGame">Writes messages to the console and log file on behalf of the game.</param>
        /// <param name="reflector">Simplifies access to private code.</param>
        public DialogueErrorPatch(IMonitor monitorForGame, Reflector reflector)
        {
            DialogueErrorPatch.MonitorForGame = monitorForGame;
            DialogueErrorPatch.Reflection = reflector;
        }


        /// <summary>Apply the Harmony patch.</summary>
        /// <param name="harmony">The Harmony instance.</param>
        public void Apply(HarmonyInstance harmony)
        {
            harmony.Patch(
                original: AccessTools.Constructor(typeof(Dialogue), new[] { typeof(string), typeof(NPC) }),
                prefix: new HarmonyMethod(this.GetType(), nameof(DialogueErrorPatch.Before_Dialogue_Constructor))
            );
            harmony.Patch(
                original: AccessTools.Property(typeof(NPC), nameof(NPC.CurrentDialogue)).GetMethod,
                prefix: new HarmonyMethod(this.GetType(), nameof(DialogueErrorPatch.Before_NPC_CurrentDialogue))
            );
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method to call instead of the Dialogue constructor.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="masterDialogue">The dialogue being parsed.</param>
        /// <param name="speaker">The NPC for which the dialogue is being parsed.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_Dialogue_Constructor(Dialogue __instance, string masterDialogue, NPC speaker)
        {
            // get private members
            bool nameArraysTranslated = DialogueErrorPatch.Reflection.GetField<bool>(typeof(Dialogue), "nameArraysTranslated").GetValue();
            IReflectedMethod translateArraysOfStrings = DialogueErrorPatch.Reflection.GetMethod(typeof(Dialogue), "TranslateArraysOfStrings");
            IReflectedMethod parseDialogueString = DialogueErrorPatch.Reflection.GetMethod(__instance, "parseDialogueString");
            IReflectedMethod checkForSpecialDialogueAttributes = DialogueErrorPatch.Reflection.GetMethod(__instance, "checkForSpecialDialogueAttributes");
            IReflectedField<List<string>> dialogues = DialogueErrorPatch.Reflection.GetField<List<string>>(__instance, "dialogues");

            // replicate base constructor
            if (dialogues.GetValue() == null)
                dialogues.SetValue(new List<string>());

            // duplicate code with try..catch
            try
            {
                if (!nameArraysTranslated)
                    translateArraysOfStrings.Invoke();
                __instance.speaker = speaker;
                parseDialogueString.Invoke(masterDialogue);
                checkForSpecialDialogueAttributes.Invoke();
            }
            catch (Exception baseEx) when (baseEx.InnerException is TargetInvocationException invocationEx && invocationEx.InnerException is Exception ex)
            {
                string name = !string.IsNullOrWhiteSpace(speaker?.Name) ? speaker.Name : null;
                DialogueErrorPatch.MonitorForGame.Log($"Failed parsing dialogue string{(name != null ? $" for {name}" : "")}:\n{masterDialogue}\n{ex}", LogLevel.Error);

                parseDialogueString.Invoke("...");
                checkForSpecialDialogueAttributes.Invoke();
            }

            return false;
        }

        /// <summary>The method to call instead of <see cref="NPC.CurrentDialogue"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The return value of the original method.</param>
        /// <param name="__originalMethod">The method being wrapped.</param>
        /// <returns>Returns whether to execute the original method.</returns>
        private static bool Before_NPC_CurrentDialogue(NPC __instance, ref Stack<Dialogue> __result, MethodInfo __originalMethod)
        {
            if (DialogueErrorPatch.IsInterceptingCurrentDialogue)
                return true;

            try
            {
                DialogueErrorPatch.IsInterceptingCurrentDialogue = true;
                __result = (Stack<Dialogue>)__originalMethod.Invoke(__instance, new object[0]);
                return false;
            }
            catch (TargetInvocationException ex)
            {
                DialogueErrorPatch.MonitorForGame.Log($"Failed loading current dialogue for NPC {__instance.Name}:\n{ex.InnerException ?? ex}", LogLevel.Error);
                __result = new Stack<Dialogue>();
                return false;
            }
            finally
            {
                DialogueErrorPatch.IsInterceptingCurrentDialogue = false;
            }
        }
    }
}
