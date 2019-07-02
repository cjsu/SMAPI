using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Framework.ModLoading;
using StardewModdingAPI.Framework.ModLoading.Finders;
using StardewModdingAPI.Framework.ModLoading.Rewriters;
using StardewModdingAPI.Framework.RewriteFacades;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace StardewModdingAPI.Metadata
{
    /// <summary>Provides CIL instruction handlers which rewrite mods for compatibility and throw exceptions for incompatible code.</summary>
    internal class InstructionMetadata
    {
        /*********
        ** Fields
        *********/
        /// <summary>The assembly names to which to heuristically detect broken references.</summary>
        /// <remarks>The current implementation only works correctly with assemblies that should always be present.</remarks>
        private readonly string[] ValidateReferencesToAssemblies = { "StardewModdingAPI", "Stardew Valley", "StardewValley", "Netcode" };

        private readonly IMonitor Monitor;

        public InstructionMetadata(IMonitor monitor)
        {
            this.Monitor = monitor;
        }


        /*********
        ** Public methods
        *********/
        /// <summary>Get rewriters which detect or fix incompatible CIL instructions in mod assemblies.</summary>
        /// <param name="paranoidMode">Whether to detect paranoid mode issues.</param>
        public IEnumerable<IInstructionHandler> GetHandlers(bool paranoidMode)
        {
            /****
            ** rewrite CIL to fix incompatible code
            ****/
            // rewrite for crossplatform compatibility
            yield return new MethodParentRewriter(typeof(SpriteBatch), typeof(SpriteBatchMethods), onlyIfPlatformChanged: true);

            //isRaining and isDebrisWeather fix 75% done.
            yield return new TypeFieldToAnotherTypeFieldRewriter(typeof(Game1), typeof(RainManager), "isRaining", "Instance", this.Monitor);
            yield return new TypeFieldToAnotherTypeFieldRewriter(typeof(Game1), typeof(WeatherDebrisManager), "isDebrisWeather", "Instance", this.Monitor);
            yield return new TypeFieldToAnotherTypeFieldRewriter(typeof(GameLocation), typeof(DebrisManager), "debris", "Instance", this.Monitor, "debrisNetCollection", false);
            yield return new TypeFieldToAnotherTypeFieldRewriter(typeof(Game1), typeof(WeatherDebrisManager), "debrisWeather", "Instance", this.Monitor, "weatherDebrisList");
            yield return new TypeFieldToAnotherTypeFieldRewriter(typeof(Game1), typeof(Game1Methods), "rainDrops", "Instance", this.Monitor, null, false, true);
            yield return new TypeFieldToAnotherTypeFieldRewriter(typeof(Game1), typeof(Game1Methods), "onScreenMenus", "", this.Monitor, null, false, true);

            //Method Rewrites
            yield return new MethodParentRewriter(typeof(Game1), typeof(Game1Methods));
            yield return new MethodParentRewriter(typeof(Farmer), typeof(FarmerMethods));
            yield return new MethodParentRewriter(typeof(IClickableMenu), typeof(IClickableMenuMethods));
            yield return new MethodParentRewriter(typeof(FarmerRenderer), typeof(FarmerRendererMethods));
            yield return new MethodParentRewriter(typeof(SpriteText), typeof(SpriteTextMethods));
            yield return new MethodParentRewriter(typeof(NPC), typeof(NPCMethods));

            //Constructor Rewrites
            yield return new MethodParentRewriter(typeof(HUDMessage), typeof(HUDMessageMethods));
            yield return new MethodParentRewriter(typeof(MapPage), typeof(MapPageMethods));
            yield return new MethodParentRewriter(typeof(TextBox), typeof(TextBoxMethods));
            yield return new MethodParentRewriter(typeof(WeatherDebris), typeof(WeatherDebrisMethods));
            yield return new MethodParentRewriter(typeof(Debris), typeof(DebrisMethods));

            //Field Rewriters
            yield return new FieldReplaceRewriter(typeof(ItemGrabMenu), "context", "specialObject");
            yield return new FieldReplaceRewriter(typeof(FarmerTeam), "demolishLock", "buildingLock");

            // rewrite for Stardew Valley 1.3
            yield return new StaticFieldToConstantRewriter<int>(typeof(Game1), "tileSize", Game1.tileSize);
            yield return new FieldToPropertyRewriter(typeof(Game1), "player");
            yield return new FieldToPropertyRewriter(typeof(Game1), "currentLocation");
            yield return new FieldToPropertyRewriter(typeof(Character), "currentLocation");
            yield return new FieldToPropertyRewriter(typeof(Farmer), "currentLocation");
            yield return new FieldToPropertyRewriter(typeof(Game1), "gameMode");
            yield return new FieldToPropertyRewriter(typeof(Game1), "currentMinigame");
            yield return new FieldToPropertyRewriter(typeof(Game1), "activeClickableMenu");
            yield return new FieldToPropertyRewriter(typeof(Game1), "stats");

            /****
            ** detect mod issues
            ****/
            // detect broken code
            yield return new ReferenceToMissingMemberFinder(this.ValidateReferencesToAssemblies);
            yield return new ReferenceToMemberWithUnexpectedTypeFinder(this.ValidateReferencesToAssemblies);

            /****
            ** detect code which may impact game stability
            ****/
            yield return new TypeFinder("Harmony.HarmonyInstance", InstructionHandleResult.DetectedGamePatch);
            yield return new TypeFinder("System.Runtime.CompilerServices.CallSite", InstructionHandleResult.DetectedDynamic);
            yield return new FieldFinder(typeof(SaveGame).FullName, nameof(SaveGame.serializer), InstructionHandleResult.DetectedSaveSerialiser);
            yield return new FieldFinder(typeof(SaveGame).FullName, nameof(SaveGame.farmerSerializer), InstructionHandleResult.DetectedSaveSerialiser);
            yield return new FieldFinder(typeof(SaveGame).FullName, nameof(SaveGame.locationSerializer), InstructionHandleResult.DetectedSaveSerialiser);
            yield return new EventFinder(typeof(ISpecialisedEvents).FullName, nameof(ISpecialisedEvents.UnvalidatedUpdateTicked), InstructionHandleResult.DetectedUnvalidatedUpdateTick);
            yield return new EventFinder(typeof(ISpecialisedEvents).FullName, nameof(ISpecialisedEvents.UnvalidatedUpdateTicking), InstructionHandleResult.DetectedUnvalidatedUpdateTick);
#if !SMAPI_3_0_STRICT
            yield return new EventFinder(typeof(SpecialisedEvents).FullName, nameof(SpecialisedEvents.UnvalidatedUpdateTick), InstructionHandleResult.DetectedUnvalidatedUpdateTick);
#endif

            /****
            ** detect paranoid issues
            ****/
            if (paranoidMode)
            {
                // filesystem access
                yield return new TypeFinder(typeof(System.IO.File).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileStream).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.Directory).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.DirectoryInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.DriveInfo).FullName, InstructionHandleResult.DetectedFilesystemAccess);
                yield return new TypeFinder(typeof(System.IO.FileSystemWatcher).FullName, InstructionHandleResult.DetectedFilesystemAccess);

                // shell access
                yield return new TypeFinder(typeof(System.Diagnostics.Process).FullName, InstructionHandleResult.DetectedShellAccess);
            }
        }
    }
}
