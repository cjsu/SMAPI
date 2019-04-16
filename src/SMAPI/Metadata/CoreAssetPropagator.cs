using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Framework.Reflection;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Projectiles;
using StardewValley.TerrainFeatures;
using xTile;
using xTile.Tiles;

namespace StardewModdingAPI.Metadata
{
    /// <summary>Propagates changes to core assets to the game state.</summary>
    internal class CoreAssetPropagator
    {
        /*********
        ** Fields
        *********/
        /// <summary>Normalises an asset key to match the cache key.</summary>
        private readonly Func<string, string> GetNormalisedPath;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly Reflector Reflection;

        /// <summary>Encapsulates monitoring and logging.</summary>
        private readonly IMonitor Monitor;


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the core asset data.</summary>
        /// <param name="getNormalisedPath">Normalises an asset key to match the cache key.</param>
        /// <param name="reflection">Simplifies access to private code.</param>
        /// <param name="monitor">Encapsulates monitoring and logging.</param>
        public CoreAssetPropagator(Func<string, string> getNormalisedPath, Reflector reflection, IMonitor monitor)
        {
            this.GetNormalisedPath = getNormalisedPath;
            this.Reflection = reflection;
            this.Monitor = monitor;
        }

        /// <summary>Reload one of the game's core assets (if applicable).</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <param name="type">The asset type to reload.</param>
        /// <returns>Returns whether an asset was reloaded.</returns>
        public bool Propagate(LocalizedContentManager content, string key, Type type)
        {
            object result = this.PropagateImpl(content, key, type);
            if (result is bool b)
                return b;
            return result != null;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Reload one of the game's core assets (if applicable).</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <param name="type">The asset type to reload.</param>
        /// <returns>Returns whether an asset was loaded. The return value may be true or false, or a non-null value for true.</returns>
        private object PropagateImpl(LocalizedContentManager content, string key, Type type)
        {
            key = this.GetNormalisedPath(key);

            /****
            ** Special case: current map tilesheet
            ** We only need to do this for the current location, since tilesheets are reloaded when you enter a location.
            ** Just in case, we should still propagate by key even if a tilesheet is matched.
            ****/
            if (Game1.currentLocation?.map?.TileSheets != null)
            {
                foreach (TileSheet tilesheet in Game1.currentLocation.map.TileSheets)
                {
                    if (this.GetNormalisedPath(tilesheet.ImageSource) == key)
                        Game1.mapDisplayDevice.LoadTileSheet(tilesheet);
                }
            }

            /****
            ** Propagate map changes
            ****/
            if (type == typeof(Map))
            {
                bool anyChanged = false;
                foreach (GameLocation location in this.GetLocations())
                {
                    if (!string.IsNullOrWhiteSpace(location.mapPath.Value) && this.GetNormalisedPath(location.mapPath.Value) == key)
                    {
                        // reload map data
                        this.Reflection.GetMethod(location, "reloadMap").Invoke();
                        this.Reflection.GetMethod(location, "updateWarps").Invoke();

                        // reload doors
                        {
                            Type interiorDoorDictType = Type.GetType($"StardewValley.InteriorDoorDictionary, {Constants.GameAssemblyName}", throwOnError: true);
                            ConstructorInfo constructor = interiorDoorDictType.GetConstructor(new[] { typeof(GameLocation) });
                            if (constructor == null)
                                throw new InvalidOperationException("Can't reset location doors: constructor not found for InteriorDoorDictionary type.");
                            object instance = constructor.Invoke(new object[] { location });

                            this.Reflection.GetField<object>(location, "interiorDoors").SetValue(instance);
                        }

                        anyChanged = true;
                    }
                }
                return anyChanged;
            }

            /****
            ** Propagate by key
            ****/
            Reflector reflection = this.Reflection;
            switch (key.ToLower().Replace("/", "\\")) // normalised key so we can compare statically
            {
                /****
                ** Animals
                ****/
                case "animals\\cat":
                    return this.ReloadPetOrHorseSprites<Cat>(content, key);
                case "animals\\dog":
                    return this.ReloadPetOrHorseSprites<Dog>(content, key);
                case "animals\\horse":
                    return this.ReloadPetOrHorseSprites<Horse>(content, key);

                /****
                ** Buildings
                ****/
                case "buildings\\houses": // Farm
                    reflection.GetField<Texture2D>(typeof(Farm), nameof(Farm.houseTextures)).SetValue(content.Load<Texture2D>(key));
                    return true;

                /****
                ** Content\Characters\Farmer
                ****/
                case "characters\\farmer\\accessories": // Game1.loadContent
                    return FarmerRenderer.accessoriesTexture = content.Load<Texture2D>(key);

                case "characters\\farmer\\farmer_base": // Farmer
                    if (Game1.player == null || !Game1.player.IsMale)
                        return false;
                    return Game1.player.FarmerRenderer = new FarmerRenderer(key, Game1.player);

                case "characters\\farmer\\farmer_girl_base": // Farmer
                    if (Game1.player == null || Game1.player.IsMale)
                        return false;
                    return Game1.player.FarmerRenderer = new FarmerRenderer(key, Game1.player);

                case "characters\\farmer\\hairstyles": // Game1.loadContent
                    return FarmerRenderer.hairStylesTexture = content.Load<Texture2D>(key);

                case "characters\\farmer\\hats": // Game1.loadContent
                    return FarmerRenderer.hatsTexture = content.Load<Texture2D>(key);

                case "characters\\farmer\\shirts": // Game1.loadContent
                    return FarmerRenderer.shirtsTexture = content.Load<Texture2D>(key);

                /****
                ** Content\Data
                ****/
                case "data\\achievements": // Game1.loadContent
                    return Game1.achievements = content.Load<Dictionary<int, string>>(key);

                case "data\\bigcraftablesinformation": // Game1.loadContent
                    return Game1.bigCraftablesInformation = content.Load<Dictionary<int, string>>(key);

                case "data\\cookingrecipes": // CraftingRecipe.InitShared
                    return CraftingRecipe.cookingRecipes = content.Load<Dictionary<string, string>>(key);

                case "data\\craftingrecipes": // CraftingRecipe.InitShared
                    return CraftingRecipe.craftingRecipes = content.Load<Dictionary<string, string>>(key);

                case "data\\npcdispositions": // NPC constructor
                    return this.ReloadNpcDispositions(content, key);

                case "data\\npcgifttastes": // Game1.loadContent
                    return Game1.NPCGiftTastes = content.Load<Dictionary<string, string>>(key);

                case "data\\objectinformation": // Game1.loadContent
                    return Game1.objectInformation = content.Load<Dictionary<int, string>>(key);

                /****
                ** Content\Fonts
                ****/
                case "fonts\\spritefont1": // Game1.loadContent
                    return Game1.dialogueFont = content.Load<SpriteFont>(key);

                case "fonts\\smallfont": // Game1.loadContent
                    return Game1.smallFont = content.Load<SpriteFont>(key);

                case "fonts\\tinyfont": // Game1.loadContent
                    return Game1.tinyFont = content.Load<SpriteFont>(key);

                case "fonts\\tinyfontborder": // Game1.loadContent
                    return Game1.tinyFontBorder = content.Load<SpriteFont>(key);

                /****
                ** Content\Lighting
                ****/
                case "loosesprites\\lighting\\greenlight": // Game1.loadContent
                    return Game1.cauldronLight = content.Load<Texture2D>(key);

                case "loosesprites\\lighting\\indoorwindowlight": // Game1.loadContent
                    return Game1.indoorWindowLight = content.Load<Texture2D>(key);

                case "loosesprites\\lighting\\lantern": // Game1.loadContent
                    return Game1.lantern = content.Load<Texture2D>(key);

                case "loosesprites\\lighting\\sconcelight": // Game1.loadContent
                    return Game1.sconceLight = content.Load<Texture2D>(key);

                case "loosesprites\\lighting\\windowlight": // Game1.loadContent
                    return Game1.windowLight = content.Load<Texture2D>(key);

                /****
                ** Content\LooseSprites
                ****/
                case "loosesprites\\controllermaps": // Game1.loadContent
                    return Game1.controllerMaps = content.Load<Texture2D>(key);

                case "loosesprites\\cursors": // Game1.loadContent
                    return Game1.mouseCursors = content.Load<Texture2D>(key);

                case "loosesprites\\daybg": // Game1.loadContent
                    return Game1.daybg = content.Load<Texture2D>(key);

                case "loosesprites\\font_bold": // Game1.loadContent
                    return SpriteText.spriteTexture = content.Load<Texture2D>(key);

                case "loosesprites\\font_colored": // Game1.loadContent
                    return SpriteText.coloredTexture = content.Load<Texture2D>(key);

                case "loosesprites\\nightbg": // Game1.loadContent
                    return Game1.nightbg = content.Load<Texture2D>(key);

                case "loosesprites\\shadow": // Game1.loadContent
                    return Game1.shadowTexture = content.Load<Texture2D>(key);

                /****
                ** Content\Critters
                ****/
                case "tilesheets\\crops": // Game1.loadContent
                    return Game1.cropSpriteSheet = content.Load<Texture2D>(key);

                case "tilesheets\\debris": // Game1.loadContent
                    return Game1.debrisSpriteSheet = content.Load<Texture2D>(key);

                case "tilesheets\\emotes": // Game1.loadContent
                    return Game1.emoteSpriteSheet = content.Load<Texture2D>(key);

                case "tilesheets\\furniture": // Game1.loadContent
                    return Furniture.furnitureTexture = content.Load<Texture2D>(key);

                case "tilesheets\\projectiles": // Game1.loadContent
                    return Projectile.projectileSheet = content.Load<Texture2D>(key);

                case "tilesheets\\rain": // Game1.loadContent
                    return Game1.rainTexture = content.Load<Texture2D>(key);

                case "tilesheets\\tools": // Game1.ResetToolSpriteSheet
                    Game1.ResetToolSpriteSheet();
                    return true;

                case "tilesheets\\weapons": // Game1.loadContent
                    return Tool.weaponsTexture = content.Load<Texture2D>(key);

                /****
                ** Content\Maps
                ****/
                case "maps\\menutiles": // Game1.loadContent
                    return Game1.menuTexture = content.Load<Texture2D>(key);

                case "maps\\springobjects": // Game1.loadContent
                    return Game1.objectSpriteSheet = content.Load<Texture2D>(key);

                case "maps\\walls_and_floors": // Wallpaper
                    return Wallpaper.wallpaperTexture = content.Load<Texture2D>(key);

                /****
                ** Content\Minigames
                ****/
                case "minigames\\clouds": // TitleMenu
                    if (Game1.activeClickableMenu is TitleMenu)
                    {
                        reflection.GetField<Texture2D>(Game1.activeClickableMenu, "cloudsTexture").SetValue(content.Load<Texture2D>(key));
                        return true;
                    }
                    return false;

                case "minigames\\titlebuttons": // TitleMenu
                    if (Game1.activeClickableMenu is TitleMenu titleMenu)
                    {
                        Texture2D texture = content.Load<Texture2D>(key);
                        reflection.GetField<Texture2D>(titleMenu, "titleButtonsTexture").SetValue(texture);
                        foreach (TemporaryAnimatedSprite bird in reflection.GetField<List<TemporaryAnimatedSprite>>(titleMenu, "birds").GetValue())
                            bird.texture = texture;
                        return true;
                    }
                    return false;

                /****
                ** Content\TileSheets
                ****/
                case "tilesheets\\animations": // Game1.loadContent
                    return Game1.animations = content.Load<Texture2D>(key);

                case "tilesheets\\buffsicons": // Game1.loadContent
                    return Game1.buffsIcons = content.Load<Texture2D>(key);

                case "tilesheets\\bushes": // new Bush()
                    reflection.GetField<Lazy<Texture2D>>(typeof(Bush), "texture").SetValue(new Lazy<Texture2D>(() => content.Load<Texture2D>(key)));
                    return true;

                case "tilesheets\\craftables": // Game1.loadContent
                    return Game1.bigCraftableSpriteSheet = content.Load<Texture2D>(key);

                case "tilesheets\\fruittrees": // FruitTree
                    return FruitTree.texture = content.Load<Texture2D>(key);

                /****
                ** Content\TerrainFeatures
                ****/
                case "terrainfeatures\\flooring": // Flooring
                    return Flooring.floorsTexture = content.Load<Texture2D>(key);

                case "terrainfeatures\\hoedirt": // from HoeDirt
                    return HoeDirt.lightTexture = content.Load<Texture2D>(key);

                case "terrainfeatures\\hoedirtdark": // from HoeDirt
                    return HoeDirt.darkTexture = content.Load<Texture2D>(key);

                case "terrainfeatures\\hoedirtsnow": // from HoeDirt
                    return HoeDirt.snowTexture = content.Load<Texture2D>(key);

                case "terrainfeatures\\mushroom_tree": // from Tree
                    return this.ReloadTreeTextures(content, key, Tree.mushroomTree);

                case "terrainfeatures\\tree_palm": // from Tree
                    return this.ReloadTreeTextures(content, key, Tree.palmTree);

                case "terrainfeatures\\tree1_fall": // from Tree
                case "terrainfeatures\\tree1_spring": // from Tree
                case "terrainfeatures\\tree1_summer": // from Tree
                case "terrainfeatures\\tree1_winter": // from Tree
                    return this.ReloadTreeTextures(content, key, Tree.bushyTree);

                case "terrainfeatures\\tree2_fall": // from Tree
                case "terrainfeatures\\tree2_spring": // from Tree
                case "terrainfeatures\\tree2_summer": // from Tree
                case "terrainfeatures\\tree2_winter": // from Tree
                    return this.ReloadTreeTextures(content, key, Tree.leafyTree);

                case "terrainfeatures\\tree3_fall": // from Tree
                case "terrainfeatures\\tree3_spring": // from Tree
                case "terrainfeatures\\tree3_winter": // from Tree
                    return this.ReloadTreeTextures(content, key, Tree.pineTree);
            }

            // dynamic textures
            if (this.IsInFolder(key, "Animals"))
                return this.ReloadFarmAnimalSprites(content, key);

            if (this.IsInFolder(key, "Buildings"))
                return this.ReloadBuildings(content, key);

            if (this.IsInFolder(key, "Characters") || this.IsInFolder(key, "Characters\\Monsters"))
                return this.ReloadNpcSprites(content, key);

            if (this.KeyStartsWith(key, "LooseSprites\\Fence"))
                return this.ReloadFenceTextures(key);

            if (this.IsInFolder(key, "Portraits"))
                return this.ReloadNpcPortraits(content, key);

            // dynamic data
            if (this.IsInFolder(key, "Characters\\Dialogue"))
                return this.ReloadNpcDialogue(key);

            if (this.IsInFolder(key, "Characters\\schedules"))
                return this.ReloadNpcSchedules(key);

            return false;
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Reload texture methods
        ****/
        /// <summary>Reload the sprites for matching pets or horses.</summary>
        /// <typeparam name="TAnimal">The animal type.</typeparam>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadPetOrHorseSprites<TAnimal>(LocalizedContentManager content, string key)
            where TAnimal : NPC
        {
            // find matches
            TAnimal[] animals = this.GetCharacters().OfType<TAnimal>().ToArray();
            if (!animals.Any())
                return false;

            // update sprites
            Texture2D texture = content.Load<Texture2D>(key);
            foreach (TAnimal animal in animals)
                this.SetSpriteTexture(animal.Sprite, texture);
            return true;
        }

        /// <summary>Reload the sprites for matching farm animals.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        /// <remarks>Derived from <see cref="FarmAnimal.reload"/>.</remarks>
        private bool ReloadFarmAnimalSprites(LocalizedContentManager content, string key)
        {
            // find matches
            FarmAnimal[] animals = this.GetFarmAnimals().ToArray();
            if (!animals.Any())
                return false;

            // update sprites
            Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
            foreach (FarmAnimal animal in animals)
            {
                // get expected key
                string expectedKey = animal.age.Value < animal.ageWhenMature.Value
                    ? $"Baby{(animal.type.Value == "Duck" ? "White Chicken" : animal.type.Value)}"
                    : animal.type.Value;
                if (animal.showDifferentTextureWhenReadyForHarvest.Value && animal.currentProduce.Value <= 0)
                    expectedKey = $"Sheared{expectedKey}";
                expectedKey = $"Animals\\{expectedKey}";

                // reload asset
                if (expectedKey == key)
                    this.SetSpriteTexture(animal.Sprite, texture.Value);
            }
            return texture.IsValueCreated;
        }

        /// <summary>Reload building textures.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadBuildings(LocalizedContentManager content, string key)
        {
            // get buildings
            string type = Path.GetFileName(key);
            Building[] buildings = Game1.locations
                .OfType<BuildableGameLocation>()
                .SelectMany(p => p.buildings)
                .Where(p => p.buildingType.Value == type)
                .ToArray();

            // reload buildings
            if (buildings.Any())
            {
                Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
                foreach (Building building in buildings)
                    building.texture = texture;
                return true;
            }
            return false;
        }

        /// <summary>Reload the sprites for a fence type.</summary>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadFenceTextures(string key)
        {
            // get fence type
            if (!int.TryParse(this.GetSegments(key)[1].Substring("Fence".Length), out int fenceType))
                return false;

            // get fences
            Fence[] fences =
                (
                    from location in this.GetLocations()
                    from fence in location.Objects.Values.OfType<Fence>()
                    where
                        fence.whichType.Value == fenceType
                        || (fence.isGate.Value && fenceType == 1) // gates are hardcoded to draw fence type 1
                    select fence
                )
                .ToArray();

            // update fence textures
            foreach (Fence fence in fences)
                this.Reflection.GetField<Lazy<Texture2D>>(fence, "fenceTexture").SetValue(new Lazy<Texture2D>(fence.loadFenceTexture));
            return true;
        }

        /// <summary>Reload the disposition data for matching NPCs.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any NPCs were affected.</returns>
        private bool ReloadNpcDispositions(LocalizedContentManager content, string key)
        {
            IDictionary<string, string> dispositions = content.Load<Dictionary<string, string>>(key);
            foreach (NPC character in this.GetCharacters())
            {
                if (!character.isVillager() || !dispositions.ContainsKey(character.Name))
                    continue;

                NPC clone = new NPC(null, character.Position, character.DefaultMap, character.FacingDirection, character.Name, null, character.Portrait, eventActor: false);
                character.Age = clone.Age;
                character.Manners = clone.Manners;
                character.SocialAnxiety = clone.SocialAnxiety;
                character.Optimism = clone.Optimism;
                character.Gender = clone.Gender;
                character.datable.Value = clone.datable.Value;
                character.homeRegion = clone.homeRegion;
                character.Birthday_Season = clone.Birthday_Season;
                character.Birthday_Day = clone.Birthday_Day;
                character.id = clone.id;
                character.displayName = clone.displayName;
            }

            return true;
        }

        /// <summary>Reload the sprites for matching NPCs.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadNpcSprites(LocalizedContentManager content, string key)
        {
            // get NPCs
            NPC[] characters = this.GetCharacters()
                .Where(npc => npc.Sprite != null && this.GetNormalisedPath(npc.Sprite.textureName.Value) == key)
                .ToArray();
            if (!characters.Any())
                return false;

            // update portrait
            Texture2D texture = content.Load<Texture2D>(key);
            foreach (NPC character in characters)
                this.SetSpriteTexture(character.Sprite, texture);
            return true;
        }

        /// <summary>Reload the portraits for matching NPCs.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadNpcPortraits(LocalizedContentManager content, string key)
        {
            // get NPCs
            NPC[] villagers = this.GetCharacters()
                .Where(npc => npc.isVillager() && this.GetNormalisedPath($"Portraits\\{this.Reflection.GetMethod(npc, "getTextureName").Invoke<string>()}") == key)
                .ToArray();
            if (!villagers.Any())
                return false;

            // update portrait
            Texture2D texture = content.Load<Texture2D>(key);
            foreach (NPC villager in villagers)
            {
                villager.resetPortrait();
                villager.Portrait = texture;
            }

            return true;
        }

        /// <summary>Reload tree textures.</summary>
        /// <param name="content">The content manager through which to reload the asset.</param>
        /// <param name="key">The asset key to reload.</param>
        /// <param name="type">The type to reload.</param>
        /// <returns>Returns whether any textures were reloaded.</returns>
        private bool ReloadTreeTextures(LocalizedContentManager content, string key, int type)
        {
            Tree[] trees = Game1.locations
                .SelectMany(p => p.terrainFeatures.Values.OfType<Tree>())
                .Where(tree => tree.treeType.Value == type)
                .ToArray();

            if (trees.Any())
            {
                Lazy<Texture2D> texture = new Lazy<Texture2D>(() => content.Load<Texture2D>(key));
                foreach (Tree tree in trees)
                    this.Reflection.GetField<Lazy<Texture2D>>(tree, "texture").SetValue(texture);
                return true;
            }

            return false;
        }

        /****
        ** Reload data methods
        ****/
        /// <summary>Reload the dialogue data for matching NPCs.</summary>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any assets were reloaded.</returns>
        private bool ReloadNpcDialogue(string key)
        {
            // get NPCs
            string name = Path.GetFileName(key);
            NPC[] villagers = this.GetCharacters().Where(npc => npc.Name == name && npc.isVillager()).ToArray();
            if (!villagers.Any())
                return false;

            // update dialogue
            foreach (NPC villager in villagers)
                villager.resetSeasonalDialogue(); // doesn't only affect seasonal dialogue
            return true;
        }

        /// <summary>Reload the schedules for matching NPCs.</summary>
        /// <param name="key">The asset key to reload.</param>
        /// <returns>Returns whether any assets were reloaded.</returns>
        private bool ReloadNpcSchedules(string key)
        {
            // get NPCs
            string name = Path.GetFileName(key);
            NPC[] villagers = this.GetCharacters().Where(npc => npc.Name == name && npc.isVillager()).ToArray();
            if (!villagers.Any())
                return false;

            // update schedule
            foreach (NPC villager in villagers)
            {
                // reload schedule
                villager.Schedule = villager.getSchedule(Game1.dayOfMonth);
                if (villager.Schedule == null)
                {
                    this.Monitor.Log($"A mod set an invalid schedule for {villager.Name ?? key}, so the NPC may not behave correctly.", LogLevel.Warn);
                    return true;
                }

                // switch to new schedule if needed
                int lastScheduleTime = villager.Schedule.Keys.Where(p => p <= Game1.timeOfDay).OrderByDescending(p => p).FirstOrDefault();
                if (lastScheduleTime != 0)
                {
                    this.Reflection.GetField<int>(villager, "scheduleTimeToTry").SetValue(this.Reflection.GetField<int>(typeof(NPC), "NO_TRY").GetValue()); // use time that's passed in to checkSchedule
                    villager.checkSchedule(lastScheduleTime);
                }
            }
            return true;
        }

        /****
        ** Helpers
        ****/
        /// <summary>Reload the texture for an animated sprite.</summary>
        /// <param name="sprite">The animated sprite to update.</param>
        /// <param name="texture">The texture to set.</param>
        private void SetSpriteTexture(AnimatedSprite sprite, Texture2D texture)
        {
            this.Reflection.GetField<Texture2D>(sprite, "spriteTexture").SetValue(texture);
        }

        /// <summary>Get all NPCs in the game (excluding farm animals).</summary>
        private IEnumerable<NPC> GetCharacters()
        {
            return this.GetLocations().SelectMany(p => p.characters);
        }

        /// <summary>Get all farm animals in the game.</summary>
        private IEnumerable<FarmAnimal> GetFarmAnimals()
        {
            foreach (GameLocation location in this.GetLocations())
            {
                if (location is Farm farm)
                {
                    foreach (FarmAnimal animal in farm.animals.Values)
                        yield return animal;
                }
                else if (location is AnimalHouse animalHouse)
                    foreach (FarmAnimal animal in animalHouse.animals.Values)
                        yield return animal;
            }
        }

        /// <summary>Get all locations in the game.</summary>
        private IEnumerable<GameLocation> GetLocations()
        {
            // get available root locations
            IEnumerable<GameLocation> rootLocations = Game1.locations;
            if (SaveGame.loaded?.locations != null)
                rootLocations = rootLocations.Concat(SaveGame.loaded.locations);

            // yield root + child locations
            foreach (GameLocation location in rootLocations)
            {
                yield return location;

                if (location is BuildableGameLocation buildableLocation)
                {
                    foreach (Building building in buildableLocation.buildings)
                    {
                        GameLocation indoors = building.indoors.Value;
                        if (indoors != null)
                            yield return indoors;
                    }
                }
            }
        }

        /// <summary>Get whether a key starts with a substring after the substring is normalised.</summary>
        /// <param name="key">The key to check.</param>
        /// <param name="rawSubstring">The substring to normalise and find.</param>
        private bool KeyStartsWith(string key, string rawSubstring)
        {
            return key.StartsWith(this.GetNormalisedPath(rawSubstring), StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>Get whether a normalised asset key is in the given folder.</summary>
        /// <param name="key">The normalised asset key (like <c>Animals/cat</c>).</param>
        /// <param name="folder">The key folder (like <c>Animals</c>); doesn't need to be normalised.</param>
        /// <param name="allowSubfolders">Whether to return true if the key is inside a subfolder of the <paramref name="folder"/>.</param>
        private bool IsInFolder(string key, string folder, bool allowSubfolders = false)
        {
            return
                this.KeyStartsWith(key, $"{folder}\\")
                && (allowSubfolders || this.CountSegments(key) == this.CountSegments(folder) + 1);
        }

        /// <summary>Get the segments in a path (e.g. 'a/b' is 'a' and 'b').</summary>
        /// <param name="path">The path to check.</param>
        private string[] GetSegments(string path)
        {
            if (path == null)
                return new string[0];
            return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        /// <summary>Count the number of segments in a path (e.g. 'a/b' is 2).</summary>
        /// <param name="path">The path to check.</param>
        private int CountSegments(string path)
        {
            return this.GetSegments(path).Length;
        }
    }
}
