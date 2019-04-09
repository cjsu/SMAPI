using Microsoft.Xna.Framework;
using SkullCavernElevator.SkullCavernElevator;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using xTile;
using xTile.Tiles;

namespace SkullCavernElevator
{
    class ModEntry : StardewModdingAPI.Mod
    {
        // Fields
        private IModHelper helper;
        private ModConfig config;

        public override void Entry(IModHelper helper)
        {
            this.helper = helper;
            Helper.Events.Player.Warped += MineEvents_MineLevelChanged;
            Helper.Events.Display.MenuChanged += MenuChanged;
            Helper.Events.GameLoop.SaveLoaded += SetUpSkullCave;
            this.config = helper.ReadConfig<ModConfig>();
        }
        private Vector2 findLadder(MineShaft ms)
        {
            Map map = ms.map;
            for (int i = 0; i < map.GetLayer("Buildings").LayerHeight; i++)
            {
                for (int j = 0; j < map.GetLayer("Buildings").LayerWidth; j++)
                {
                    if ((map.GetLayer("Buildings").Tiles[j, i] != null) && (map.GetLayer("Buildings").Tiles[j, i].TileIndex == 0x73))
                    {
                        return new Vector2((float)j, (float)(i + 1));
                    }
                }
            }
            return this.helper.Reflection.GetField<Vector2>(ms, "tileBeneathLadder", true).GetValue();
        }
        private void MenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!(e.NewMenu is MineElevatorMenu) || Game1.currentLocation.Name == "Mine" || e.NewMenu is MyElevatorMenu || e.NewMenu is MyElevatorMenuWithScrollbar)
            {
                return;
            }
            if (Game1.currentLocation is MineShaft)
            {
                MineShaft mineShaft = Game1.currentLocation as MineShaft;
                if (mineShaft != null && mineShaft.mineLevel <= 120)
                {
                    return;
                }
            }
            if (Game1.player.deepestMineLevel > 120 + 121 * config.elevatorStep)
            {
                Game1.activeClickableMenu = (new MyElevatorMenuWithScrollbar(config.elevatorStep, config.difficulty));
            }
            else
            {
                Game1.activeClickableMenu = (new MyElevatorMenu(config.elevatorStep, config.difficulty));
            }
        }
        private void MineEvents_MineLevelChanged(object sender, WarpedEventArgs e)
        {
            MineShaft shaft;
            if (((shaft = e.NewLocation as MineShaft) != null) && e.IsLocalPlayer)
            {
                base.Monitor.Log("Current lowest minelevel of player " + Game1.player.deepestMineLevel, LogLevel.Debug);
                base.Monitor.Log("Value of MineShaft.lowestMineLevel " + MineShaft.lowestLevelReached, LogLevel.Debug);
                base.Monitor.Log("Value of current mineShaft level " + shaft.mineLevel, LogLevel.Debug);
                if ((Game1.hasLoadedGame && (Game1.mine != null)) && (((((Game1.CurrentMineLevel - 120) % this.config.elevatorStep) == 0) && (Game1.CurrentMineLevel > 120)) && (Game1.currentLocation is MineShaft)))
                {
                    MineShaft currentLocation = Game1.currentLocation as MineShaft;
                    TileSheet tileSheet = Game1.getLocationFromName("Mine").map.GetTileSheet("untitled tile sheet");
                    currentLocation.map.AddTileSheet(new TileSheet("z_path_objects_custom_sheet", currentLocation.map, tileSheet.ImageSource, tileSheet.SheetSize, tileSheet.TileSize));
                    currentLocation.map.DisposeTileSheets(Game1.mapDisplayDevice);
                    currentLocation.map.LoadTileSheets(Game1.mapDisplayDevice);
                    Vector2 vector1 = this.findLadder(currentLocation);
                    int tileX = ((int)vector1.X) + 1;
                    int tileY = ((int)vector1.Y) - 3;
                    typeof(MineShaft).GetMethods();
                    currentLocation.setMapTileIndex(tileX, tileY + 2, 0x70, "Buildings", 1);
                    currentLocation.setMapTileIndex(tileX, tileY + 1, 0x60, "Front", 1);
                    currentLocation.setMapTileIndex(tileX, tileY, 80, "Front", 1);
                    currentLocation.setMapTile(tileX, tileY, 80, "Front", "MineElevator", 1);
                    currentLocation.setMapTile(tileX, tileY + 1, 0x60, "Front", "MineElevator", 1);
                    currentLocation.setMapTile(tileX, tileY + 2, 0x70, "Buildings", "MineElevator", 1);
                    this.helper.Reflection.GetMethod(currentLocation, "prepareElevator", true).Invoke(new object[0]);
                    Point point = Utility.findTile(currentLocation, 80, "Buildings");
                    object[] objArray1 = new object[] { "x ", point.X, " y ", point.Y };
                    base.Monitor.Log(string.Concat(objArray1), LogLevel.Debug);
                }
            }
        }
        private void SetUpSkullCave(object sender, SaveLoadedEventArgs e)
        {
            if (Game1.hasLoadedGame && (Game1.CurrentEvent == null))
            {
                GameLocation location = Game1.getLocationFromName("SkullCave");
                TileSheet tileSheet = Game1.getLocationFromName("Mine").map.GetTileSheet("untitled tile sheet");
                location.map.AddTileSheet(new TileSheet("z_path_objects_custom_sheet", location.map, tileSheet.ImageSource, tileSheet.SheetSize, tileSheet.TileSize));
                location.map.DisposeTileSheets(Game1.mapDisplayDevice);
                location.map.LoadTileSheets(Game1.mapDisplayDevice);
                location.setMapTileIndex(4, 3, 0x70, "Buildings", 2);
                location.setMapTileIndex(4, 2, 0x60, "Front", 2);
                location.setMapTileIndex(4, 1, 80, "Front", 2);
                location.setMapTile(4, 3, 0x70, "Buildings", "MineElevator", 2);
                location.setMapTile(4, 2, 0x60, "Front", "MineElevator", 2);
                location.setMapTile(4, 1, 80, "Front", "MineElevator", 2);
            }
        }
    }




}

