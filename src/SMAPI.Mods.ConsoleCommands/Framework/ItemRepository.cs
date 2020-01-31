using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;
using StardewValley;
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
            IEnumerable<SearchableItem> GetAllRaw()
            {
                // get tools
                for (int quality = Tool.stone; quality <= Tool.iridium; quality++)
                {
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.axe, () => ToolFactory.getToolFromDescription(ToolFactory.axe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.hoe, () => ToolFactory.getToolFromDescription(ToolFactory.hoe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.pickAxe, () => ToolFactory.getToolFromDescription(ToolFactory.pickAxe, quality));
                    yield return this.TryCreate(ItemType.Tool, ToolFactory.wateringCan, () => ToolFactory.getToolFromDescription(ToolFactory.wateringCan, quality));
                    if (quality != Tool.iridium)
                        yield return this.TryCreate(ItemType.Tool, ToolFactory.fishingRod, () => ToolFactory.getToolFromDescription(ToolFactory.fishingRod, quality));
                }
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset, () => new MilkPail()); // these don't have any sort of ID, so we'll just assign some arbitrary ones
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 1, () => new Shears());
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 2, () => new Pan());
                yield return this.TryCreate(ItemType.Tool, this.CustomIDOffset + 3, () => new Wand());

                // clothing
                foreach (int id in Game1.clothingInformation.Keys)
                    yield return this.TryCreate(ItemType.Clothing, id, () => new Clothing(id));

                // wallpapers
                for (int id = 0; id < 112; id++)
                    yield return this.TryCreate(ItemType.Wallpaper, id, () => new Wallpaper(id) { Category = SObject.furnitureCategory });

                // flooring
                for (int id = 0; id < 56; id++)
                    yield return this.TryCreate(ItemType.Flooring, id, () => new Wallpaper(id, isFloor: true) { Category = SObject.furnitureCategory });

                // equipment
                foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\Boots").Keys)
                    yield return this.TryCreate(ItemType.Boots, id, () => new Boots(id));
                foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\hats").Keys)
                    yield return this.TryCreate(ItemType.Hat, id, () => new Hat(id));

                // weapons
                foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\weapons").Keys)
                {
                    yield return this.TryCreate(ItemType.Weapon, id, () => (id >= 32 && id <= 34)
                        ? (Item)new Slingshot(id)
                        : new MeleeWeapon(id)
                    );
                }

                // furniture
                foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\Furniture").Keys)
                {
                    if (id == 1466 || id == 1468)
                        yield return this.TryCreate(ItemType.Furniture, id, () => new TV(id, Vector2.Zero));
                    else
                        yield return this.TryCreate(ItemType.Furniture, id, () => new Furniture(id, Vector2.Zero));
                }

                // craftables
                foreach (int id in Game1.bigCraftablesInformation.Keys)
                    yield return this.TryCreate(ItemType.BigCraftable, id, () => new SObject(Vector2.Zero, id));

                // objects
                foreach (int id in Game1.objectInformation.Keys)
                {
                    string[] fields = Game1.objectInformation[id]?.Split('/');

                    // secret notes
                    if (id == 79)
                    {
                        foreach (int secretNoteId in Game1.content.Load<Dictionary<int, string>>("Data\\SecretNotes").Keys)
                        {
                            yield return this.TryCreate(ItemType.Object, this.CustomIDOffset + secretNoteId, () =>
                            {
                                SObject note = new SObject(79, 1);
                                note.name = $"{note.name} #{secretNoteId}";
                                return note;
                            });
                        }
                    }

                    // ring
                    else if (id != 801 && fields?.Length >= 4 && fields[3] == "Ring") // 801 = wedding ring, which isn't an equippable ring
                        yield return this.TryCreate(ItemType.Ring, id, () => new Ring(id));

                    // item
                    else
                    {
                        // spawn main item
                        SObject item = null;
                        yield return this.TryCreate(ItemType.Object, id, () =>
                        {
                            return item = (id == 812 // roe
                                ? new ColoredObject(id, 1, Color.White)
                                : new SObject(id, 1)
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
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 2 + id, () => new SObject(348, 1)
                                {
                                    Name = $"{item.Name} Wine",
                                    Price = item.Price * 3,
                                    preserve = { SObject.PreserveType.Wine },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });

                                // jelly
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 3 + id, () => new SObject(344, 1)
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
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 4 + id, () => new SObject(350, 1)
                                {
                                    Name = $"{item.Name} Juice",
                                    Price = (int)(item.Price * 2.25d),
                                    preserve = { SObject.PreserveType.Juice },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });

                                // pickled
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + id, () => new SObject(342, 1)
                                {
                                    Name = $"Pickled {item.Name}",
                                    Price = 50 + item.Price * 2,
                                    preserve = { SObject.PreserveType.Pickle },
                                    preservedParentSheetIndex = { item.ParentSheetIndex }
                                });
                                break;

                            // flower honey
                            case SObject.flowersCategory:
                                yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 5 + id, () =>
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
                            case SObject.sellAtFishShopCategory when id == 812:
                                foreach (var pair in Game1.objectInformation)
                                {
                                    // get input
                                    SObject input = this.TryCreate(ItemType.Object, -1, () => new SObject(pair.Key, 1))?.Item as SObject;
                                    if (input == null || input.Category != SObject.FishCategory)
                                        continue;
                                    Color color = TailoringMenu.GetDyeColor(input) ?? Color.Orange;

                                    // yield roe
                                    SObject roe = null;
                                    yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + id, () =>
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
                                        yield return this.TryCreate(ItemType.Object, this.CustomIDOffset * 7 + id, () => new ColoredObject(447, 1, color)
                                        {
                                            name = $"Aged {input.Name} Roe",
                                            Category = -27,
                                            preserve = { Value = SObject.PreserveType.AgedRoe },
                                            preservedParentSheetIndex = { Value = input.ParentSheetIndex },
                                            Price = roe.Price * 2
                                        });
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
        /// <summary>Create a searchable item if valid.</summary>
        /// <param name="type">The item type.</param>
        /// <param name="id">The unique ID (if different from the item's parent sheet index).</param>
        /// <param name="createItem">Create an item instance.</param>
        private SearchableItem TryCreate(ItemType type, int id, Func<Item> createItem)
        {
            try
            {
                return new SearchableItem(type, id, createItem());
            }
            catch
            {
                return null; // if some item data is invalid, just don't include it
            }
        }
    }
}
