using System;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using TrainerMod.Framework.ItemData;
using Object = StardewValley.Object;

namespace TrainerMod.Framework.Commands.Player
{
    /// <summary>A command which adds an item to the player inventory.</summary>
    internal class AddCommand : TrainerCommand
    {
        /*********
        ** Properties
        *********/
        /// <summary>Provides methods for searching and constructing items.</summary>
        private readonly ItemRepository Items = new ItemRepository();


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public AddCommand()
            : base("player_add", AddCommand.GetDescription())
        { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // read arguments
            if (!args.TryGet(0, "item type", out string rawType, oneOf: Enum.GetNames(typeof(ItemType))))
                return;
            if (!args.TryGetInt(1, "item ID", out int id, min: 0))
                return;
            if (!args.TryGetInt(2, "count", out int count, min: 1, required: false))
                count = 1;
            if (!args.TryGetInt(3, "quality", out int quality, min: Object.lowQuality, max: Object.bestQuality, required: false))
                quality = Object.lowQuality;
            ItemType type = (ItemType)Enum.Parse(typeof(ItemType), rawType, ignoreCase: true);

            // find matching item
            SearchableItem match = this.Items.GetAll().FirstOrDefault(p => p.Type == type && p.ID == id);
            if (match == null)
            {
                monitor.Log($"There's no {type} item with ID {id}.", LogLevel.Error);
                return;
            }

            // apply count & quality
            match.Item.Stack = count;
            if (match.Item is Object obj)
                obj.quality = quality;

            // add to inventory
            Game1.player.addItemByMenuIfNecessary(match.Item);
            monitor.Log($"OK, added {match.Name} ({match.Type} #{match.ID}) to your inventory.", LogLevel.Info);
        }

        /*********
        ** Private methods
        *********/
        private static string GetDescription()
        {
            string[] typeValues = Enum.GetNames(typeof(ItemType));
            return "Gives the player an item.\n"
                + "\n"
                + "Usage: player_add <type> <item> [count] [quality]\n"
                + $"- type: the item type (one of {string.Join(", ", typeValues)}).\n"
                + "- item: the item ID (use the 'list_items' command to see a list).\n"
                + "- count (optional): how many of the item to give.\n"
                + $"- quality (optional): one of {Object.lowQuality} (normal), {Object.medQuality} (silver), {Object.highQuality} (gold), or {Object.bestQuality} (iridium).\n"
                + "\n"
                + "This example adds the galaxy sword to your inventory:\n"
                + "  player_add weapon 4";
        }
    }
}
