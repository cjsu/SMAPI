using System;
using System.Collections.Generic;

namespace JsonAssets
{
    public interface IApi
    {
        void LoadAssets(string path);

        int GetObjectId(string name);
        int GetCropId(string name);
        int GetFruitTreeId(string name);
        int GetBigCraftableId(string name);
        int GetHatId(string name);
        int GetWeaponId(string name);

        IDictionary<string, int> GetAllObjectIds();
        IDictionary<string, int> GetAllCropIds();
        IDictionary<string, int> GetAllFruitTreeIds();
        IDictionary<string, int> GetAllBigCraftableIds();
        IDictionary<string, int> GetAllHatIds();
        IDictionary<string, int> GetAllWeaponIds();

        event EventHandler IdsAssigned;
        event EventHandler AddedItemsToShop;
    }

    public class Api : IApi
    {
        private readonly Action<string> loadFolder;

        public Api(Action<string> loadFolder)
        {
            this.loadFolder = loadFolder;
        }

        public void LoadAssets(string path)
        {
            this.loadFolder(path);
        }

        public int GetObjectId(string name)
        {
            return Mod.instance.objectIds.ContainsKey(name) ? Mod.instance.objectIds[name] : -1;
        }

        public int GetCropId(string name)
        {
            return Mod.instance.cropIds.ContainsKey(name) ? Mod.instance.cropIds[name] : -1;
        }

        public int GetFruitTreeId(string name)
        {
            return Mod.instance.fruitTreeIds.ContainsKey(name) ? Mod.instance.fruitTreeIds[name] : -1;
        }

        public int GetBigCraftableId(string name)
        {
            return Mod.instance.bigCraftableIds.ContainsKey(name) ? Mod.instance.bigCraftableIds[name] : -1;
        }

        public int GetHatId(string name)
        {
            return Mod.instance.hatIds.ContainsKey(name) ? Mod.instance.hatIds[name] : -1;
        }

        public int GetWeaponId(string name)
        {
            return Mod.instance.weaponIds.ContainsKey(name) ? Mod.instance.weaponIds[name] : -1;
        }

        public IDictionary<string, int> GetAllObjectIds()
        {
            return new Dictionary<string, int>(Mod.instance.objectIds);
        }

        public IDictionary<string, int> GetAllCropIds()
        {
            return new Dictionary<string, int>(Mod.instance.cropIds);
        }

        public IDictionary<string, int> GetAllFruitTreeIds()
        {
            return new Dictionary<string, int>(Mod.instance.fruitTreeIds);
        }

        public IDictionary<string, int> GetAllBigCraftableIds()
        {
            return new Dictionary<string, int>(Mod.instance.bigCraftableIds);
        }

        public IDictionary<string, int> GetAllHatIds()
        {
            return new Dictionary<string, int>(Mod.instance.hatIds);
        }

        public IDictionary<string, int> GetAllWeaponIds()
        {
            return new Dictionary<string, int>(Mod.instance.weaponIds);
        }

        public event EventHandler IdsAssigned;
        internal void InvokeIdsAssigned()
        {
            Log.trace("Event: IdsAssigned");
            if (IdsAssigned == null)
                return;
            Util.invokeEvent("JsonAssets.Api.IdsAssigned", IdsAssigned.GetInvocationList(), null);
        }

        public event EventHandler AddedItemsToShop;
        internal void InvokeAddedItemsToShop()
        {
            Log.trace("Event: AddedItemsToShop");
            if (AddedItemsToShop == null)
                return;
            Util.invokeEvent("JsonAssets.Api.AddedItemsToShop", AddedItemsToShop.GetInvocationList(), null);
        }
    }
}
