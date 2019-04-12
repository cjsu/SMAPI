using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.Integrations.CustomFarmingRedux;
using Pathoschild.Stardew.LookupAnything.Framework;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using Pathoschild.Stardew.LookupAnything.Framework.Data;
using Pathoschild.Stardew.LookupAnything.Framework.Models;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.LookupAnything
{
    /// <summary>Provides utility methods for interacting with the game code.</summary>
    internal class GameHelper
    {
        /*********
        ** Fields
        *********/
        /// <summary>The cached object data.</summary>
        private Lazy<ObjectModel[]> Objects;

        /// <summary>The cached villagers' gift tastes.</summary>
        private Lazy<GiftTasteModel[]> GiftTastes;

        /// <summary>The cached recipes.</summary>
        private Lazy<RecipeModel[]> Recipes;

        /// <summary>The Custom Farming Redux integration.</summary>
        private readonly CustomFarmingReduxIntegration CustomFarmingRedux;

        /// <summary>Parses the raw game data into usable models.</summary>
        private readonly DataParser DataParser;


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="customFarmingRedux">The Custom Farming Redux integration.</param>
        public GameHelper(CustomFarmingReduxIntegration customFarmingRedux)
        {
            this.DataParser = new DataParser(this);
            this.CustomFarmingRedux = customFarmingRedux;
        }

        /// <summary>Reset the low-level cache used to store expensive query results, so the data is recalculated on demand.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        /// <param name="reflectionHelper">Simplifies access to private game code.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        /// <param name="monitor">The monitor with which to log errors.</param>
        public void ResetCache(Metadata metadata, IReflectionHelper reflectionHelper, ITranslationHelper translations, IMonitor monitor)
        {
            this.Objects = new Lazy<ObjectModel[]>(() => this.DataParser.GetObjects(monitor).ToArray());
            this.GiftTastes = new Lazy<GiftTasteModel[]>(() => this.DataParser.GetGiftTastes(this.Objects.Value).ToArray());
            this.Recipes = new Lazy<RecipeModel[]>(() => this.DataParser.GetRecipes(metadata, reflectionHelper, translations).ToArray());
        }

        /****
        ** Data helpers
        ****/
        /// <summary>Get the number of times the player has shipped a given item.</summary>
        /// <param name="itemID">The item's parent sprite index.</param>
        public int GetShipped(int itemID)
        {
            return Game1.player.basicShipped.ContainsKey(itemID)
                ? Game1.player.basicShipped[itemID]
                : 0;
        }

        /// <summary>Get all shippable items.</summary>
        /// <remarks>Derived from <see cref="Utility.hasFarmerShippedAllItems"/>.</remarks>
        public IEnumerable<KeyValuePair<int, bool>> GetFullShipmentAchievementItems()
        {
            return (
                from obj in this.Objects.Value
                where obj.Type != "Arch" && obj.Type != "Fish" && obj.Type != "Mineral" && obj.Type != "Cooking" && SObject.isPotentialBasicShippedCategory(obj.ParentSpriteIndex, obj.Category.ToString())
                select new KeyValuePair<int, bool>(obj.ParentSpriteIndex, Game1.player.basicShipped.ContainsKey(obj.ParentSpriteIndex))
            );
        }

        /// <summary>Get all items owned by the player.</summary>
        /// <remarks>Derived from <see cref="Utility.doesItemWithThisIndexExistAnywhere"/>.</remarks>
        public IEnumerable<Item> GetAllOwnedItems()
        {
            List<Item> items = new List<Item>();

            // inventory
            items.AddRange(Game1.player.Items);

            // in locations
            foreach (GameLocation location in CommonHelper.GetLocations())
            {
                // map objects
                foreach (SObject item in location.objects.Values)
                {
                    // chest
                    if (item is Chest chest)
                    {
                        if (chest.playerChest.Value)
                        {
                            items.Add(chest);
                            items.AddRange(chest.items);
                        }
                    }

                    // auto-grabber
                    else if (item.ParentSheetIndex == 165 && item.heldObject.Value is Chest grabberChest)
                    {
                        items.Add(item);
                        items.AddRange(grabberChest.items);
                    }

                    // cask
                    else if (item is Cask)
                    {
                        items.Add(item);
                        items.Add(item.heldObject.Value); // cask contents can be retrieved anytime
                    }

                    // craftable
                    else if (item.bigCraftable.Value)
                    {
                        items.Add(item);
                        if (item.MinutesUntilReady == 0)
                            items.Add(item.heldObject.Value);
                    }

                    // anything else
                    else if (!item.IsSpawnedObject)
                    {
                        items.Add(item);
                        items.Add(item.heldObject.Value);
                    }
                }

                // furniture
                if (location is DecoratableLocation decorableLocation)
                {
                    foreach (Furniture furniture in decorableLocation.furniture)
                    {
                        items.Add(furniture);
                        items.Add(furniture.heldObject.Value);
                    }
                }

                // building output
                if (location is Farm farm)
                {
                    foreach (var building in farm.buildings)
                    {
                        if (building is Mill mill)
                            items.AddRange(mill.output.Value.items);
                        else if (building is JunimoHut hut)
                            items.AddRange(hut.output.Value.items);
                    }
                }

                // farmhouse fridge
                if (location is FarmHouse house)
                    items.AddRange(house.fridge.Value.items);
            }

            return items.Where(p => p != null);
        }

        /// <summary>Get all NPCs currently in the world.</summary>
        public IEnumerable<NPC> GetAllCharacters()
        {
            List<NPC> characters = new List<NPC>();
            Utility.getAllCharacters(characters);
            return characters.Distinct(); // fix rare issue where the game duplicates an NPC (seems to happen when the player's child is born)
        }

        /// <summary>Count how many of an item the player owns.</summary>
        /// <param name="item">The item to count.</param>
        public int CountOwnedItems(Item item)
        {
            return (
                from worldItem in this.GetAllOwnedItems()
                where this.AreEquivalent(worldItem, item)
                let canStack = worldItem.canStackWith(worldItem)
                select canStack ? Math.Max(1, worldItem.Stack) : 1
            ).Sum();
        }

        /// <summary>Get whether two items are the same type (ignoring flavour text like 'blueberry wine' vs 'cranberry wine').</summary>
        /// <param name="a">The first item to compare.</param>
        /// <param name="b">The second item to compare.</param>
        private bool AreEquivalent(Item a, Item b)
        {
            return
                // same generic item type
                a.GetType() == b.GetType()
                && a.Category == b.Category
                && a.ParentSheetIndex == b.ParentSheetIndex

                // same discriminators
                && a.GetSpriteType() == b.GetSpriteType()
                && (a as Boots)?.indexInTileSheet == (b as Boots)?.indexInTileSheet
                && (a as BreakableContainer)?.Type == (b as BreakableContainer)?.Type
                && (a as Fence)?.isGate == (b as Fence)?.isGate
                && (a as Fence)?.whichType == (b as Fence)?.whichType
                && (a as Hat)?.which == (b as Hat)?.which
                && (a as MeleeWeapon)?.type == (b as MeleeWeapon)?.type
                && (a as Ring)?.indexInTileSheet == (b as Ring)?.indexInTileSheet
                && (a as Tool)?.InitialParentTileIndex == (b as Tool)?.InitialParentTileIndex
                && (a as Tool)?.CurrentParentTileIndex == (b as Tool)?.CurrentParentTileIndex;
        }

        /// <summary>Get whether the specified NPC has social data like a birthday and gift tastes.</summary>
        /// <param name="npc">The NPC to check.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public bool IsSocialVillager(NPC npc, Metadata metadata)
        {
            return npc.isVillager() && !metadata.Constants.AsocialVillagers.Contains(npc.Name);
        }

        /// <summary>Get how much each NPC likes receiving an item as a gift.</summary>
        /// <param name="item">The item to check.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public IEnumerable<KeyValuePair<NPC, GiftTaste>> GetGiftTastes(Item item, Metadata metadata)
        {
            if (!item.canBeGivenAsGift())
                yield break;

            foreach (NPC npc in this.GetAllCharacters())
            {
                if (!this.IsSocialVillager(npc, metadata))
                    continue;

                GiftTaste? taste = this.GetGiftTaste(npc, item);
                if (taste.HasValue)
                    yield return new KeyValuePair<NPC, GiftTaste>(npc, taste.Value);
            }
        }

        /// <summary>Get the items a specified NPC can receive.</summary>
        /// <param name="npc">The NPC to check.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public IDictionary<SObject, GiftTaste> GetGiftTastes(NPC npc, Metadata metadata)
        {
            if (!this.IsSocialVillager(npc, metadata))
                return new Dictionary<SObject, GiftTaste>();

            // get giftable items
            HashSet<int> giftableItemIDs = new HashSet<int>(
                from int refID in this.GiftTastes.Value.Select(p => p.RefID)
                from ObjectModel obj in this.Objects.Value
                where obj.ParentSpriteIndex == refID || obj.Category == refID
                select obj.ParentSpriteIndex
            );

            // get gift tastes
            return
                (
                    from int itemID in giftableItemIDs
                    let item = this.GetObjectBySpriteIndex(itemID)
                    let taste = this.GetGiftTaste(npc, item)
                    where taste.HasValue
                    select new { Item = item, Taste = taste.Value }
                )
                .ToDictionary(p => p.Item, p => p.Taste);
        }

        /// <summary>Get parsed data about the friendship between a player and NPC.</summary>
        /// <param name="player">The player.</param>
        /// <param name="npc">The NPC.</param>
        /// <param name="friendship">The current friendship data.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public FriendshipModel GetFriendshipForVillager(Farmer player, NPC npc, Friendship friendship, Metadata metadata)
        {
            return this.DataParser.GetFriendshipForVillager(player, npc, friendship, metadata);
        }

        /// <summary>Get parsed data about the friendship between a player and NPC.</summary>
        /// <param name="player">The player.</param>
        /// <param name="pet">The pet.</param>
        public FriendshipModel GetFriendshipForPet(Farmer player, Pet pet)
        {
            return this.DataParser.GetFriendshipForPet(player, pet);
        }

        /// <summary>Get parsed data about the friendship between a player and NPC.</summary>
        /// <param name="player">The player.</param>
        /// <param name="animal">The farm animal.</param>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public FriendshipModel GetFriendshipForAnimal(Farmer player, FarmAnimal animal, Metadata metadata)
        {
            return this.DataParser.GetFriendshipForAnimal(player, animal, metadata);
        }

        /// <summary>Parse monster data.</summary>
        public IEnumerable<MonsterData> GetMonsterData()
        {
            return this.DataParser.GetMonsters();
        }

        /// <summary>Read parsed data about the Community Center bundles.</summary>
        public IEnumerable<BundleModel> GetBundleData()
        {
            return this.DataParser.GetBundles();
        }

        /// <summary>Get the recipes for which an item is needed.</summary>
        public IEnumerable<RecipeModel> GetRecipes()
        {
            return this.Recipes.Value;
        }

        /// <summary>Get the recipes for which an item is needed.</summary>
        /// <param name="item">The item.</param>
        public IEnumerable<RecipeModel> GetRecipesForIngredient(Item item)
        {
            if (item is SObject obj && obj.bigCraftable.Value)
                return new RecipeModel[0]; // bigcraftables never valid as an ingredient

            return (
                from recipe in this.GetRecipes()
                where
                    (recipe.Ingredients.ContainsKey(item.ParentSheetIndex) || recipe.Ingredients.ContainsKey(item.Category))
                    && recipe.ExceptIngredients?.Contains(item.ParentSheetIndex) != true
                select recipe
            );
        }

        /// <summary>Get an object by its parent sprite index.</summary>
        /// <param name="index">The parent sprite index.</param>
        /// <param name="stack">The number of items in the stack.</param>
        public SObject GetObjectBySpriteIndex(int index, int stack = 1)
        {
            return new SObject(index, stack);
        }

        /// <summary>Get whether an item can have a quality (which increases its sale price).</summary>
        /// <param name="item">The item.</param>
        public bool CanHaveQuality(Item item)
        {
            // check category
            if (new[] { "Artifact", "Trash", "Crafting", "Seed", "Decor", "Resource", "Fertilizer", "Bait", "Fishing Tackle" }.Contains(item.getCategoryName()))
                return false;

            // check type
            if (new[] { "Crafting", "asdf" /*dig spots*/, "Quest" }.Contains((item as SObject)?.Type))
                return false;

            return true;
        }

        /****
        ** Coordinates
        ****/
        /// <summary>Get the viewport coordinates from the current cursor position.</summary>
        public Vector2 GetScreenCoordinatesFromCursor()
        {
            return new Vector2(Game1.getOldMouseX(), Game1.getOldMouseY());
        }

        /// <summary>Get the viewport coordinates represented by a tile position.</summary>
        /// <param name="coordinates">The absolute coordinates.</param>
        public Vector2 GetScreenCoordinatesFromAbsolute(Vector2 coordinates)
        {
            return coordinates - new Vector2(Game1.viewport.X, Game1.viewport.Y);
        }

        /// <summary>Get the viewport coordinates represented by a tile position.</summary>
        /// <param name="tile">The tile position.</param>
        public Rectangle GetScreenCoordinatesFromTile(Vector2 tile)
        {
            Vector2 position = this.GetScreenCoordinatesFromAbsolute(tile * new Vector2(Game1.tileSize));
            return new Rectangle((int)position.X, (int)position.Y, Game1.tileSize, Game1.tileSize);
        }

        /// <summary>Get whether a sprite on a given tile could occlude a specified tile position.</summary>
        /// <param name="spriteTile">The tile of the possible sprite.</param>
        /// <param name="occludeTile">The tile to check for possible occlusion.</param>
        /// <param name="spriteSize">The largest expected sprite size (measured in tiles).</param>
        public bool CouldSpriteOccludeTile(Vector2 spriteTile, Vector2 occludeTile, Vector2? spriteSize = null)
        {
            spriteSize = spriteSize ?? Constant.MaxTargetSpriteSize;
            return
                spriteTile.Y >= occludeTile.Y // sprites never extend downard from their tile
                && Math.Abs(spriteTile.X - occludeTile.X) <= spriteSize.Value.X
                && Math.Abs(spriteTile.Y - occludeTile.Y) <= spriteSize.Value.Y;
        }

        /// <summary>Get the pixel coordinates within a sprite sheet corresponding to a sprite displayed in the world.</summary>
        /// <param name="worldPosition">The pixel position in the world.</param>
        /// <param name="worldRectangle">The sprite rectangle in the world.</param>
        /// <param name="spriteRectangle">The sprite rectangle in the sprite sheet.</param>
        /// <param name="spriteEffects">The transformation to apply on the sprite.</param>
        public Vector2 GetSpriteSheetCoordinates(Vector2 worldPosition, Rectangle worldRectangle, Rectangle spriteRectangle, SpriteEffects spriteEffects = SpriteEffects.None)
        {
            // get position within sprite rectangle
            float x = (worldPosition.X - worldRectangle.X) / Game1.pixelZoom;
            float y = (worldPosition.Y - worldRectangle.Y) / Game1.pixelZoom;

            // flip values
            if (spriteEffects.HasFlag(SpriteEffects.FlipHorizontally))
                x = spriteRectangle.Width - x;
            if (spriteEffects.HasFlag(SpriteEffects.FlipVertically))
                y = spriteRectangle.Height - y;

            // get position within sprite sheet
            x += spriteRectangle.X;
            y += spriteRectangle.Y;

            // return coordinates
            return new Vector2(x, y);
        }

        /// <summary>Get a pixel from a sprite sheet.</summary>
        /// <typeparam name="TPixel">The pixel value type.</typeparam>
        /// <param name="spriteSheet">The sprite sheet.</param>
        /// <param name="position">The position of the pixel within the sprite sheet.</param>
        public TPixel GetSpriteSheetPixel<TPixel>(Texture2D spriteSheet, Vector2 position) where TPixel : struct
        {
            // get pixel index
            int x = (int)position.X;
            int y = (int)position.Y;
            int spriteIndex = y * spriteSheet.Width + x; // (pixels in preceding rows) + (preceding pixels in current row)

            // get pixel
            TPixel[] pixels = new TPixel[spriteSheet.Width * spriteSheet.Height];
            spriteSheet.GetData(pixels);
            return pixels[spriteIndex];
        }

        /// <summary>Get the sprite for an item.</summary>
        /// <param name="item">The item.</param>
        /// <param name="onlyCustom">Only return the sprite info if it's custom.</param>
        /// <returns>Returns a tuple containing the sprite sheet and the sprite's position and dimensions within the sheet.</returns>
        public SpriteInfo GetSprite(Item item, bool onlyCustom = false)
        {
            SObject obj = item as SObject;

            // Custom Farming Redux
            if (obj != null && this.CustomFarmingRedux.IsLoaded)
            {
                SpriteInfo data = this.CustomFarmingRedux.GetSprite(obj);
                if (data != null)
                    return data;
            }

            if (onlyCustom)
                return null;

            // standard object
            if (obj != null)
            {
                return obj.bigCraftable.Value
                    ? new SpriteInfo(Game1.bigCraftableSpriteSheet, SObject.getSourceRectForBigCraftable(obj.ParentSheetIndex))
                    : new SpriteInfo(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, obj.ParentSheetIndex, SObject.spriteSheetTileSize, SObject.spriteSheetTileSize));
            }

            // boots or ring
            if (item is Boots || item is Ring)
            {
                int indexInTileSheet = (item as Boots)?.indexInTileSheet ?? ((Ring)item).indexInTileSheet;
                return new SpriteInfo(Game1.objectSpriteSheet, Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, indexInTileSheet, SObject.spriteSheetTileSize, SObject.spriteSheetTileSize));
            }

            // unknown item
            return null;
        }


        /****
        ** UI
        ****/
        /// <summary>Draw a pretty hover box for the given text.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="label">The text to display.</param>
        /// <param name="position">The position at which to draw the text.</param>
        /// <param name="wrapWidth">The maximum width to display.</param>
        public Vector2 DrawHoverBox(SpriteBatch spriteBatch, string label, Vector2 position, float wrapWidth)
        {
            return CommonHelper.DrawHoverBox(spriteBatch, label, position, wrapWidth);
        }

        /// <summary>Show an informational message to the player.</summary>
        /// <param name="message">The message to show.</param>
        public void ShowInfoMessage(string message)
        {
            CommonHelper.ShowInfoMessage(message);
        }

        /// <summary>Show an error message to the player.</summary>
        /// <param name="message">The message to show.</param>
        public void ShowErrorMessage(string message)
        {
            CommonHelper.ShowErrorMessage(message);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get an NPC's preference for an item.</summary>
        /// <param name="npc">The NPC whose gift taste to get.</param>
        /// <param name="item">The item to check.</param>
        /// <returns>Returns the NPC's gift taste if applicable, else <c>null</c>.</returns>
        private GiftTaste? GetGiftTaste(NPC npc, Item item)
        {
            try
            {
                return (GiftTaste)npc.getGiftTasteForThisItem(item);
            }
            catch
            {
                // fails for non-social NPCs
                return null;
            }
        }
    }
}
