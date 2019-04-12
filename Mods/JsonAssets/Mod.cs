using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JsonAssets.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using StardewValley.Objects;
using System.Reflection;
using Netcode;
using StardewValley.Buildings;
using Harmony;
using System.Text.RegularExpressions;
using JsonAssets.Overrides;
using Newtonsoft.Json;
using StardewValley.Tools;

// TODO: Refactor recipes

namespace JsonAssets
{
    public class Mod : StardewModdingAPI.Mod
    {
        public static Mod instance;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            instance = this;

            helper.Events.Display.MenuChanged += this.onMenuChanged;
            helper.Events.GameLoop.Saved += this.onSaved;
            helper.Events.Player.InventoryChanged += this.onInventoryChanged;
            helper.Events.GameLoop.SaveCreated += this.onCreated;
            helper.Events.Specialised.LoadStageChanged += this.onLoadStageChanged;
            helper.Events.Multiplayer.PeerContextReceived += this.clientConnected;

            Log.info("Loading content packs...");
            foreach (IContentPack contentPack in this.Helper.ContentPacks.GetOwned())
                this.loadData(contentPack);
            if (Directory.Exists(Path.Combine(this.Helper.DirectoryPath, "ContentPacks")))
            {
                foreach (string dir in Directory.EnumerateDirectories(Path.Combine(this.Helper.DirectoryPath, "ContentPacks")))
                    this.loadData(dir);
            }

            this.resetAtTitle();

            try
            {
                helper.Events.Hook.ObjectCanBePlacedHere += (args) => ObjectCanPlantHereOverride.Prefix(args.__instance, args.location, args.tile, ref args.__result);
                helper.Events.Hook.ObjectCheckForAction += (args) => ObjectNoActionHook.Prefix(args.__instance);
                helper.Events.Hook.ObjectIsIndexOkForBasicShippedCategory += (args) => { ObjectCollectionShippingHook.Postfix(args.index, ref args.__result); return true; };
            }
            catch (Exception e)
            {
                Log.error($"Exception doing harmony stuff: {e}");
            }
        }

        private Api api;
        public override object GetApi()
        {
            return this.api ?? (this.api = new Api(this.loadData));
        }

        private void loadData(string dir)
        {
            // read initial info
            IContentPack temp = this.Helper.ContentPacks.CreateFake(dir);
            ContentPackData info = temp.ReadJsonFile<ContentPackData>("content-pack.json");
            if (info == null)
            {
                Log.warn($"\tNo {dir}/content-pack.json!");
                return;
            }

            // load content pack
            IContentPack contentPack = this.Helper.ContentPacks.CreateTemporary(dir, id: Guid.NewGuid().ToString("N"), name: info.Name, description: info.Description, author: info.Author, version: new SemanticVersion(info.Version));
            this.loadData(contentPack);
        }

        private Dictionary<string, IContentPack> dupObjects = new Dictionary<string, IContentPack>();
        private Dictionary<string, IContentPack> dupCrops = new Dictionary<string, IContentPack>();
        private Dictionary<string, IContentPack> dupFruitTrees = new Dictionary<string, IContentPack>();
        private Dictionary<string, IContentPack> dupBigCraftables = new Dictionary<string, IContentPack>();
        private Dictionary<string, IContentPack> dupHats = new Dictionary<string, IContentPack>();
        private Dictionary<string, IContentPack> dupWeapons = new Dictionary<string, IContentPack>();

        private readonly Regex SeasonLimiter = new Regex("(z(?: spring| summer| fall| winter){2,4})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private void loadData(IContentPack contentPack)
        {
            Log.info($"\t{contentPack.Manifest.Name} {contentPack.Manifest.Version} by {contentPack.Manifest.Author} - {contentPack.Manifest.Description}");

            // load objects
            DirectoryInfo objectsDir = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Objects"));
            if (objectsDir.Exists)
            {
                foreach (DirectoryInfo dir in objectsDir.EnumerateDirectories())
                {
                    string relativePath = $"Objects/{dir.Name}";

                    // load data
                    ObjectData obj = contentPack.ReadJsonFile<ObjectData>($"{relativePath}/object.json");
                    if (obj == null)
                        continue;

                    // save object
                    obj.texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/object.png");
                    if (obj.IsColored)
                        obj.textureColor = contentPack.LoadAsset<Texture2D>($"{relativePath}/color.png");
                    this.objects.Add(obj);

                    // save ring
                    if (obj.Category == ObjectData.Category_.Ring)
                        this.myRings.Add(obj);

                    // Duplicate check
                    if (this.dupObjects.ContainsKey(obj.Name))
                        Log.error($"Duplicate object: {obj.Name} just added by {contentPack.Manifest.Name}, already added by {this.dupObjects[obj.Name].Manifest.Name}!");
                    else
                        this.dupObjects[obj.Name] = contentPack;
                }
            }

            // load crops
            DirectoryInfo cropsDir = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Crops"));
            if (cropsDir.Exists)
            {
                foreach (DirectoryInfo dir in cropsDir.EnumerateDirectories())
                {
                    string relativePath = $"Crops/{dir.Name}";

                    // load data
                    CropData crop = contentPack.ReadJsonFile<CropData>($"{relativePath}/crop.json");
                    if (crop == null)
                        continue;

                    // save crop
                    crop.texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/crop.png");
                    this.crops.Add(crop);

                    // save seeds
                    crop.seed = new ObjectData
                    {
                        texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/seeds.png"),
                        Name = crop.SeedName,
                        Description = crop.SeedDescription,
                        Category = ObjectData.Category_.Seeds,
                        Price = crop.SeedPurchasePrice,
                        CanPurchase = true,
                        PurchaseFrom = crop.SeedPurchaseFrom,
                        PurchasePrice = crop.SeedPurchasePrice,
                        PurchaseRequirements = crop.SeedPurchaseRequirements ?? new List<string>(),
                        NameLocalization = crop.SeedNameLocalization,
                        DescriptionLocalization = crop.SeedDescriptionLocalization
                    };

                    // TODO: Clean up this chunk
                    // I copy/pasted it from the unofficial update decompiled
                    string str = "";
                    string[] array =  new[] { "spring", "summer", "fall", "winter" }
                        .Except(crop.Seasons)
                        .ToArray();
                    foreach (string season in array)
                    {
                        str += $"/z {season}";
                    }
                    string strtrimstart = str.TrimStart(new char[] { '/' });
                    if (crop.SeedPurchaseRequirements != null && crop.SeedPurchaseRequirements.Count > 0)
                    {
                        for (int index = 0; index < crop.SeedPurchaseRequirements.Count; index++)
                        {
                            if (this.SeasonLimiter.IsMatch(crop.SeedPurchaseRequirements[index]))
                            {
                                crop.SeedPurchaseRequirements[index] = strtrimstart;
                                Log.warn($"        Faulty season requirements for {crop.SeedName}!\n        Fixed season requirements: {crop.SeedPurchaseRequirements[index]}");
                            }
                        }
                        if (!crop.SeedPurchaseRequirements.Contains(str.TrimStart('/')))
                        {
                            Log.trace($"        Adding season requirements for {crop.SeedName}:\n        New season requirements: {strtrimstart}");
                            crop.seed.PurchaseRequirements.Add(strtrimstart);
                        }
                    }
                    else
                    {
                        Log.trace($"        Adding season requirements for {crop.SeedName}:\n        New season requirements: {strtrimstart}");
                        crop.seed.PurchaseRequirements.Add(strtrimstart);
                    }

                    this.objects.Add(crop.seed);

                    // Duplicate check
                    if (this.dupCrops.ContainsKey(crop.Name))
                        Log.error($"Duplicate crop: {crop.Name} just added by {contentPack.Manifest.Name}, already added by {this.dupCrops[crop.Name].Manifest.Name}!");
                    else
                        this.dupCrops[crop.Name] = contentPack;
                }
            }

            // load fruit trees
            DirectoryInfo fruitTreesDir = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "FruitTrees"));
            if (fruitTreesDir.Exists)
            {
                foreach (DirectoryInfo dir in fruitTreesDir.EnumerateDirectories())
                {
                    string relativePath = $"FruitTrees/{dir.Name}";

                    // load data
                    FruitTreeData tree = contentPack.ReadJsonFile<FruitTreeData>($"{relativePath}/tree.json");
                    if (tree == null)
                        continue;

                    // save fruit tree
                    tree.texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/tree.png");
                    this.fruitTrees.Add(tree);

                    // save seed
                    tree.sapling = new ObjectData
                    {
                        texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/sapling.png"),
                        Name = tree.SaplingName,
                        Description = tree.SaplingDescription,
                        Category = ObjectData.Category_.Seeds,
                        Price = tree.SaplingPurchasePrice,
                        CanPurchase = true,
                        PurchaseRequirements = tree.SaplingPurchaseRequirements,
                        PurchaseFrom = tree.SaplingPurchaseFrom,
                        PurchasePrice = tree.SaplingPurchasePrice,
                        NameLocalization = tree.SaplingNameLocalization,
                        DescriptionLocalization = tree.SaplingDescriptionLocalization
                    };
                    this.objects.Add(tree.sapling);

                    // Duplicate check
                    if (this.dupFruitTrees.ContainsKey(tree.Name))
                        Log.error($"Duplicate fruit tree: {tree.Name} just added by {contentPack.Manifest.Name}, already added by {this.dupFruitTrees[tree.Name].Manifest.Name}!");
                    else
                        this.dupFruitTrees[tree.Name] = contentPack;
                }
            }

            // load big craftables
            DirectoryInfo bigCraftablesDir = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "BigCraftables"));
            if (bigCraftablesDir.Exists)
            {
                foreach (DirectoryInfo dir in bigCraftablesDir.EnumerateDirectories())
                {
                    string relativePath = $"BigCraftables/{dir.Name}";

                    // load data
                    BigCraftableData craftable = contentPack.ReadJsonFile<BigCraftableData>($"{relativePath}/big-craftable.json");
                    if (craftable == null)
                        continue;

                    // save craftable
                    craftable.texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/big-craftable.png");
                    this.bigCraftables.Add(craftable);

                    // Duplicate check
                    if (this.dupBigCraftables.ContainsKey(craftable.Name))
                        Log.error($"Duplicate big craftable: {craftable.Name} just added by {contentPack.Manifest.Name}, already added by {this.dupBigCraftables[craftable.Name].Manifest.Name}!");
                    else
                        this.dupBigCraftables[craftable.Name] = contentPack;
                }
            }

            // load hats
            DirectoryInfo hatsDir = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Hats"));
            if (hatsDir.Exists)
            {
                foreach (DirectoryInfo dir in hatsDir.EnumerateDirectories())
                {
                    string relativePath = $"Hats/{dir.Name}";

                    // load data
                    HatData hat = contentPack.ReadJsonFile<HatData>($"{relativePath}/hat.json");
                    if (hat == null)
                        continue;

                    // save object
                    hat.texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/hat.png");
                    this.hats.Add(hat);

                    // Duplicate check
                    if (this.dupHats.ContainsKey(hat.Name))
                        Log.error($"Duplicate hat: {hat.Name} just added by {contentPack.Manifest.Name}, already added by {this.dupHats[hat.Name].Manifest.Name}!");
                    else
                        this.dupBigCraftables[hat.Name] = contentPack;
                }
            }

            // Load weapons
            // load objects
            DirectoryInfo weaponsDir = new DirectoryInfo(Path.Combine(contentPack.DirectoryPath, "Weapons"));
            if (weaponsDir.Exists)
            {
                foreach (DirectoryInfo dir in weaponsDir.EnumerateDirectories())
                {
                    string relativePath = $"Weapons/{dir.Name}";

                    // load data
                    WeaponData weapon = contentPack.ReadJsonFile<WeaponData>($"{relativePath}/weapon.json");
                    if (weapon == null)
                        continue;

                    // save object
                    weapon.texture = contentPack.LoadAsset<Texture2D>($"{relativePath}/weapon.png");
                    this.weapons.Add(weapon);

                    // Duplicate check
                    if (this.dupWeapons.ContainsKey(weapon.Name))
                        Log.error($"Duplicate weapon: {weapon.Name} just added by {contentPack.Manifest.Name}, already added by {this.dupWeapons[weapon.Name].Manifest.Name}!");
                    else
                        this.dupBigCraftables[weapon.Name] = contentPack;
                }
            }
        }

        private void resetAtTitle()
        {
            this.didInit = false;
            // When we go back to the title menu we need to reset things so things don't break when
            // going back to a save.
            this.clearIds(out this.objectIds, this.objects.ToList<DataNeedsId>());
            this.clearIds(out this.cropIds, this.crops.ToList<DataNeedsId>());
            this.clearIds(out this.fruitTreeIds, this.fruitTrees.ToList<DataNeedsId>());
            this.clearIds(out this.bigCraftableIds, this.bigCraftables.ToList<DataNeedsId>());
            this.clearIds(out this.hatIds, this.hats.ToList<DataNeedsId>());
            this.clearIds(out this.weaponIds, this.weapons.ToList<DataNeedsId>());

            IAssetEditor editor = this.Helper.Content.AssetEditors.FirstOrDefault(p => p is ContentInjector);
            if (editor != null)
                this.Helper.Content.AssetEditors.Remove(editor);
        }

        private void onCreated(object sender, SaveCreatedEventArgs e)
        {
            Log.debug("Loading stuff early (creation)");
            this.initStuff( loadIdFiles: false );
        }

        private void onLoadStageChanged(object sender, LoadStageChangedEventArgs e)
        {
            if (e.NewStage == StardewModdingAPI.Enums.LoadStage.SaveParsed)
            {
                Log.debug("Loading stuff early (loading)");
                this.initStuff( loadIdFiles: true );
            }
            else if ( e.NewStage == StardewModdingAPI.Enums.LoadStage.SaveLoadedLocations )
            {
                Log.debug("Fixing IDs");
                this.fixIdsEverywhere();
            }
            else if ( e.NewStage == StardewModdingAPI.Enums.LoadStage.Loaded )
            {
                Log.debug("Adding default recipes");
                foreach (ObjectData obj in this.objects)
                {
                    if (obj.Recipe != null && obj.Recipe.IsDefault && !Game1.player.knowsRecipe(obj.Name))
                    {
                        if (obj.Category == ObjectData.Category_.Cooking)
                        {
                            Game1.player.cookingRecipes.Add(obj.Name, 0);
                        }
                        else
                        {
                            Game1.player.craftingRecipes.Add(obj.Name, 0);
                        }
                    }
                }
                foreach (BigCraftableData big in this.bigCraftables)
                {
                    if (big.Recipe != null && big.Recipe.IsDefault && !Game1.player.knowsRecipe(big.Name))
                    {
                        Game1.player.craftingRecipes.Add(big.Name, 0);
                    }
                }
            }
        }

        private void clientConnected(object sender, PeerContextReceivedEventArgs e)
        {
            if (!Context.IsMainPlayer && !this.didInit)
            {
                Log.debug("Loading stuff early (MP client)");
                this.initStuff( loadIdFiles: false );
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void onMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if ( e.NewMenu == null )
                return;

            if ( e.NewMenu is TitleMenu )
            {
                this.resetAtTitle();
                return;
            }
            if (e.OldMenu is ShopMenu)
            {
                return;
            }

            ShopMenu menu = e.NewMenu as ShopMenu;
            bool hatMouse = menu != null && menu.potraitPersonDialogue == Game1.parseText(Game1.content.LoadString("Strings\\StringsFromCSFiles:ShopMenu.cs.11494"), Game1.dialogueFont, Game1.tileSize * 5 - Game1.pixelZoom * 4);
            if (menu == null || menu.portraitPerson == null && !hatMouse)
                return;

            //if (menu.portraitPerson.name == "Pierre")
            {
                Log.trace($"Adding objects to {menu.portraitPerson?.Name}'s shop");
                List<Item> forSale = this.Helper.Reflection.GetField<List<Item>>(menu, "forSale").GetValue();
                int count = forSale.Count;
                Dictionary<Item, int[]> itemPriceAndStock = this.Helper.Reflection.GetField<Dictionary<Item, int[]>>(menu, "itemPriceAndStock").GetValue();

                IReflectedMethod precondMeth = this.Helper.Reflection.GetMethod(Game1.currentLocation, "checkEventPrecondition");
                foreach (ObjectData obj in this.objects)
                {
                    if (obj.Recipe != null && obj.Recipe.CanPurchase)
                    {
                        bool add = true;
                        // Can't use continue here or the item might not sell
                        if (obj.Recipe.PurchaseFrom != menu.portraitPerson?.Name || (obj.Recipe.PurchaseFrom == "HatMouse" && hatMouse) )
                            add = false;
                        if (Game1.player.craftingRecipes.ContainsKey(obj.Name) || Game1.player.cookingRecipes.ContainsKey(obj.Name))
                            add = false;
                        if (obj.Recipe.PurchaseRequirements != null && obj.Recipe.PurchaseRequirements.Count > 0 &&
                            precondMeth.Invoke<int>(new object[] { obj.Recipe.GetPurchaseRequirementString() }) == -1)
                            add = false;
                        if (add)
                        {
                            StardewValley.Object recipeObj = new StardewValley.Object(obj.id, 1, true, obj.Recipe.PurchasePrice, 0);
                            forSale.Add(recipeObj);
                            itemPriceAndStock.Add(recipeObj, new int[] { obj.Recipe.PurchasePrice, 1 });
                            Log.trace($"\tAdding recipe for {obj.Name}");
                        }
                    }
                    if (!obj.CanPurchase)
                        continue;
                    if (obj.PurchaseFrom != menu.portraitPerson?.Name || (obj.PurchaseFrom == "HatMouse" && hatMouse))
                        continue;
                    if (obj.PurchaseRequirements != null && obj.PurchaseRequirements.Count > 0 &&
                        precondMeth.Invoke<int>(new object[] { obj.GetPurchaseRequirementString() }) == -1)
                        continue;
                    Item item = new StardewValley.Object(Vector2.Zero, obj.id, int.MaxValue);
                    forSale.Add(item);
                    itemPriceAndStock.Add(item, new int[] { obj.PurchasePrice, int.MaxValue });
                    Log.trace($"\tAdding {obj.Name}");
                }
                foreach (BigCraftableData big in this.bigCraftables)
                {
                    if (big.Recipe != null && big.Recipe.CanPurchase)
                    {
                        bool add = true;
                        // Can't use continue here or the item might not sell
                        if (big.Recipe.PurchaseFrom != menu.portraitPerson?.Name || (big.Recipe.PurchaseFrom == "HatMouse" && hatMouse))
                            add = false;
                        if (Game1.player.craftingRecipes.ContainsKey(big.Name) || Game1.player.cookingRecipes.ContainsKey(big.Name))
                            add = false;
                        if (big.Recipe.PurchaseRequirements != null && big.Recipe.PurchaseRequirements.Count > 0 &&
                            precondMeth.Invoke<int>(new object[] { big.Recipe.GetPurchaseRequirementString() }) == -1)
                            add = false;
                        if (add)
                        {
                            StardewValley.Object recipeObj = new StardewValley.Object(new Vector2(0, 0), big.id, true);
                            forSale.Add(recipeObj);
                            itemPriceAndStock.Add(recipeObj, new int[] { big.Recipe.PurchasePrice, 1 });
                            Log.trace($"\tAdding recipe for {big.Name}");
                        }
                    }
                    if (!big.CanPurchase)
                        continue;
                    if (big.PurchaseFrom != menu.portraitPerson?.Name || (big.PurchaseFrom == "HatMouse" && hatMouse))
                        continue;
                    if (big.PurchaseRequirements != null && big.PurchaseRequirements.Count > 0 &&
                        precondMeth.Invoke<int>(new object[] { big.GetPurchaseRequirementString() }) == -1)
                        continue;
                    Item item = new StardewValley.Object(Vector2.Zero, big.id, false);
                    forSale.Add(item);
                    itemPriceAndStock.Add(item, new int[] { big.PurchasePrice, int.MaxValue });
                    Log.trace($"\tAdding {big.Name}");
                }
                if ( hatMouse )
                {
                    foreach (HatData hat in this.hats )
                    {
                        Item item = new Hat(hat.GetHatId());
                        forSale.Add(item);
                        itemPriceAndStock.Add(item, new int[] { hat.PurchasePrice, int.MaxValue });
                        Log.trace($"\tAdding {hat.Name}");
                    }
                }
                foreach (WeaponData weapon in this.weapons)
                {
                    if (!weapon.CanPurchase)
                        continue;
                    if (weapon.PurchaseFrom != menu.portraitPerson?.Name || (weapon.PurchaseFrom == "HatMouse" && hatMouse))
                        continue;
                    if (weapon.PurchaseRequirements != null && weapon.PurchaseRequirements.Count > 0 &&
                        precondMeth.Invoke<int>(new object[] { weapon.GetPurchaseRequirementString() }) == -1)
                        continue;
                    Item item = new StardewValley.Tools.MeleeWeapon(weapon.id);
                    forSale.Add(item);
                    itemPriceAndStock.Add(item, new int[] { weapon.PurchasePrice, int.MaxValue });
                    Log.trace($"\tAdding {weapon.Name}");
                }
                if(count != forSale.Count)
                {
                    Game1.activeClickableMenu = new ShopMenu(itemPriceAndStock, this.Helper.Reflection.GetField<int>(menu, "currency").GetValue(), this.Helper.Reflection.GetField<string>(menu, "personName").GetValue());
                }
            }

            ( ( Api )this.api ).InvokeAddedItemsToShop();
        }

        private bool didInit = false;
        private void initStuff( bool loadIdFiles )
        {
            if (this.didInit)
                return;
            this.didInit = true;

            // load object ID mappings from save folder
            if (loadIdFiles)
            {
                IDictionary<TKey, TValue> LoadDictionary<TKey, TValue>(string filename)
                {
                    string path = Path.Combine(Constants.CurrentSavePath, "JsonAssets", filename);
                    return File.Exists(path)
                        ? JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(File.ReadAllText(path))
                        : new Dictionary<TKey, TValue>();
                }
                Directory.CreateDirectory(Path.Combine(Constants.CurrentSavePath, "JsonAssets"));
                this.oldObjectIds = LoadDictionary<string, int>("ids-objects.json");
                this.oldCropIds = LoadDictionary<string, int>("ids-crops.json");
                this.oldFruitTreeIds = LoadDictionary<string, int>("ids-fruittrees.json");
                this.oldBigCraftableIds = LoadDictionary<string, int>("ids-big-craftables.json");
                this.oldHatIds = LoadDictionary<string, int>("ids-hats.json");
                this.oldWeaponIds = LoadDictionary<string, int>("ids-weapons.json");

                Log.trace("OLD IDS START");
                foreach (KeyValuePair<string, int> id in this.oldObjectIds)
                    Log.trace("\tObject " + id.Key + " = " + id.Value);
                foreach (KeyValuePair<string, int> id in this.oldCropIds)
                    Log.trace("\tCrop " + id.Key + " = " + id.Value);
                foreach (KeyValuePair<string, int> id in this.oldFruitTreeIds)
                    Log.trace("\tFruit Tree " + id.Key + " = " + id.Value);
                foreach (KeyValuePair<string, int> id in this.oldBigCraftableIds)
                    Log.trace("\tBigCraftable " + id.Key + " = " + id.Value);
                foreach (KeyValuePair<string, int> id in this.oldHatIds)
                    Log.trace("\tHat " + id.Key + " = " + id.Value);
                foreach (KeyValuePair<string, int> id in this.oldWeaponIds)
                    Log.trace("\tWeapon " + id.Key + " = " + id.Value);
                Log.trace("OLD IDS END");
            }

            // assign IDs
            this.objectIds = this.AssignIds("objects", StartingObjectId, this.objects.ToList<DataNeedsId>());
            this.cropIds = this.AssignIds("crops", StartingCropId, this.crops.ToList<DataNeedsId>());
            this.fruitTreeIds = this.AssignIds("fruittrees", StartingFruitTreeId, this.fruitTrees.ToList<DataNeedsId>());
            this.bigCraftableIds = this.AssignIds("big-craftables", StartingBigCraftableId, this.bigCraftables.ToList<DataNeedsId>());
            this.hatIds = this.AssignIds("hats", StartingHatId, this.hats.ToList<DataNeedsId>());
            this.weaponIds = this.AssignIds("weapons", StartingWeaponId, this.weapons.ToList<DataNeedsId>());

            this.api.InvokeIdsAssigned();

            // init
            this.Helper.Content.AssetEditors.Add(new ContentInjector());
        }

        /// <summary>Raised after the game finishes writing data to the save file (except the initial save creation).</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void onSaved(object sender, SavedEventArgs e)
        {
            if (!Directory.Exists(Path.Combine(Constants.CurrentSavePath, "JsonAssets")))
                Directory.CreateDirectory(Path.Combine(Constants.CurrentSavePath, "JsonAssets"));

            File.WriteAllText(Path.Combine(Constants.CurrentSavePath, "JsonAssets", "ids-objects.json"), JsonConvert.SerializeObject(this.objectIds));
            File.WriteAllText(Path.Combine(Constants.CurrentSavePath, "JsonAssets", "ids-crops.json"), JsonConvert.SerializeObject(this.cropIds));
            File.WriteAllText(Path.Combine(Constants.CurrentSavePath, "JsonAssets", "ids-fruittrees.json"), JsonConvert.SerializeObject(this.fruitTreeIds));
            File.WriteAllText(Path.Combine(Constants.CurrentSavePath, "JsonAssets", "ids-big-craftables.json"), JsonConvert.SerializeObject(this.bigCraftableIds));
            File.WriteAllText(Path.Combine(Constants.CurrentSavePath, "JsonAssets", "ids-hats.json"), JsonConvert.SerializeObject(this.hatIds));
            File.WriteAllText(Path.Combine(Constants.CurrentSavePath, "JsonAssets", "ids-weapons.json"), JsonConvert.SerializeObject(this.weaponIds));
        }

        internal IList<ObjectData> myRings = new List<ObjectData>();

        /// <summary>Raised after items are added or removed to a player's inventory. NOTE: this event is currently only raised for the current player.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void onInventoryChanged(object sender, InventoryChangedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;

            IList<int> ringIds = new List<int>();
            foreach (ObjectData ring in this.myRings)
                ringIds.Add(ring.id);

            for (int i = 0; i < Game1.player.Items.Count; ++i)
            {
                Item item = Game1.player.Items[i];
                if (item is StardewValley.Object obj && ringIds.Contains(obj.ParentSheetIndex))
                {
                    Log.trace($"Turning a ring-object of {obj.ParentSheetIndex} into a proper ring");
                    Game1.player.Items[i] = new StardewValley.Objects.Ring(obj.ParentSheetIndex);
                }
            }
        }

        private const int StartingObjectId = 2000;
        private const int StartingCropId = 100;
        private const int StartingFruitTreeId = 10;
        private const int StartingBigCraftableId = 300;
        private const int StartingHatId = 50;
        private const int StartingWeaponId = 64;

        internal IList<ObjectData> objects = new List<ObjectData>();
        internal IList<CropData> crops = new List<CropData>();
        internal IList<FruitTreeData> fruitTrees = new List<FruitTreeData>();
        internal IList<BigCraftableData> bigCraftables = new List<BigCraftableData>();
        internal IList<HatData> hats = new List<HatData>();
        internal IList<WeaponData> weapons = new List<WeaponData>();

        internal IDictionary<string, int> objectIds;
        internal IDictionary<string, int> cropIds;
        internal IDictionary<string, int> fruitTreeIds;
        internal IDictionary<string, int> bigCraftableIds;
        internal IDictionary<string, int> hatIds;
        internal IDictionary<string, int> weaponIds;

        internal IDictionary<string, int> oldObjectIds;
        internal IDictionary<string, int> oldCropIds;
        internal IDictionary<string, int> oldFruitTreeIds;
        internal IDictionary<string, int> oldBigCraftableIds;
        internal IDictionary<string, int> oldHatIds;
        internal IDictionary<string, int> oldWeaponIds;

        internal IDictionary<int, string> origObjects;
        internal IDictionary<int, string> origCrops;
        internal IDictionary<int, string> origFruitTrees;
        internal IDictionary<int, string> origBigCraftables;
        internal IDictionary<int, string> origHats;
        internal IDictionary<int, string> origWeapons;

        public int ResolveObjectId(object data)
        {
            if (data.GetType() == typeof(long))
                return (int)(long)data;
            else
            {
                if (this.objectIds.ContainsKey((string)data))
                    return this.objectIds[(string)data];

                foreach (KeyValuePair<int, string> obj in Game1.objectInformation )
                {
                    if (obj.Value.Split('/')[0] == (string)data)
                        return obj.Key;
                }

                Log.warn($"No idea what '{data}' is!");
                return 0;
            }
        }

        private Dictionary<string, int> AssignIds(string type, int starting, IList<DataNeedsId> data)
        {
            Dictionary<string, int> ids = new Dictionary<string, int>();

            int currId = starting;
            foreach (DataNeedsId d in data)
            {
                if (d.id == -1)
                {
                    Log.trace($"New ID: {d.Name} = {currId}");
                    ids.Add(d.Name, currId++);
                    if (type == "objects" && ((ObjectData)d).IsColored)
                        ++currId;
                    d.id = ids[d.Name];
                }
            }

            return ids;
        }

        private void clearIds(out IDictionary<string, int> ids, List<DataNeedsId> objs)
        {
            ids = null;
            foreach ( DataNeedsId obj in objs )
            {
                obj.id = -1;
            }
        }

        private IDictionary<int, string> cloneIdDictAndRemoveOurs( IDictionary<int, string> full, IDictionary<string, int> ours )
        {
            Dictionary<int, string> ret = new Dictionary<int, string>(full);
            foreach (KeyValuePair<string, int> obj in ours)
                ret.Remove(obj.Value);
            return ret;
        }

        private void fixIdsEverywhere()
        {
            this.origObjects = this.cloneIdDictAndRemoveOurs(Game1.objectInformation, this.objectIds);
            this.origCrops = this.cloneIdDictAndRemoveOurs(Game1.content.Load<Dictionary<int, string>>("Data\\Crops"), this.cropIds);
            this.origFruitTrees = this.cloneIdDictAndRemoveOurs(Game1.content.Load<Dictionary<int, string>>("Data\\fruitTrees"), this.fruitTreeIds);
            this.origBigCraftables = this.cloneIdDictAndRemoveOurs(Game1.bigCraftablesInformation, this.bigCraftableIds);
            this.origHats = this.cloneIdDictAndRemoveOurs(Game1.content.Load<Dictionary<int, string>>("Data\\hats"), this.hatIds);
            this.origWeapons = this.cloneIdDictAndRemoveOurs(Game1.content.Load<Dictionary<int, string>>("Data\\weapons"), this.weaponIds);

            this.fixItemList(Game1.player.Items);
            foreach (GameLocation loc in Game1.locations )
                this.fixLocation(loc);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage( "SMAPI.CommonErrors", "AvoidNetField") ]
        private void fixLocation( GameLocation loc )
        {
            if (loc is FarmHouse fh)
            {
#pragma warning disable AvoidImplicitNetFieldCast
                if (fh.fridge.Value?.items != null)
#pragma warning restore AvoidImplicitNetFieldCast
                    this.fixItemList(fh.fridge.Value.items);
            }

            IList<Vector2> toRemove = new List<Vector2>();
            foreach (Vector2 tfk in loc.terrainFeatures.Keys )
            {
                TerrainFeature tf = loc.terrainFeatures[tfk];
                if ( tf is HoeDirt hd )
                {
                    if (hd.crop == null)
                        continue;

                    if (this.fixId(this.oldCropIds, this.cropIds, hd.crop.rowInSpriteSheet, this.origCrops))
                        hd.crop = null;
                    else
                    {
                        string key = this.cropIds.FirstOrDefault(x => x.Value == hd.crop.rowInSpriteSheet.Value).Key;
                        CropData c = this.crops.FirstOrDefault(x => x.Name == key);
                        if ( c != null ) // Non-JA crop
                            hd.crop.indexOfHarvest.Value = this.ResolveObjectId(c.Product);
                    }
                }
                else if ( tf is FruitTree ft )
                {
                    if (this.fixId(this.oldFruitTreeIds, this.fruitTreeIds, ft.treeType, this.origFruitTrees))
                        toRemove.Add(tfk);
                    else
                    {
                        string key = this.oldFruitTreeIds.FirstOrDefault(x => x.Value == ft.treeType.Value).Key;
                        FruitTreeData ftt = this.fruitTrees.FirstOrDefault(x => x.Name == key);
                        if ( ftt != null ) // Non-JA fruit tree
                            ft.indexOfFruit.Value = this.ResolveObjectId(ftt.Product);
                    }
                }
            }
            foreach (Vector2 rem in toRemove)
                loc.terrainFeatures.Remove(rem);

            toRemove.Clear();
            foreach (Vector2 objk in loc.netObjects.Keys )
            {
                StardewValley.Object obj = loc.netObjects[objk];
                if ( obj is Chest chest )
                {
                    this.fixItemList(chest.items);
                }
                else
                {
                    if (!obj.bigCraftable.Value)
                    {
                        if (this.fixId(this.oldObjectIds, this.objectIds, obj.parentSheetIndex, this.origObjects))
                            toRemove.Add(objk);
                    }
                    else
                    {
                        if (this.fixId(this.oldBigCraftableIds, this.bigCraftableIds, obj.parentSheetIndex, this.origBigCraftables))
                            toRemove.Add(objk);
                    }
                }
                
                if ( obj.heldObject.Value != null )
                {
                    if (this.fixId(this.oldObjectIds, this.objectIds, obj.heldObject.Value.parentSheetIndex, this.origObjects))
                        obj.heldObject.Value = null;

                    if ( obj.heldObject.Value is Chest chest2 )
                    {
                        this.fixItemList(chest2.items);
                    }
                }
            }
            foreach (Vector2 rem in toRemove)
                loc.objects.Remove(rem);

            toRemove.Clear();
            foreach (Vector2 objk in loc.overlayObjects.Keys)
            {
                StardewValley.Object obj = loc.overlayObjects[objk];
                if (obj is Chest chest)
                {
                    this.fixItemList(chest.items);
                }
                else
                {
                    if (!obj.bigCraftable.Value)
                    {
                        if (this.fixId(this.oldObjectIds, this.objectIds, obj.parentSheetIndex, this.origObjects))
                            toRemove.Add(objk);
                    }
                    else
                    {
                        if (this.fixId(this.oldBigCraftableIds, this.bigCraftableIds, obj.parentSheetIndex, this.origBigCraftables))
                            toRemove.Add(objk);
                    }
                }

                if (obj.heldObject.Value != null)
                {
                    if (this.fixId(this.oldObjectIds, this.objectIds, obj.heldObject.Value.parentSheetIndex, this.origObjects))
                        obj.heldObject.Value = null;

                    if (obj.heldObject.Value is Chest chest2)
                    {
                        this.fixItemList(chest2.items);
                    }
                }
            }
            foreach (Vector2 rem in toRemove)
                loc.overlayObjects.Remove(rem);

            if (loc is BuildableGameLocation buildLoc)
                foreach (Building building in buildLoc.buildings)
                {
                    if (building.indoors.Value != null)
                        this.fixLocation(building.indoors.Value);
                    if ( building is Mill mill )
                    {
                        this.fixItemList(mill.input.Value.items);
                        this.fixItemList(mill.output.Value.items);
                    }
                }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("SMAPI.CommonErrors", "AvoidNetField")]
        private void fixItemList( IList< Item > items )
        {
            for ( int i = 0; i < items.Count; ++i )
            {
                Item item = items[i];
                if ( item is StardewValley.Object obj )
                {
                    if (!obj.bigCraftable.Value)
                    {
                        if (this.fixId(this.oldObjectIds, this.objectIds, obj.parentSheetIndex, this.origObjects))
                            items[i] = null;
                    }
                    else
                    {
                        if (this.fixId(this.oldBigCraftableIds, this.bigCraftableIds, obj.parentSheetIndex, this.origBigCraftables))
                            items[i] = null;
                    }
                }
                else if ( item is Hat hat )
                {
                    if (this.fixId(this.oldHatIds, this.hatIds, hat.which, this.origHats))
                        items[i] = null;
                }
                else if ( item is MeleeWeapon weapon )
                {
                    if (this.fixId(this.oldWeaponIds, this.weaponIds, weapon.initialParentTileIndex, this.origWeapons))
                        items[i] = null;
                    else if (this.fixId(this.oldWeaponIds, this.weaponIds, weapon.currentParentTileIndex, this.origWeapons))
                        items[i] = null;
                    else if (this.fixId(this.oldWeaponIds, this.weaponIds, weapon.currentParentTileIndex, this.origWeapons))
                        items[i] = null;
                }
                else if ( item is Ring ring )
                {
                    if (this.fixId(this.oldObjectIds, this.objectIds, ring.indexInTileSheet, this.origObjects))
                        items[i] = null;
                }
            }
        }

        // Return true if the item should be deleted, false otherwise.
        // Only remove something if old has it but not new
        private bool fixId(IDictionary<string, int> oldIds, IDictionary<string, int> newIds, NetInt id, IDictionary<int, string> origData )
        {
            if (origData.ContainsKey(id.Value))
                return false;

            if (oldIds.Values.Contains(id.Value))
            {
                int id_ = id.Value;
                string key = oldIds.FirstOrDefault(x => x.Value == id_).Key;

                if (newIds.ContainsKey(key))
                {
                    id.Value = newIds[key];
                    return false;
                }
                else return true;
            }
            else return false;
        }
    }
}
