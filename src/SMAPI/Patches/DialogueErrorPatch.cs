using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI.Framework.Patching;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;
#if HARMONY_2
using HarmonyLib;
using StardewModdingAPI.Framework;
#else
using System.Reflection;
using Harmony;
#endif

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


        /*********
        ** Accessors
        *********/
        /// <inheritdoc />
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


        /// <inheritdoc />
#if HARMONY_2
        public void Apply(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Constructor(typeof(Dialogue), new[] { typeof(string), typeof(NPC) }),
                finalizer: new HarmonyMethod(this.GetType(), nameof(DialogueErrorPatch.Finalize_Dialogue_Constructor))
            );
            harmony.Patch(
                original: AccessTools.Property(typeof(NPC), nameof(NPC.CurrentDialogue)).GetMethod,
                finalizer: new HarmonyMethod(this.GetType(), nameof(DialogueErrorPatch.Finalize_NPC_CurrentDialogue))
            );
        }
#else
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
#endif


        /*********
        ** Private methods
        *********/
#if HARMONY_2
        /// <summary>The method to call after the Dialogue constructor.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="masterDialogue">The dialogue being parsed.</param>
        /// <param name="speaker">The NPC for which the dialogue is being parsed.</param>
        /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
        /// <returns>Returns the exception to throw, if any.</returns>
        private static Exception Finalize_Dialogue_Constructor(Dialogue __instance, string masterDialogue, NPC speaker, Exception __exception)
        {
            if (__exception != null)
            {
                // log message
                string name = !string.IsNullOrWhiteSpace(speaker?.Name) ? speaker.Name : null;
                DialogueErrorPatch.MonitorForGame.Log($"Failed parsing dialogue string{(name != null ? $" for {name}" : "")}:\n{masterDialogue}\n{__exception.GetLogSummary()}", LogLevel.Error);

                // set default dialogue
                IReflectedMethod parseDialogueString = DialogueErrorPatch.Reflection.GetMethod(__instance, "parseDialogueString");
                IReflectedMethod checkForSpecialDialogueAttributes = DialogueErrorPatch.Reflection.GetMethod(__instance, "checkForSpecialDialogueAttributes");
                parseDialogueString.Invoke("...");
                checkForSpecialDialogueAttributes.Invoke();
            }

            return null;
        }

        /// <summary>The method to call after <see cref="NPC.CurrentDialogue"/>.</summary>
        /// <param name="__instance">The instance being patched.</param>
        /// <param name="__result">The return value of the original method.</param>
        /// <param name="__exception">The exception thrown by the wrapped method, if any.</param>
        /// <returns>Returns the exception to throw, if any.</returns>
        private static Exception Finalize_NPC_CurrentDialogue(NPC __instance, ref Stack<Dialogue> __result, Exception __exception)
        {
            if (__exception == null)
                return null;

            DialogueErrorPatch.MonitorForGame.Log($"Failed loading current dialogue for NPC {__instance.Name}:\n{__exception.GetLogSummary()}", LogLevel.Error);
            __result = new Stack<Dialogue>();

            return null;
        }
#else

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

            // replicate base constructor
            __instance.dialogues ??= new List<string>();

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
            const string key = nameof(Before_NPC_CurrentDialogue);
            if (!PatchHelper.StartIntercept(key))
                return true;

            try
            {
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
                PatchHelper.StopIntercept(key);
            }
        }
#endif
    }
}
