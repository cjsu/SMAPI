using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;
using StardewValley;
using StardewValley.GameData.FishPond;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace StardewModdingAPI.Mods.ConsoleCommands.Framework
{
    /// <summary>Provides methods for searching and constructing items.</summary>
    internal class ItemRepository
    {
        /*********
        ** Fields
        *********/
        /// <summary>The custom ID offset for items don't have a unique ID in the game.</summary>
        private readonly int CustomIDOffset = 1000;


        /*********
        ** Public methods
        *********/
        /// <summary>Get all spawnable items.</summary>
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "TryCreate invokes the lambda immediately.")]
        public IEnumerable<SearchableItem> GetAll()
        {
            //
            //
            // Be careful about closure variable capture here!
            //
            // SearchableItem stores the Func<Item> to create new instances later. Loop variables passed into the
            // function will be captured, so every func in the loop will use the value from the last iteration. Use the
            // TryCreate(type, id, entity => item) form to avoid the issue, or create a local variable to pass in.
            //
            //


            IEnumerable<SearchableItem> GetAllRaw()
            {
                // get tools
                for (int q = Tool.stone; q <= Tool.iridium; q++)
                {
                    int quality = q;

                    yield return this.TryCreate(ItemType.Tool, ToolFactory.axe, _ => ToolFactory.getToolFromDescription(ToolFactory.axe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.hoe, _ => ToolFactory.getToolFromDescription(ToolFactory.hoe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.pickAxe, _ => ToolFactory.getToolFromDescription(ToolFactory.pickAxe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.wateringCan, _ => ToolFactory.getToolFromDescription(ToolFactory.wateringCan, quality));
                    if (quality != Tool.iridium)
                        yield return this.TryCreate(ItemType.Tool, ToolFactory.fishingRod, _ => ToolFactory.getToolFromDescription(ToolFactory.fishingRod, quality));
                }
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset, _ => new MilkPail()); // these don't have any sort of ID, so we'll just assign some arbitrary ones
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 1, _ => new Shears());
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 2, _ => new Pan());
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 3, _ => new Wand());

                // clothing
                {
                    // items
                    HashSet<int> clothingIds = new HashSet<int>();
                    foreach (int id in Game1.clothingInformation.Keys)
                    {
                        if (id < 0)
                            continue; // placeholder data for character customization clothing below

                        clothingIds.Add(id);
                        yield return this.TryCreate(ItemType.Clothing, id, p => new Clothing(p.ID));
                    }

                    // character customization shirts (some shirts in this range have no data, but game has special logic to handle them)
                    for (int id = 1000; id <= 1111; id++)
                    {
                        if (!clothingIds.Contains(id))
                            yield return this.TryCreate(ItemType.Clothing, id, p => new Clothing(p.ID));
                    }
                }

                // wallpapers
                for (int id = 0; id < 112; id++)
                    yield return this.TryCreate(ItemType.Wallpaper, id, p => new Wallpaper(p.ID) { Category = SObject.furnitureCategory });

                // flooring
                for (int id = 0; id < 56; id++)
                    yield return this.TryCreate(ItemType.Flooring, id, p => new Wallpaper(p.ID, isFloor: true) { Category = SObject.furnitureCategory });

                // equipment
                foreach (int id in this.TryLoad<int, string>("Data\\Boots").Keys)
                    yield return this.TryCreate(ItemType.Boots, id, p => new Boots(p.ID));
                foreach (int id in this.TryLoad<int, string>("Data\\hats").Keys)
                    yield return this.TryCreate(ItemType.Hat, id, p => new Hat(p.ID));

                // weapons
                foreach (int id in this.TryLoad<int, string>("Data\\weapons").Keys)
                {
                    yield return this.TryCreate(ItemType.Weapon, id, p => (p.ID >= 32 && p.ID <= 34)
                        ? (Item)new Slingshot(p.ID)
                        : new MeleeWeapon(p.ID)
                    );
                }

                // furniture
                foreach (int id in this.TryLoad<int, string>("Data\\Furniture").Keys)
                    yield return this.TryCreate(ItemType.Furniture, id, p => Furniture.GetFurnitureInstance(p.ID));

                // craftables
                foreach (int id in Game1.bigCraftablesInformation.Keys)
                    yield return this.TryCreate(ItemType.BigCraftable, id, p => new SObject(Vector2.Zero, p.ID));

                // objects
                foreach (int id in Game1.objectInformation.Keys)
                {
                    string[] fields = Game1.objectInformation[id]?.Split('/');

                    // secret notes
                    if (id == 79)
                    {
                        foreach (int secretNoteId in this.TryLoad<int, string>("Data\\SecretNotes").Keys)
                        {
                            yield return this.TryCreate(ItemType.Object, this.CustomIDOffset + secretNoteId, _ =>
                            {
                                SObject note = new SObject(79, 1);
                                note.name = $"{note.name} #{secretNoteId}";
                                return note;
                            });
                        }
                    }

                    // ring
                    else if (id != 801 && fields?.Length >= 4 && fields[3] == "Ring") // 801 = wedding ring, which isn't an equippable ring
                        yield return this.TryCreate(ItemType.Ring, id, p => new Ring(p.ID));

                    // item
                    else
                    {
                        // spawn main item
                        SObject item = null;
                        yield return this.TryCreate(ItemType.Object, id, p =>
                        {
                            return item = (p.ID == 812 // roe
                                ? new ColoredObject(p.ID, 1, Color.White)
                                : new SObject(p.ID, 1)
                            );
                        });
                        if (item == null)
                            continue;

                        // flavored items
                        switch (item.Category)
                        {
                            // fruit products
                            case SObject.FruitsCategory:
                                // wine
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 2 + item.ParentSheetIndex, _ => new SObject(348, 1)
                                {
                                    Name = $"{item.Name} Wine",
                                    Price = item.Price * 3,
                                    preserve = { SObject.PreserveType.Wine },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });

                                // jelly
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 3 + item.ParentSheetIndex, _ => new SObject(344, 1)
                                {
                                    Name = $"{item.Name} Jelly",
                                    Price = 50 + item.Price * 2,
                                    preserve = { SObject.PreserveType.Jelly },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });
                                break;

                            // vegetable products
                            case SObject.VegetableCategory:
                                // juice
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 4 + item.ParentSheetIndex, _ => new SObject(350, 1)
                                {
                                    Name = $"{item.Name} Juice",
                                    Price = (int)(item.Price * 2.25d),
                                    preserve = { SObject.PreserveType.Juice },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });

                                // pickled
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + item.ParentSheetIndex, _ => new SObject(342, 1)
                                {
                                    Name = $"Pickled {item.Name}",
                                    Price = 50 + item.Price * 2,
                                    preserve = { SObject.PreserveType.Pickle },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });
                                break;

                            // flower honey
                            case SObject.flowersCategory:
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + item.ParentSheetIndex, _ =>
                                {
                                    SObject honey = new SObject(Vector2.Zero, 340, $"{item.Name} Honey", false, true, false, false)
                                    {
                                        Name = $"{item.Name} Honey",
                                        preservedParentSheetIndex = { item.ParentSheetIndex }
                                    };
                                    honey.Price += item.Price * 2;
                                    return honey;
                                });
                                break;

                            // roe and aged roe (derived from FishPond.GetFishProduce)
                            case SObject.sellAtFishShopCategory when item.ParentSheetIndex == 812:
                                {
                                    this.GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags);

                                    foreach (var pair in Game1.objectInformation)
                                    {
                                        // get input
                                        SObject input = this.TryCreate(ItemType.Object, pair.Key, p => new SObject(p.ID, 1))?.Item as SObject;
                                        var inputTags = input?.GetContextTags();
                                        if (inputTags?.Any() != true)
                                            continue;

                                        // check if roe-producing fish
                                        if (!inputTags.Any(tag => simpleTags.Contains(tag)) && !complexTags.Any(set => set.All(tag => input.HasContextTag(tag))))
                                            continue;

                                        // yield roe
                                        SObject roe = null;
                                        Color color = this.GetRoeColor(input);
                                        yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + item.ParentSheetIndex, _ =>
                                        {
                                            roe = new ColoredObject(812, 1, color)
                                            {
                                                name = $"{input.Name} Roe",
                                                preserve = { Value = SObject.PreserveType.Roe },
                                                preservedParentSheetIndex = { Value = input.ParentSheetIndex }
                                            };
                                            roe.Price += input.Price / 2;
                                            return roe;
                                        });

                                        // aged roe
                                        if (roe != null && pair.Key != 698) // aged sturgeon roe is caviar, which is a separate item
                                        {
                                            yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + item.ParentSheetIndex, _ => new ColoredObject(447, 1, color)
                                            {
                                                name = $"Aged {input.Name} Roe",
                                                Category = -27,
                                                preserve = { Value = SObject.PreserveType.AgedRoe },
                                                preservedParentSheetIndex = { Value = input.ParentSheetIndex },
                                                Price = roe.Price * 2
                                            });
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
            }

            return GetAllRaw().Where(p => p != null);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get optimized lookups to match items which produce roe in a fish pond.</summary>
        /// <param name="simpleTags">A lookup of simple singular tags which match a roe-producing fish.</param>
        /// <param name="complexTags">A list of tag sets which match roe-producing fish.</param>
        private void GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags)
        {
            simpleTags = new HashSet<string>();
            complexTags = new List<List<string>>();

            foreach (FishPondData data in Game1.content.Load<List<FishPondData>>("Data\\FishPondData"))
            {
                if (data.ProducedItems.All(p => p.ItemID != 812))
                    continue; // doesn't produce roe

                if (data.RequiredTags.Count == 1 && !data.RequiredTags[0].StartsWith("!"))
                    simpleTags.Add(data.RequiredTags[0]);
                else
                    complexTags.Add(data.RequiredTags);
            }
        }

        /// <summary>Try to load a data file, and return empty data if it's invalid.</summary>
        /// <typeparam name="TKey">The asset key type.</typeparam>
        /// <typeparam name="TValue">The asset value type.</typeparam>
        /// <param name="assetName">The data asset name.</param>
        private Dictionary<TKey, TValue> TryLoad<TKey, TValue>(string assetName)
        {
            try
            {
                return Game1.content.Load<Dictionary<TKey, TValue>>(assetName);
            }
            catch (ContentLoadException)
            {
                // generally due to a player incorrectly replacing a data file with an XNB mod
                return new Dictionary<TKey, TValue>();
            }
        }

        /// <summary>Create a searchable item if valid.</summary>
        /// <param name="type">The item type.</param>
        /// <param name="id">The unique ID (if different from the item's parent sheet index).</param>
        /// <param name="createItem">Create an item instance.</param>
        private SearchableItem TryCreate(ItemType type, int id, Func<SearchableItem, Item> createItem)
        {
            try
            {
                var item = new SearchableItem(type, id, createItem);
                item.Item.getDescription(); // force-load item data, so it crashes here if it's invalid
                return item;
            }
            catch
            {
                return null; // if some item data is invalid, just don't include it
            }
        }

        /// <summary>Get the color to use a given fish's roe.</summary>
        /// <param name="fish">The fish whose roe to color.</param>
        /// <remarks>Derived from <see cref="StardewValley.Buildings.FishPond.GetFishProduce"/>.</remarks>
        private Color GetRoeColor(SObject fish)
        {
            return fish.ParentSheetIndex == 698 // sturgeon
                ? new Color(61, 55, 42)
                : (TailoringMenu.GetDyeColor(fish) ?? Color.Orange);
        }
    }
}
