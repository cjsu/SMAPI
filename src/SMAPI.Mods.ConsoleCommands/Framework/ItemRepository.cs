using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Mods.ConsoleCommands.Framework.ItemData;
using StardewValley;
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
        public IEnumerable<SearchableItem> GetAll()
        {
            // get tools
            yield return new SearchableItem(ItemType.Tool, ToolFactory.axe, ToolFactory.getToolFromDescription(ToolFactory.axe, 0));
            yield return new SearchableItem(ItemType.Tool, ToolFactory.hoe, ToolFactory.getToolFromDescription(ToolFactory.hoe, 0));
            yield return new SearchableItem(ItemType.Tool, ToolFactory.pickAxe, ToolFactory.getToolFromDescription(ToolFactory.pickAxe, 0));
            yield return new SearchableItem(ItemType.Tool, ToolFactory.wateringCan, ToolFactory.getToolFromDescription(ToolFactory.wateringCan, 0));
            yield return new SearchableItem(ItemType.Tool, ToolFactory.fishingRod, ToolFactory.getToolFromDescription(ToolFactory.fishingRod, 0));
            yield return new SearchableItem(ItemType.Tool, this.CustomIDOffset, new MilkPail()); // these don't have any sort of ID, so we'll just assign some arbitrary ones
            yield return new SearchableItem(ItemType.Tool, this.CustomIDOffset + 1, new Shears());
            yield return new SearchableItem(ItemType.Tool, this.CustomIDOffset + 2, new Pan());
            yield return new SearchableItem(ItemType.Tool, this.CustomIDOffset + 3, new Wand());

            // clothing
            foreach (int id in Game1.clothingInformation.Keys)
                yield return new SearchableItem(ItemType.Clothing, id, new Clothing(id));

            // wallpapers
            for (int id = 0; id < 112; id++)
                yield return new SearchableItem(ItemType.Wallpaper, id, new Wallpaper(id) { Category = SObject.furnitureCategory });

            // flooring
            for (int id = 0; id < 40; id++)
                yield return new SearchableItem(ItemType.Flooring, id, new Wallpaper(id, isFloor: true) { Category = SObject.furnitureCategory });

            // equipment
            foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\Boots").Keys)
                yield return new SearchableItem(ItemType.Boots, id, new Boots(id));
            foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\hats").Keys)
                yield return new SearchableItem(ItemType.Hat, id, new Hat(id));
            foreach (int id in Game1.objectInformation.Keys)
            {
                if (id >= Ring.ringLowerIndexRange && id <= Ring.ringUpperIndexRange)
                    yield return new SearchableItem(ItemType.Ring, id, new Ring(id));
            }

            // weapons
            foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\weapons").Keys)
            {
                Item weapon = (id >= 32 && id <= 34)
                    ? (Item)new Slingshot(id)
                    : new MeleeWeapon(id);
                yield return new SearchableItem(ItemType.Weapon, id, weapon);
            }

            // furniture
            foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\Furniture").Keys)
            {
                if (id == 1466 || id == 1468)
                    yield return new SearchableItem(ItemType.Furniture, id, new TV(id, Vector2.Zero));
                else
                    yield return new SearchableItem(ItemType.Furniture, id, new Furniture(id, Vector2.Zero));
            }

            // craftables
            foreach (int id in Game1.bigCraftablesInformation.Keys)
                yield return new SearchableItem(ItemType.BigCraftable, id, new SObject(Vector2.Zero, id));

            // secret notes
            foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\SecretNotes").Keys)
            {
                SObject note = new SObject(79, 1);
                note.name = $"{note.name} #{id}";
                yield return new SearchableItem(ItemType.Object, this.CustomIDOffset + id, note);
            }

            // objects
            foreach (int id in Game1.objectInformation.Keys)
            {
                if (id == 79)
                    continue; // secret note handled above
                if (id >= Ring.ringLowerIndexRange && id <= Ring.ringUpperIndexRange)
                    continue; // handled separated

                SObject item = new SObject(id, 1);
                yield return new SearchableItem(ItemType.Object, id, item);

                // fruit products
                if (item.Category == SObject.FruitsCategory)
                {
                    // wine
                    SObject wine = new SObject(348, 1)
                    {
                        Name = $"{item.Name} Wine",
                        Price = item.Price * 3
                    };
                    wine.preserve.Value = SObject.PreserveType.Wine;
                    wine.preservedParentSheetIndex.Value = item.ParentSheetIndex;
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 2 + id, wine);

                    // jelly
                    SObject jelly = new SObject(344, 1)
                    {
                        Name = $"{item.Name} Jelly",
                        Price = 50 + item.Price * 2
                    };
                    jelly.preserve.Value = SObject.PreserveType.Jelly;
                    jelly.preservedParentSheetIndex.Value = item.ParentSheetIndex;
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 3 + id, jelly);
                }

                // vegetable products
                else if (item.Category == SObject.VegetableCategory)
                {
                    // juice
                    SObject juice = new SObject(350, 1)
                    {
                        Name = $"{item.Name} Juice",
                        Price = (int)(item.Price * 2.25d)
                    };
                    juice.preserve.Value = SObject.PreserveType.Juice;
                    juice.preservedParentSheetIndex.Value = item.ParentSheetIndex;
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 4 + id, juice);

                    // pickled
                    SObject pickled = new SObject(342, 1)
                    {
                        Name = $"Pickled {item.Name}",
                        Price = 50 + item.Price * 2
                    };
                    pickled.preserve.Value = SObject.PreserveType.Pickle;
                    pickled.preservedParentSheetIndex.Value = item.ParentSheetIndex;
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 5 + id, pickled);
                }

                // flower honey
                else if (item.Category == SObject.flowersCategory)
                {
                    // get honey type
                    SObject.HoneyType? type = null;
                    switch (item.ParentSheetIndex)
                    {
                        case 376:
                            type = SObject.HoneyType.Poppy;
                            break;
                        case 591:
                            type = SObject.HoneyType.Tulip;
                            break;
                        case 593:
                            type = SObject.HoneyType.SummerSpangle;
                            break;
                        case 595:
                            type = SObject.HoneyType.FairyRose;
                            break;
                        case 597:
                            type = SObject.HoneyType.BlueJazz;
                            break;
                        case 421: // sunflower standing in for all other flowers
                            type = SObject.HoneyType.Wild;
                            break;
                    }

                    // yield honey
                    if (type != null)
                    {
                        SObject honey = new SObject(Vector2.Zero, 340, item.Name + " Honey", false, true, false, false)
                        {
                            Name = "Wild Honey"
                        };
                        honey.honeyType.Value = type;

                        if (type != SObject.HoneyType.Wild)
                        {
                            honey.Name = $"{item.Name} Honey";
                            honey.Price += item.Price * 2;
                        }
                        yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 5 + id, honey);
                    }
                }
            }
        }
    }
}
