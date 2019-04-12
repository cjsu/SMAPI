using System;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI.Framework;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;

namespace SMDroid
{
    public class ModEntry : ModHooks
    {

        private SCore core;
        /// <summary>Whether the next content manager requested by the game will be for <see cref="Game1.content"/>.</summary>
        private bool NextContentManagerIsMain;
        /// <summary>SMAPI's content manager.</summary>
        private ContentCoordinator ContentCore { get; set; }


        public ModEntry()
        {
            this.core = new SCore("/sdcard/SMDroid/Mods", false);
        }
        public override LocalizedContentManager OnGame1_CreateContentManager(IServiceProvider serviceProvider, string rootDirectory)
        {
            // Game1._temporaryContent initialising from SGame constructor
            // NOTE: this method is called before the SGame constructor runs. Don't depend on anything being initialised at this point.
            if (this.ContentCore == null)
            {
                this.ContentCore = new ContentCoordinator(serviceProvider, rootDirectory, Thread.CurrentThread.CurrentUICulture, SGame.ConstructorHack.Monitor, SGame.ConstructorHack.Reflection, SGame.ConstructorHack.JsonHelper, SGame.OnLoadingFirstAsset ?? SGame.ConstructorHack?.OnLoadingFirstAsset);
                this.NextContentManagerIsMain = true;
                this.core.RunInteractively(this.ContentCore);
                SGame.printLog("ROOT Directory:" + rootDirectory);
                return this.ContentCore.CreateGameContentManager("Game1._temporaryContent");
            }

            // Game1.content initialising from LoadContent
            if (this.NextContentManagerIsMain)
            {
                this.NextContentManagerIsMain = false;
                return this.ContentCore.MainContentManager;
            }

            // any other content manager
            return this.ContentCore.CreateGameContentManager("(generated)");
        }
        public override void OnGame1_Update(GameTime time)
        {
            this.core.GameInstance.Update(time);
        }
        public override void OnGame1_Draw(GameTime time, RenderTarget2D toBuffer)
        {
            this.core.GameInstance.Draw(time, toBuffer);
        }
        public override void OnGame1_NewDayAfterFade(Action action)
        {
            this.core.GameInstance.OnNewDayAfterFade();
            base.OnGame1_NewDayAfterFade(action);
        }
        public override bool OnObject_canBePlacedHere(StardewValley.Object __instance, GameLocation location, Vector2 tile, ref bool __result)
        {
            return this.core.GameInstance.OnObjectCanBePlacedHere(__instance, location, tile, ref __result);
        }
        public override void OnObject_isIndexOkForBasicShippedCategory(int index, ref bool __result)
        {
            this.core.GameInstance.OnObjectIsIndexOkForBasicShippedCategory(index, ref __result);
        }
        public override bool OnObject_checkForAction(StardewValley.Object __instance)
        {
            return this.core.GameInstance.OnObjectCheckForAction(__instance);
        }
    }
}
