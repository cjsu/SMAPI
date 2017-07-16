using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using TrainerMod.Framework.ItemData;
using SObject = StardewValley.Object;

namespace TrainerMod.Framework
{
    /// <summary>Provides methods for searching and constructing items.</summary>
    internal class ItemRepository
    {
        /*********
        ** Properties
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
            for (int quality = Tool.stone; quality <= Tool.iridium; quality++)
            {
                yield return new SearchableItem(ItemType.Tool, ToolFactory.axe, ToolFactory.getToolFromDescription(ToolFactory.axe, quality));
                yield return new SearchableItem(ItemType.Tool, ToolFactory.hoe, ToolFactory.getToolFromDescription(ToolFactory.hoe, quality));
                yield return new SearchableItem(ItemType.Tool, ToolFactory.pickAxe, ToolFactory.getToolFromDescription(ToolFactory.pickAxe, quality));
                yield return new SearchableItem(ItemType.Tool, ToolFactory.wateringCan, ToolFactory.getToolFromDescription(ToolFactory.wateringCan, quality));
                if (quality != Tool.iridium)
                    yield return new SearchableItem(ItemType.Tool, ToolFactory.fishingRod, ToolFactory.getToolFromDescription(ToolFactory.fishingRod, quality));
            }
            yield return new SearchableItem(ItemType.Tool, this.CustomIDOffset, new MilkPail()); // these don't have any sort of ID, so we'll just assign some arbitrary ones
            yield return new SearchableItem(ItemType.Tool, this.CustomIDOffset + 1, new Shears());
            yield return new SearchableItem(ItemType.Tool, this.CustomIDOffset + 2, new Pan());

            // wallpapers
            for (int id = 0; id < 112; id++)
                yield return new SearchableItem(ItemType.Wallpaper, id, new Wallpaper(id));

            // flooring
            for (int id = 0; id < 40; id++)
                yield return new SearchableItem(ItemType.Flooring, id, new Wallpaper(id, isFloor: true));

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

            // fish
            foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\Fish").Keys)
                yield return new SearchableItem(ItemType.Fish, id, new SObject(id, 999));

            // craftables
            foreach (int id in Game1.bigCraftablesInformation.Keys)
                yield return new SearchableItem(ItemType.BigCraftable, id, new SObject(Vector2.Zero, id));

            // objects
            foreach (int id in Game1.objectInformation.Keys)
            {
                if (id >= Ring.ringLowerIndexRange && id <= Ring.ringUpperIndexRange)
                    continue; // handled separated

                SObject item = new SObject(id, 1);
                yield return new SearchableItem(ItemType.Object, id, item);

                // fruit products
                if (item.category == SObject.FruitsCategory)
                {
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset + id, new SObject(348, 1)
                    {
                        name = $"{item.Name} Wine",
                        price = item.price * 3,
                        preserve = SObject.PreserveType.Wine,
                        preservedParentSheetIndex = item.parentSheetIndex
                    });
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 2 + id, new SObject(344, 1)
                    {
                        name = $"{item.Name} Jelly",
                        price = 50 + item.Price * 2,
                        preserve = SObject.PreserveType.Jelly,
                        preservedParentSheetIndex = item.parentSheetIndex
                    });
                }

                // vegetable products
                else if (item.category == SObject.VegetableCategory)
                {
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 3 + id, new SObject(350, 1)
                    {
                        name = $"{item.Name} Juice",
                        price = (int)(item.price * 2.25d),
                        preserve = SObject.PreserveType.Juice,
                        preservedParentSheetIndex = item.parentSheetIndex
                    });
                    yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 4 + id, new SObject(342, 1)
                    {
                        name = $"Pickled {item.Name}",
                        price = 50 + item.Price * 2,
                        preserve = SObject.PreserveType.Pickle,
                        preservedParentSheetIndex = item.parentSheetIndex
                    });
                }

                // flower honey
                else if (item.category == SObject.flowersCategory)
                {
                    // get honey type
                    SObject.HoneyType? type = null;
                    switch (item.parentSheetIndex)
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
                            name = "Wild Honey",
                            honeyType = type
                        };
                        if (type != SObject.HoneyType.Wild)
                        {
                            honey.name = $"{item.Name} Honey";
                            honey.price += item.price * 2;
                        }
                        yield return new SearchableItem(ItemType.Object, this.CustomIDOffset * 5 + id, honey);
                    }
                }
            }
        }
    }
}
