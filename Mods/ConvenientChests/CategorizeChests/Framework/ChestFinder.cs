using System.Linq;
using ConvenientChests.CategorizeChests.Framework.Persistence;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace ConvenientChests.CategorizeChests.Framework
{
    class ChestFinder : IChestFinder
    {
        public Chest GetChestByAddress(ChestAddress address)
        {
            if (address.LocationType == ChestLocationType.Refrigerator)
            {
                var house = (FarmHouse) Game1.locations.SingleOrDefault(l => l is FarmHouse f && address.LocationName == (f.uniqueName?.Value ?? f.Name));

                if (house == null)
                    throw new InvalidSaveDataException($"Save data contains refrigerator data in {address.LocationName} but location does not exist");

                if (house.upgradeLevel < 1)
                    throw new InvalidSaveDataException($"Save data contains refrigerator data in {address.LocationName} but refrigerator does not exist");

                return house.fridge.Value;
            }

            var location = GetLocationFromAddress(address);
            if (location.objects.ContainsKey(address.Tile) && location.objects[address.Tile] is Chest chest)
                return chest;

            throw new InvalidSaveDataException($"Can't find chest in {location.Name} at {address.Tile}");
        }

        private GameLocation GetLocationFromAddress(ChestAddress address)
        {
            var location = Game1.locations.FirstOrDefault(l => l.Name == address.LocationName);

            if (location == null)
                throw new InvalidSaveDataException($"Can't find location named {address.LocationName}");

            if (address.LocationType != ChestLocationType.Building)
                return location;
            
            if (!(location is BuildableGameLocation buildableLocation))
                throw new InvalidSaveDataException($"Can't find any buildings in location named {location.Name}");
                
            var building = buildableLocation.buildings.SingleOrDefault(b => b.nameOfIndoors == address.BuildingName);
            if (building == null)
                throw new InvalidSaveDataException($"Save data contains building data in {address.BuildingName} but building does not exist");

            return building.indoors.Value;
        }
    }
}