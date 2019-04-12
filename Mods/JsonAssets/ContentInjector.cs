using JsonAssets.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;

namespace JsonAssets
{
    public class ContentInjector : IAssetEditor
    {
        public bool CanEdit<T>(IAssetInfo asset)
        {
            if (asset.AssetNameEquals("Data\\ObjectInformation"))
                return true;
            if (asset.AssetNameEquals("Data\\Crops"))
                return true;
            if (asset.AssetNameEquals("Data\\fruitTrees"))
                return true;
            if (asset.AssetNameEquals("Data\\CookingRecipes"))
                return true;
            if (asset.AssetNameEquals("Data\\CraftingRecipes"))
                return true;
            if (asset.AssetNameEquals("Data\\BigCraftablesInformation"))
                return true;
            if (asset.AssetNameEquals("Data\\hats"))
                return true;
            if (asset.AssetNameEquals("Data\\weapons"))
                return true;
            if (asset.AssetNameEquals("Data\\NPCGiftTastes"))
                return true;
            if (asset.AssetNameEquals("Maps\\springobjects"))
                return true;
            if (asset.AssetNameEquals("TileSheets\\crops"))
                return true;
            if (asset.AssetNameEquals("TileSheets\\fruitTrees"))
                return true;
            if (asset.AssetNameEquals("TileSheets\\Craftables") || asset.AssetNameEquals("TileSheets\\Craftables_indoor") || asset.AssetNameEquals("TileSheets\\Craftables_outdoor"))
                return true; // _indoor/_outdoor for Seasonal Immersion compat
            if (asset.AssetNameEquals("Characters\\Farmer\\hats"))
                return true;
            if (asset.AssetNameEquals("TileSheets\\weapons"))
                return true;
            return false;
        }

        public void Edit<T>(IAssetData asset)
        {
            if (asset.AssetNameEquals("Data\\ObjectInformation"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                foreach (ObjectData obj in Mod.instance.objects)
                {
                    try
                    {
                        Log.trace($"Injecting to objects: {obj.GetObjectId()}: {obj.GetObjectInformation()}");
                        data.Add(obj.GetObjectId(), obj.GetObjectInformation());
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting object information for {obj.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\Crops"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                foreach (CropData crop in Mod.instance.crops)
                {
                    try
                    {
                        Log.trace($"Injecting to crops: {crop.GetSeedId()}: {crop.GetCropInformation()}");
                        data.Add(crop.GetSeedId(), crop.GetCropInformation());
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting crop for {crop.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\fruitTrees"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                foreach (FruitTreeData fruitTree in Mod.instance.fruitTrees)
                {
                    try
                    {
                        Log.trace($"Injecting to fruit trees: {fruitTree.GetSaplingId()}: {fruitTree.GetFruitTreeInformation()}");
                        data.Add(fruitTree.GetSaplingId(), fruitTree.GetFruitTreeInformation());
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting fruit tree for {fruitTree.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\CookingRecipes"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                foreach (ObjectData obj in Mod.instance.objects)
                {
                    try
                    {
                        if (obj.Recipe == null)
                            continue;
                        if (obj.Category != ObjectData.Category_.Cooking)
                            continue;
                        Log.trace($"Injecting to cooking recipes: {obj.Name}: {obj.Recipe.GetRecipeString(obj)}");
                        data.Add(obj.Name, obj.Recipe.GetRecipeString(obj));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting cooking recipe for {obj.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\CraftingRecipes"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                foreach (ObjectData obj in Mod.instance.objects)
                {
                    try
                    {
                        if (obj.Recipe == null)
                            continue;
                        if (obj.Category == ObjectData.Category_.Cooking)
                            continue;
                        Log.trace($"Injecting to crafting recipes: {obj.Name}: {obj.Recipe.GetRecipeString(obj)}");
                        data.Add(obj.Name, obj.Recipe.GetRecipeString(obj));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting crafting recipe for {obj.Name}: {e}");
                    }
                }
                foreach (BigCraftableData big in Mod.instance.bigCraftables)
                {
                    try
                    {
                        if (big.Recipe == null)
                            continue;
                        Log.trace($"Injecting to crafting recipes: {big.Name}: {big.Recipe.GetRecipeString(big)}");
                        data.Add(big.Name, big.Recipe.GetRecipeString(big));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting crafting recipe for {big.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\BigCraftablesInformation"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                foreach (BigCraftableData big in Mod.instance.bigCraftables)
                {
                    try
                    {
                        Log.trace($"Injecting to big craftables: {big.GetCraftableId()}: {big.GetCraftableInformation()}");
                        data.Add(big.GetCraftableId(), big.GetCraftableInformation());
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting object information for {big.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\hats"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                foreach (HatData hat in Mod.instance.hats)
                {
                    try
                    {
                        Log.trace($"Injecting to hats: {hat.GetHatId()}: {hat.GetHatInformation()}");
                        data.Add(hat.GetHatId(), hat.GetHatInformation());
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting hat information for {hat.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\weapons"))
            {
                IDictionary<int, string> data = asset.AsDictionary<int, string>().Data;
                foreach (WeaponData weapon in Mod.instance.weapons)
                {
                    try
                    {
                        Log.trace($"Injecting to weapons: {weapon.GetWeaponId()}: {weapon.GetWeaponInformation()}");
                        data.Add(weapon.GetWeaponId(), weapon.GetWeaponInformation());
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting weapon information for {weapon.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Data\\NPCGiftTastes"))
            {
                IDictionary<string, string> data = asset.AsDictionary<string, string>().Data;
                // TODO: This could be optimized from mn to... m + n?
                // Basically, iterate through objects and create Dictionary<NPC name, GiftData[]>
                // Iterate through objects, each section and add to dict[npc][approp. section]
                // Point is, I'm doing this the lazy way right now
                Dictionary<string, string> newData = new Dictionary<string, string>(data);
                foreach (KeyValuePair<string, string> npc in data)
                {
                    if (npc.Key.StartsWith("Universal_"))
                        continue;

                    string[] sections = npc.Value.Split('/');
                    if ( sections.Length != 11 )
                    {
                        Log.warn($"Bad gift taste data for {npc.Key}!");
                        continue;
                    }

                    string loveStr = sections[0];
                    List<string> loveIds = new List<string>(sections[1].Split(' '));
                    string likeStr = sections[2];
                    List<string> likeIds = new List<string>(sections[3].Split(' '));
                    string dislikeStr = sections[4];
                    List<string> dislikeIds = new List<string>(sections[5].Split(' '));
                    string hateStr = sections[6];
                    List<string> hateIds = new List<string>(sections[7].Split(' '));
                    string neutralStr = sections[8];
                    List<string> neutralIds = new List<string>(sections[9].Split(' '));

                    foreach (ObjectData obj in Mod.instance.objects )
                    {
                        if (obj.GiftTastes == null)
                            continue;
                        if (obj.GiftTastes.Love != null && obj.GiftTastes.Love.Contains(npc.Key))
                            loveIds.Add(obj.GetObjectId().ToString());
                        if (obj.GiftTastes.Like != null && obj.GiftTastes.Like.Contains(npc.Key))
                            likeIds.Add(obj.GetObjectId().ToString());
                        if (obj.GiftTastes.Neutral != null && obj.GiftTastes.Neutral.Contains(npc.Key))
                            neutralIds.Add(obj.GetObjectId().ToString());
                        if (obj.GiftTastes.Dislike != null && obj.GiftTastes.Dislike.Contains(npc.Key))
                            dislikeIds.Add(obj.GetObjectId().ToString());
                        if (obj.GiftTastes.Hate != null && obj.GiftTastes.Hate.Contains(npc.Key))
                            hateIds.Add(obj.GetObjectId().ToString());
                    }

                    string loveIdStr = string.Join(" ", loveIds);
                    string likeIdStr = string.Join(" ", likeIds);
                    string dislikeIdStr = string.Join(" ", dislikeIds);
                    string hateIdStr = string.Join(" ", hateIds);
                    string neutralIdStr = string.Join(" ", neutralIds);
                    newData[npc.Key] = $"{loveStr}/{loveIdStr}/{likeStr}/{likeIdStr}/{dislikeStr}/{dislikeIdStr}/{hateStr}/{hateIdStr}/{neutralStr}/{neutralIdStr}/ ";

                    Log.trace($"Adding gift tastes for {npc.Key}: {newData[npc.Key]}");
                }
                asset.ReplaceWith(newData);
            }
            else if (asset.AssetNameEquals("Maps\\springobjects"))
            {
                Texture2D oldTex = asset.AsImage().Data;
                Texture2D newTex = new Texture2D(Game1.graphics.GraphicsDevice, oldTex.Width, Math.Max(oldTex.Height, 4096));
                asset.ReplaceWith(newTex);
                asset.AsImage().PatchImage(oldTex);

                foreach (ObjectData obj in Mod.instance.objects)
                {
                    try
                    {
                        Log.trace($"Injecting {obj.Name} sprites @ {this.objectRect(obj.GetObjectId())}");
                        asset.AsImage().PatchImage(obj.texture, null, this.objectRect(obj.GetObjectId()));
                        if (obj.IsColored)
                        {
                            Log.trace($"Injecting {obj.Name} color sprites @ {this.objectRect(obj.GetObjectId() + 1)}");
                            asset.AsImage().PatchImage(obj.textureColor, null, this.objectRect(obj.GetObjectId() + 1));
                        }
                    }
                    catch ( Exception e )
                    {
                        Log.error($"Exception injecting sprite for {obj.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("TileSheets\\crops"))
            {
                Texture2D oldTex = asset.AsImage().Data;
                Texture2D newTex = new Texture2D(Game1.graphics.GraphicsDevice, oldTex.Width, Math.Max(oldTex.Height, 4096));
                asset.ReplaceWith(newTex);
                asset.AsImage().PatchImage(oldTex);

                foreach (CropData crop in Mod.instance.crops)
                {
                    try
                    {
                        Log.trace($"Injecting {crop.Name} crop images @ {this.cropRect(crop.GetCropSpriteIndex())}");
                        asset.AsImage().PatchImage(crop.texture, null, this.cropRect(crop.GetCropSpriteIndex()));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting crop sprite for {crop.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("TileSheets\\fruitTrees"))
            {
                Texture2D oldTex = asset.AsImage().Data;
                Texture2D newTex = new Texture2D(Game1.graphics.GraphicsDevice, oldTex.Width, Math.Max(oldTex.Height, 4096));
                asset.ReplaceWith(newTex);
                asset.AsImage().PatchImage(oldTex);

                foreach (FruitTreeData fruitTree in Mod.instance.fruitTrees)
                {
                    try
                    {
                        Log.trace($"Injecting {fruitTree.Name} fruit tree images @ {this.fruitTreeRect(fruitTree.GetFruitTreeIndex())}");
                        asset.AsImage().PatchImage(fruitTree.texture, null, this.fruitTreeRect(fruitTree.GetFruitTreeIndex()));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting fruit tree sprite for {fruitTree.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("TileSheets\\Craftables") || asset.AssetNameEquals("TileSheets\\Craftables_indoor") || asset.AssetNameEquals("TileSheets\\Craftables_outdoor"))
            {
                Texture2D oldTex = asset.AsImage().Data;
                Texture2D newTex = new Texture2D(Game1.graphics.GraphicsDevice, oldTex.Width, Math.Max(oldTex.Height, 4096));
                asset.ReplaceWith(newTex);
                asset.AsImage().PatchImage(oldTex);
                Log.trace($"Big craftables are now ({oldTex.Width}, {Math.Max(oldTex.Height, 4096)})");

                foreach (BigCraftableData big in Mod.instance.bigCraftables)
                {
                    try
                    {
                        Log.trace($"Injecting {big.Name} sprites @ {this.bigCraftableRect(big.GetCraftableId())}");
                        asset.AsImage().PatchImage(big.texture, null, this.bigCraftableRect(big.GetCraftableId()));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting sprite for {big.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("Characters\\Farmer\\hats"))
            {
                Texture2D oldTex = asset.AsImage().Data;
                Texture2D newTex = new Texture2D(Game1.graphics.GraphicsDevice, oldTex.Width, Math.Max(oldTex.Height, 4096));
                asset.ReplaceWith(newTex);
                asset.AsImage().PatchImage(oldTex);
                Log.trace($"Hats are now ({oldTex.Width}, {Math.Max(oldTex.Height, 4096)})");

                foreach (HatData hat in Mod.instance.hats)
                {
                    try
                    {
                        Log.trace($"Injecting {hat.Name} sprites @ {this.hatRect(hat.GetHatId())}");
                        asset.AsImage().PatchImage(hat.texture, null, this.hatRect(hat.GetHatId()));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting sprite for {hat.Name}: {e}");
                    }
                }
            }
            else if (asset.AssetNameEquals("TileSheets\\weapons"))
            {
                Texture2D oldTex = asset.AsImage().Data;
                Texture2D newTex = new Texture2D(Game1.graphics.GraphicsDevice, oldTex.Width, Math.Max(oldTex.Height, 4096));
                asset.ReplaceWith(newTex);
                asset.AsImage().PatchImage(oldTex);
                Log.trace($"Weapons are now ({oldTex.Width}, {Math.Max(oldTex.Height, 4096)})");

                foreach (WeaponData weapon in Mod.instance.weapons)
                {
                    try
                    {
                        Log.trace($"Injecting {weapon.Name} sprites @ {this.weaponRect(weapon.GetWeaponId())}");
                        asset.AsImage().PatchImage(weapon.texture, null, this.weaponRect(weapon.GetWeaponId()));
                    }
                    catch (Exception e)
                    {
                        Log.error($"Exception injecting sprite for {weapon.Name}: {e}");
                    }
                }
            }
        }
        private Rectangle objectRect(int index)
        {
            return new Rectangle(index % 24 * 16, index / 24 * 16, 16, 16);
        }
        private Rectangle cropRect(int index)
        {
            return new Rectangle(index % 2 * 128, index / 2 * 32, 128, 32);
        }
        private Rectangle fruitTreeRect(int index)
        {
            return new Rectangle(0, index * 80, 432, 80);
        }
        private Rectangle bigCraftableRect(int index)
        {
            return new Rectangle(index % 8 * 16, index / 8 * 32, 16, 32);
        }
        private Rectangle hatRect(int index)
        {
            return new Rectangle(index % 12 * 20, index / 12 * 80, 20, 80);
        }
        private Rectangle weaponRect(int index)
        {
            return new Rectangle(index % 8 * 16, index / 8 * 16, 16, 16);
        }
    }
}
