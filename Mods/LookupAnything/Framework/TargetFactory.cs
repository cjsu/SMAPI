using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Data;
using Pathoschild.Stardew.LookupAnything.Framework.Subjects;
using Pathoschild.Stardew.LookupAnything.Framework.Targets;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.LookupAnything.Framework
{
    /// <summary>Finds and analyses lookup targets in the world.</summary>
    internal class TargetFactory
    {
        /*********
        ** Fields
        *********/
        /// <summary>Provides metadata that's not available from the game data directly.</summary>
        private readonly Metadata Metadata;

        /// <summary>Simplifies access to private game code.</summary>
        private readonly IReflectionHelper Reflection;

        /// <summary>Provides translations stored in the mod folder.</summary>
        private readonly ITranslationHelper Translations;

        /// <summary>Provides utility methods for interacting with the game code.</summary>
        private readonly GameHelper GameHelper;


        /*********
        ** Public methods
        *********/
        /****
        ** Constructors
        ****/
        /// <summary>Construct an instance.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        /// <param name="reflection">Simplifies access to private game code.</param>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        public TargetFactory(Metadata metadata, ITranslationHelper translations, IReflectionHelper reflection, GameHelper gameHelper)
        {
            this.Metadata = metadata;
            this.Translations = translations;
            this.Reflection = reflection;
            this.GameHelper = gameHelper;
        }

        /****
        ** Targets
        ****/
        /// <summary>Get all potential lookup targets in the current location.</summary>
        /// <param name="location">The current location.</param>
        /// <param name="originTile">The tile from which to search for targets.</param>
        /// <param name="includeMapTile">Whether to allow matching the map tile itself.</param>
        public IEnumerable<ITarget> GetNearbyTargets(GameLocation location, Vector2 originTile, bool includeMapTile)
        {
            // NPCs
            foreach (NPC npc in location.characters)
            {
                if (!this.GameHelper.CouldSpriteOccludeTile(npc.getTileLocation(), originTile))
                    continue;

                TargetType type = TargetType.Unknown;
                if (npc is Child || npc.isVillager())
                    type = TargetType.Villager;
                else if (npc is Horse)
                    type = TargetType.Horse;
                else if (npc is Junimo)
                    type = TargetType.Junimo;
                else if (npc is Pet)
                    type = TargetType.Pet;
                else if (npc is Monster)
                    type = TargetType.Monster;

                yield return new CharacterTarget(this.GameHelper, type, npc, npc.getTileLocation(), this.Reflection);
            }

            // animals
            foreach (FarmAnimal animal in (location as Farm)?.animals.Values ?? (location as AnimalHouse)?.animals.Values ?? Enumerable.Empty<FarmAnimal>())
            {
                if (!this.GameHelper.CouldSpriteOccludeTile(animal.getTileLocation(), originTile))
                    continue;

                yield return new FarmAnimalTarget(this.GameHelper, animal, animal.getTileLocation());
            }

            // map objects
            foreach (KeyValuePair<Vector2, SObject> pair in location.objects.Pairs)
            {
                Vector2 spriteTile = pair.Key;
                SObject obj = pair.Value;

                if (!this.GameHelper.CouldSpriteOccludeTile(spriteTile, originTile))
                    continue;

                yield return new ObjectTarget(this.GameHelper, obj, spriteTile, this.Reflection);
            }

            // furniture
            if (location is DecoratableLocation decoratableLocation)
            {
                foreach (var furniture in decoratableLocation.furniture)
                    yield return new ObjectTarget(this.GameHelper, furniture, furniture.TileLocation, this.Reflection);
            }

            // terrain features
            foreach (KeyValuePair<Vector2, TerrainFeature> pair in location.terrainFeatures.Pairs)
            {
                Vector2 spriteTile = pair.Key;
                TerrainFeature feature = pair.Value;

                if (!this.GameHelper.CouldSpriteOccludeTile(spriteTile, originTile))
                    continue;

                if (feature is HoeDirt dirt && dirt.crop != null)
                    yield return new CropTarget(this.GameHelper, dirt, spriteTile, this.Reflection);
                else if (feature is FruitTree fruitTree)
                {
                    if (this.Reflection.GetField<float>(feature, "alpha").GetValue() < 0.8f)
                        continue; // ignore when tree is faded out (so player can lookup things behind it)
                    yield return new FruitTreeTarget(this.GameHelper, fruitTree, spriteTile);
                }
                else if (feature is Tree wildTree)
                {
                    if (this.Reflection.GetField<float>(feature, "alpha").GetValue() < 0.8f)
                        continue; // ignore when tree is faded out (so player can lookup things behind it)
                    yield return new TreeTarget(this.GameHelper, wildTree, spriteTile, this.Reflection);
                }
                else
                    yield return new UnknownTarget(this.GameHelper, feature, spriteTile);
            }

            // players
            foreach (Farmer farmer in location.farmers)
            {
                if (!this.GameHelper.CouldSpriteOccludeTile(farmer.getTileLocation(), originTile))
                    continue;

                yield return new FarmerTarget(this.GameHelper, farmer);
            }

            // buildings
            if (location is BuildableGameLocation buildableLocation)
            {
                foreach (Building building in buildableLocation.buildings)
                {
                    if (!this.GameHelper.CouldSpriteOccludeTile(new Vector2(building.tileX.Value, building.tileY.Value + building.tilesHigh.Value), originTile, Constant.MaxBuildingTargetSpriteSize))
                        continue;

                    yield return new BuildingTarget(this.GameHelper, building);
                }
            }

            // tiles
            if (includeMapTile)
                yield return new TileTarget(this.GameHelper, originTile);
        }

        /// <summary>Get the target on the specified tile.</summary>
        /// <param name="location">The current location.</param>
        /// <param name="tile">The tile to search.</param>
        /// <param name="includeMapTile">Whether to allow matching the map tile itself.</param>
        public ITarget GetTargetFromTile(GameLocation location, Vector2 tile, bool includeMapTile)
        {
            return (
                from target in this.GetNearbyTargets(location, tile, includeMapTile)
                where
                    target.Type != TargetType.Unknown
                    && target.IsAtTile(tile)
                select target
            ).FirstOrDefault();
        }

        /// <summary>Get the target at the specified coordinate.</summary>
        /// <param name="location">The current location.</param>
        /// <param name="tile">The tile to search.</param>
        /// <param name="position">The viewport-relative pixel coordinate to search.</param>
        /// <param name="includeMapTile">Whether to allow matching the map tile itself.</param>
        public ITarget GetTargetFromScreenCoordinate(GameLocation location, Vector2 tile, Vector2 position, bool includeMapTile)
        {
            // get target sprites which might overlap cursor position (first approximation)
            Rectangle tileArea = this.GameHelper.GetScreenCoordinatesFromTile(tile);
            var candidates = (
                from target in this.GetNearbyTargets(location, tile, includeMapTile)
                let spriteArea = target.GetWorldArea()
                let isAtTile = target.IsAtTile(tile)
                where
                    target.Type != TargetType.Unknown
                    && (isAtTile || spriteArea.Intersects(tileArea))
                orderby
                    target.Type != TargetType.Tile ? 0 : 1, // Tiles are always under anything else.
                    spriteArea.Y descending,                // A higher Y value is closer to the foreground, and will occlude any sprites behind it.
                    spriteArea.X ascending                  // If two sprites at the same Y coordinate overlap, assume the left sprite occludes the right.

                select new { target, spriteArea, isAtTile }
            ).ToArray();

            // choose best match
            return
                candidates.FirstOrDefault(p => p.target.SpriteIntersectsPixel(tile, position, p.spriteArea))?.target // sprite pixel under cursor
                ?? candidates.FirstOrDefault(p => p.isAtTile)?.target; // tile under cursor
        }

        /****
        ** Subjects
        ****/
        /// <summary>Get metadata for a Stardew object at the specified position.</summary>
        /// <param name="player">The player performing the lookup.</param>
        /// <param name="location">The current location.</param>
        /// <param name="lookupMode">The lookup target mode.</param>
        /// <param name="includeMapTile">Whether to allow matching the map tile itself.</param>
        public ISubject GetSubjectFrom(Farmer player, GameLocation location, LookupMode lookupMode, bool includeMapTile)
        {
            // get target
            ITarget target;
            switch (lookupMode)
            {
                // under cursor
                case LookupMode.Cursor:
                    target = this.GetTargetFromScreenCoordinate(location, Game1.currentCursorTile, this.GameHelper.GetScreenCoordinatesFromCursor(), includeMapTile);
                    break;

                // in front of player
                case LookupMode.FacingPlayer:
                    Vector2 tile = this.GetFacingTile(Game1.player);
                    target = this.GetTargetFromTile(location, tile, includeMapTile);
                    break;

                default:
                    throw new NotSupportedException($"Unknown lookup mode '{lookupMode}'.");
            }

            // get subject
            return target != null
                ? this.GetSubjectFrom(target)
                : null;
        }

        /// <summary>Get metadata for a Stardew object represented by a target.</summary>
        /// <param name="target">The target.</param>
        public ISubject GetSubjectFrom(ITarget target)
        {
            switch (target.Type)
            {
                // NPC
                case TargetType.Horse:
                case TargetType.Junimo:
                case TargetType.Pet:
                case TargetType.Monster:
                case TargetType.Villager:
                    return new CharacterSubject(this.GameHelper, target.GetValue<NPC>(), target.Type, this.Metadata, this.Translations, this.Reflection);

                // player
                case TargetType.Farmer:
                    return new FarmerSubject(this.GameHelper, target.GetValue<Farmer>(), this.Translations, this.Reflection);

                // animal
                case TargetType.FarmAnimal:
                    return new FarmAnimalSubject(this.GameHelper, target.GetValue<FarmAnimal>(), this.Translations);

                // crop
                case TargetType.Crop:
                    Crop crop = target.GetValue<HoeDirt>().crop;
                    return new ItemSubject(this.GameHelper, this.Translations, this.GameHelper.GetObjectBySpriteIndex(crop.indexOfHarvest.Value), ObjectContext.World, knownQuality: false, fromCrop: crop);

                // tree
                case TargetType.FruitTree:
                    return new FruitTreeSubject(this.GameHelper, target.GetValue<FruitTree>(), target.GetTile(), this.Translations);
                case TargetType.WildTree:
                    return new TreeSubject(this.GameHelper, target.GetValue<Tree>(), target.GetTile(), this.Translations);

                // object
                case TargetType.InventoryItem:
                    return new ItemSubject(this.GameHelper, this.Translations, target.GetValue<Item>(), ObjectContext.Inventory, knownQuality: false);
                case TargetType.Object:
                    return new ItemSubject(this.GameHelper, this.Translations, target.GetValue<Item>(), ObjectContext.World, knownQuality: false);

                // building
                case TargetType.Building:
                    return new BuildingSubject(this.GameHelper, this.Metadata, target.GetValue<Building>(), target.GetSpritesheetArea(), this.Translations, this.Reflection);

                // tile
                case TargetType.Tile:
                    return new TileSubject(this.GameHelper, Game1.currentLocation, target.GetValue<Vector2>(), this.Translations);
            }

            return null;
        }

        /// <summary>Get metadata for a menu element at the specified position.</summary>
        /// <param name="menu">The active menu.</param>
        /// <param name="cursorPos">The cursor's viewport-relative coordinates.</param>
        public ISubject GetSubjectFrom(IClickableMenu menu, Vector2 cursorPos)
        {
            switch (menu)
            {
                // calendar
                case Billboard billboard:
                    {
                        // get target day
                        int selectedDay = -1;
                        for (int i = 0; i < billboard.calendarDays.Count; i++)
                        {
                            if (billboard.calendarDays[i].containsPoint((int)cursorPos.X, (int)cursorPos.Y))
                            {
                                selectedDay = i + 1;
                                break;
                            }
                        }
                        if (selectedDay == -1)
                            return null;

                        // get villager with a birthday on that date
                        NPC target = this.GameHelper.GetAllCharacters().FirstOrDefault(p => p.Birthday_Season == Game1.currentSeason && p.Birthday_Day == selectedDay);
                        if (target != null)
                            return new CharacterSubject(this.GameHelper, target, TargetType.Villager, this.Metadata, this.Translations, this.Reflection);
                    }
                    break;

                // chest
                case MenuWithInventory inventoryMenu:
                    {
                        Item item = inventoryMenu.hoveredItem;
                        if (item != null)
                            return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);
                    }
                    break;

                // inventory
                case GameMenu gameMenu:
                    {
                        List<IClickableMenu> tabs = this.Reflection.GetField<List<IClickableMenu>>(gameMenu, "pages").GetValue();
                        IClickableMenu curTab = tabs[gameMenu.currentTab];
                        switch (curTab)
                        {
                            // inventory
                            case InventoryPage _:
                                {
                                    Item item = this.Reflection.GetField<Item>(curTab, "hoveredItem").GetValue();
                                    if (item != null)
                                        return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);
                                }
                                break;

                            // collections menu
                            // derived from CollectionsPage::performHoverAction
                            case CollectionsPage collectionsTab:
                                {
                                    int currentTab = this.Reflection.GetField<int>(curTab, "currentTab").GetValue();
                                    if (currentTab == CollectionsPage.achievementsTab || currentTab == CollectionsPage.secretNotesTab)
                                        break;

                                    int currentPage = this.Reflection.GetField<int>(curTab, "currentPage").GetValue();

                                    foreach (ClickableTextureComponent component in collectionsTab.collections[currentTab][currentPage])
                                    {
                                        if (component.containsPoint((int)cursorPos.X, (int)cursorPos.Y))
                                        {
                                            int itemID = Convert.ToInt32(component.name.Split(' ')[0]);
                                            SObject obj = new SObject(itemID, 1);
                                            return new ItemSubject(this.GameHelper, this.Translations, obj, ObjectContext.Inventory, knownQuality: false);
                                        }
                                    }
                                }
                                break;

                            // cooking or crafting menu
                            case CraftingPage _:
                                {
                                    // player inventory item
                                    Item item = this.Reflection.GetField<Item>(curTab, "hoverItem").GetValue();
                                    if (item != null)
                                        return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);

                                    // crafting recipe
                                    CraftingRecipe recipe = this.Reflection.GetField<CraftingRecipe>(curTab, "hoverRecipe").GetValue();
                                    if (recipe != null)
                                        return new ItemSubject(this.GameHelper, this.Translations, recipe.createItem(), ObjectContext.Inventory, knownQuality: true);
                                }
                                break;

                            // social tab
                            case SocialPage _:
                                {
                                    // get villagers on current page
                                    int scrollOffset = this.Reflection.GetField<int>(curTab, "slotPosition").GetValue();
                                    ClickableTextureComponent[] entries = this.Reflection
                                        .GetField<List<ClickableTextureComponent>>(curTab, "sprites")
                                        .GetValue()
                                        .Skip(scrollOffset)
                                        .ToArray();

                                    // find hovered villager
                                    ClickableTextureComponent entry = entries.FirstOrDefault(p => p.containsPoint((int)cursorPos.X, (int)cursorPos.Y));
                                    if (entry != null)
                                    {
                                        int index = Array.IndexOf(entries, entry) + scrollOffset;
                                        object socialID = this.Reflection.GetField<List<object>>(curTab, "names").GetValue()[index];
                                        if (socialID is long playerID)
                                        {
                                            Farmer player = Game1.getFarmer(playerID);
                                            return new FarmerSubject(this.GameHelper, player, this.Translations, this.Reflection);
                                        }
                                        else if (socialID is string villagerName)
                                        {
                                            NPC npc = this.GameHelper.GetAllCharacters().FirstOrDefault(p => p.isVillager() && p.Name == villagerName);
                                            if (npc != null)
                                                return new CharacterSubject(this.GameHelper, npc, TargetType.Villager, this.Metadata, this.Translations, this.Reflection);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    break;

                // Community Center bundle menu
                case JunimoNoteMenu bundleMenu:
                    {
                        // hovered inventory item
                        {
                            Item item = this.Reflection.GetField<Item>(menu, "hoveredItem").GetValue();
                            if (item != null)
                                return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);
                        }

                        // list of required ingredients
                        for (int i = 0; i < bundleMenu.ingredientList.Count; i++)
                        {
                            if (bundleMenu.ingredientList[i].containsPoint((int)cursorPos.X, (int)cursorPos.Y))
                            {
                                Bundle bundle = this.Reflection.GetField<Bundle>(bundleMenu, "currentPageBundle").GetValue();
                                var ingredient = bundle.ingredients[i];
                                var item = this.GameHelper.GetObjectBySpriteIndex(ingredient.index, ingredient.stack);
                                item.Quality = ingredient.quality;
                                return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);
                            }
                        }

                        // list of submitted ingredients
                        foreach (ClickableTextureComponent slot in bundleMenu.ingredientSlots)
                        {
                            if (slot.item != null && slot.containsPoint((int)cursorPos.X, (int)cursorPos.Y))
                                return new ItemSubject(this.GameHelper, this.Translations, slot.item, ObjectContext.Inventory, knownQuality: true);
                        }
                    }
                    break;

                // kitchen
                case CraftingPage _:
                    {
                        CraftingRecipe recipe = this.Reflection.GetField<CraftingRecipe>(menu, "hoverRecipe").GetValue();
                        if (recipe != null)
                            return new ItemSubject(this.GameHelper, this.Translations, recipe.createItem(), ObjectContext.Inventory, knownQuality: true);
                    }
                    break;

                // load menu
                case TitleMenu _ when TitleMenu.subMenu is LoadGameMenu loadMenu:
                    {
                        //ClickableComponent button = loadMenu.slotButtons.FirstOrDefault(p => p.containsPoint((int)cursorPos.X, (int)cursorPos.Y));
                        //if (button != null)
                        //{
                        //    int index = this.Reflection.GetField<int>(loadMenu, "currentItemIndex").GetValue() + int.Parse(button.name);
                        //    var slots = this.Reflection.GetProperty<List<LoadGameMenu.MenuSlot>>(loadMenu, "MenuSlots").GetValue();
                        //    LoadGameMenu.SaveFileSlot slot = slots[index] as LoadGameMenu.SaveFileSlot;
                        //    if (slot?.Farmer != null)
                        //        return new FarmerSubject(this.GameHelper, slot.Farmer, this.Translations, this.Reflection, isLoadMenu: true);
                        //}
                    }
                    break;

                // shop
                case ShopMenu _:
                    {
                        Item item = this.Reflection.GetField<Item>(menu, "hoveredItem").GetValue();
                        if (item != null)
                            return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);
                    }
                    break;

                // toolbar
                case Toolbar _:
                    {
                        // find hovered slot
                        List<ClickableComponent> slots = this.Reflection.GetField<List<ClickableComponent>>(menu, "buttons").GetValue();
                        ClickableComponent hoveredSlot = slots.FirstOrDefault(slot => slot.containsPoint((int)cursorPos.X, (int)cursorPos.Y));
                        if (hoveredSlot == null)
                            return null;

                        // get inventory index
                        int index = slots.IndexOf(hoveredSlot);
                        if (index < 0 || index > Game1.player.Items.Count - 1)
                            return null;

                        // get hovered item
                        Item item = Game1.player.Items[index];
                        if (item != null)
                            return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);
                    }
                    break;

                // by convention (for mod support)
                default:
                    {
                        Item item = this.Reflection.GetField<Item>(menu, "HoveredItem", required: false)?.GetValue(); // ChestsAnywhere
                        if (item != null)
                            return new ItemSubject(this.GameHelper, this.Translations, item, ObjectContext.Inventory, knownQuality: true);
                    }
                    break;
            }

            return null;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the tile the player is facing.</summary>
        /// <param name="player">The player to check.</param>
        private Vector2 GetFacingTile(Farmer player)
        {
            Vector2 tile = player.getTileLocation();
            FacingDirection direction = (FacingDirection)player.FacingDirection;
            switch (direction)
            {
                case FacingDirection.Up:
                    return tile + new Vector2(0, -1);
                case FacingDirection.Right:
                    return tile + new Vector2(1, 0);
                case FacingDirection.Down:
                    return tile + new Vector2(0, 1);
                case FacingDirection.Left:
                    return tile + new Vector2(-1, 0);
                default:
                    throw new NotSupportedException($"Unknown facing direction {direction}");
            }
        }
    }
}
