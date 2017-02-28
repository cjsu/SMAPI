using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using TrainerMod.Framework;
using TrainerMod.ItemData;
using Object = StardewValley.Object;

namespace TrainerMod
{
    /// <summary>The main entry point for the mod.</summary>
    public class TrainerMod : Mod
    {
        /*********
        ** Properties
        *********/
        /// <summary>The time of day at which to freeze time.</summary>
        private int FrozenTime;

        /// <summary>Whether to keep the player's health at its maximum.</summary>
        private bool InfiniteHealth;

        /// <summary>Whether to keep the player's stamina at its maximum.</summary>
        private bool InfiniteStamina;

        /// <summary>Whether to keep the player's money at a set value.</summary>
        private bool InfiniteMoney;

        /// <summary>Whether to freeze time.</summary>
        private bool FreezeTime;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.RegisterCommands(helper);
            GameEvents.UpdateTick += this.ReceiveUpdateTick;
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Implementation
        ****/
        /// <summary>The method invoked when the game updates its state.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ReceiveUpdateTick(object sender, EventArgs e)
        {
            if (Game1.player == null)
                return;

            if (this.InfiniteHealth)
                Game1.player.health = Game1.player.maxHealth;
            if (this.InfiniteStamina)
                Game1.player.stamina = Game1.player.MaxStamina;
            if (this.InfiniteMoney)
                Game1.player.money = 999999;
            if (this.FreezeTime)
                Game1.timeOfDay = this.FrozenTime;
        }

        /****
        ** Command definitions
        ****/
        /// <summary>Register all trainer commands.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        private void RegisterCommands(IModHelper helper)
        {
            helper.ConsoleCommands
                .Add("types", "Lists all value types.", this.HandleCommand)
                .Add("save", "Saves the game? Doesn't seem to work.", this.HandleCommand)
                .Add("load", "Shows the load screen.", this.HandleCommand)
                .Add("player_setname", "Sets the player's name.\n\nUsage: player_setname <target> <name>\n- target: what to rename (one of 'player' or 'farm').\n- name: the new name to set.", this.HandleCommand)
                .Add("player_setmoney", "Sets the player's money.\n\nUsage: player_setmoney <value>\n- value: an integer amount, or 'inf' for infinite money.", this.HandleCommand)
                .Add("player_setstamina", "Sets the player's stamina.\n\nUsage: player_setstamina [value]\n- value: an integer amount, or 'inf' for infinite stamina.", this.HandleCommand)
                .Add("player_setmaxstamina", "Sets the player's max stamina.\n\nUsage: player_setmaxstamina [value]\n- value: an integer amount.", this.HandleCommand)
                .Add("player_sethealth", "Sets the player's health.\n\nUsage: player_sethealth [value]\n- value: an integer amount, or 'inf' for infinite health.", this.HandleCommand)
                .Add("player_setmaxhealth", "Sets the player's max health.\n\nUsage: player_setmaxhealth [value]\n- value: an integer amount.", this.HandleCommand)
                .Add("player_setimmunity", "Sets the player's immunity.\n\nUsage: player_setimmunity [value]\n- value: an integer amount.", this.HandleCommand)

                .Add("player_setlevel", "Sets the player's specified skill to the specified value.\n\nUsage: player_setlevel <skill> <value>\n- skill: the skill to set (one of 'luck', 'mining', 'combat', 'farming', 'fishing', or 'foraging').\n- value: the target level (a number from 1 to 10).", this.HandleCommand)
                .Add("player_setspeed", "Sets the player's speed to the specified value?\n\nUsage: player_setspeed <value>\n- value: an integer amount (0 is normal).", this.HandleCommand)
                .Add("player_changecolour", "Sets the colour of a player feature.\n\nUsage: player_changecolor <target> <colour>\n- target: what to change (one of 'hair', 'eyes', or 'pants').\n- colour: a colour value in RGB format, like (255,255,255).", this.HandleCommand)
                .Add("player_changestyle", "Sets the style of a player feature.\n\nUsage: player_changecolor <target> <value>.\n- target: what to change (one of 'hair', 'shirt', 'skin', 'acc', 'shoe', 'swim', or 'gender').\n- value: the integer style ID.", this.HandleCommand)

                .Add("player_additem", $"Gives the player an item.\n\nUsage: player_additem <item> [count] [quality]\n- item: the item ID (use the 'list_items' command to see a list).\n- count (optional): how many of the item to give.\n- quality (optional): one of {Object.lowQuality} (normal), {Object.medQuality} (silver), {Object.highQuality} (gold), or {Object.bestQuality} (iridium).", this.HandleCommand)
                .Add("player_addmelee", "Gives the player a melee weapon.\n\nUsage: player_addmelee <item>\n- item: the melee weapon ID (use the 'list_items' command to see a list).", this.HandleCommand)
                .Add("player_addring", "Gives the player a ring.\n\nUsage: player_addring <item>\n- item: the ring ID (use the 'list_items' command to see a list).", this.HandleCommand)

                .Add("list_items", "Lists and searches items in the game data.\n\nUsage: list_items [search]\n- search (optional): an arbitrary search string to filter by.", this.HandleCommand)

                .Add("world_settime", "Sets the time to the specified value.\n\nUsage: world_settime <value>\n- value: the target time in military time (like 0600 for 6am and 1800 for 6pm)", this.HandleCommand)
                .Add("world_freezetime", "Freezes or resumes time.\n\nUsage: world_freezetime [value]\n- value: one of 0 (resume), 1 (freeze) or blank (toggle).", this.HandleCommand)
                .Add("world_setday", "Sets the day to the specified value.\n\nUsage: world_setday <value>.\n- value: the target day (a number from 1 to 28).", this.HandleCommand)
                .Add("world_setseason", "Sets the season to the specified value.\n\nUsage: world_setseason <season>\n- value: the target season (one of 'spring', 'summer', 'fall', 'winter').", this.HandleCommand)
                .Add("world_downminelevel", "Goes down one mine level?", this.HandleCommand)
                .Add("world_setminelevel", "Sets the mine level?\n\nUsage: world_setminelevel <value>\n- value: The target level (a number between 1 and 120).", this.HandleCommand)

                .Add("show_game_files", "Opens the game folder.", this.HandleCommand)
                .Add("show_data_files", "Opens the folder containing the save and log files.", this.HandleCommand);
        }

        /// <summary>Handle a TrainerMod command.</summary>
        /// <param name="name">The command name.</param>
        /// <param name="args">The command arguments.</param>
        private void HandleCommand(string name, string[] args)
        {
            switch (name)
            {
                case "type":
                    this.Monitor.Log($"[Int32: {int.MinValue} - {int.MaxValue}], [Int64: {long.MinValue} - {long.MaxValue}], [String: \"raw text\"], [Colour: r,g,b (EG: 128, 32, 255)]", LogLevel.Info);
                    break;

                case "save":
                    SaveGame.Save();
                    break;

                case "load":
                    Game1.hasLoadedGame = false;
                    Game1.activeClickableMenu = new LoadGameMenu();
                    break;

                case "player_setname":
                    if (args.Length > 1)
                    {
                        string target = args[0];
                        string[] validTargets = { "player", "farm" };
                        if (validTargets.Contains(target))
                        {
                            switch (target)
                            {
                                case "player":
                                    Game1.player.Name = args[1];
                                    break;
                                case "farm":
                                    Game1.player.farmName = args[1];
                                    break;
                            }
                        }
                        else
                            this.LogObjectInvalid();
                    }
                    else
                        this.LogObjectValueNotSpecified();
                    break;

                case "player_setmoney":
                    if (args.Any())
                    {
                        string amountStr = args[0];
                        if (amountStr == "inf")
                            this.InfiniteMoney = true;
                        else
                        {
                            this.InfiniteMoney = false;
                            int amount;
                            if (int.TryParse(amountStr, out amount))
                            {
                                Game1.player.Money = amount;
                                this.Monitor.Log($"Set {Game1.player.Name}'s money to {Game1.player.Money}", LogLevel.Info);
                            }
                            else
                                this.LogValueNotInt32();
                        }
                    }
                    else
                        this.LogValueNotSpecified();
                    break;

                case "player_setstamina":
                    if (args.Any())
                    {
                        string amountStr = args[0];
                        if (amountStr == "inf")
                            this.InfiniteStamina = true;
                        else
                        {
                            this.InfiniteStamina = false;
                            int amount;
                            if (int.TryParse(amountStr, out amount))
                            {
                                Game1.player.Stamina = amount;
                                this.Monitor.Log($"Set {Game1.player.Name}'s stamina to {Game1.player.Stamina}", LogLevel.Info);
                            }
                            else
                                this.LogValueNotInt32();
                        }
                    }
                    else
                        this.Monitor.Log($"{Game1.player.Name}'s stamina is {Game1.player.Stamina}", LogLevel.Info);
                    break;

                case "player_setmaxstamina":
                    if (args.Any())
                    {
                        int amount;
                        if (int.TryParse(args[0], out amount))
                        {
                            Game1.player.MaxStamina = amount;
                            this.Monitor.Log($"Set {Game1.player.Name}'s max stamina to {Game1.player.MaxStamina}", LogLevel.Info);
                        }
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        this.Monitor.Log($"{Game1.player.Name}'s maxstamina is {Game1.player.MaxStamina}", LogLevel.Info);
                    break;

                case "player_setlevel":
                    if (args.Length > 1)
                    {
                        string skill = args[0];
                        string[] skills = { "luck", "mining", "combat", "farming", "fishing", "foraging" };
                        if (skills.Contains(skill))
                        {
                            int level;
                            if (int.TryParse(args[1], out level))
                            {
                                switch (skill)
                                {
                                    case "luck":
                                        Game1.player.LuckLevel = level;
                                        break;
                                    case "mining":
                                        Game1.player.MiningLevel = level;
                                        break;
                                    case "combat":
                                        Game1.player.CombatLevel = level;
                                        break;
                                    case "farming":
                                        Game1.player.FarmingLevel = level;
                                        break;
                                    case "fishing":
                                        Game1.player.FishingLevel = level;
                                        break;
                                    case "foraging":
                                        Game1.player.ForagingLevel = level;
                                        break;
                                }
                            }
                            else
                                this.LogValueNotInt32();
                        }
                        else
                            this.Monitor.Log("<skill> is invalid", LogLevel.Error);
                    }
                    else
                        this.Monitor.Log("<skill> and <value> must be specified", LogLevel.Error);
                    break;

                case "player_setspeed":
                    if (args.Any())
                    {
                        string amountStr = args[0];
                        if (amountStr.IsInt())
                        {
                            Game1.player.addedSpeed = amountStr.ToInt();
                            this.Monitor.Log($"Set {Game1.player.Name}'s added speed to {Game1.player.addedSpeed}", LogLevel.Info);
                        }
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        this.LogValueNotSpecified();
                    break;

                case "player_changecolour":
                    if (args.Length > 1)
                    {
                        string target = args[0];
                        string[] validTargets = { "hair", "eyes", "pants" };
                        if (validTargets.Contains(target))
                        {
                            string[] colorHexes = args[1].Split(new[] { ',' }, 3);
                            if (colorHexes[0].IsInt() && colorHexes[1].IsInt() && colorHexes[2].IsInt())
                            {
                                var color = new Color(colorHexes[0].ToInt(), colorHexes[1].ToInt(), colorHexes[2].ToInt());
                                switch (target)
                                {
                                    case "hair":
                                        Game1.player.hairstyleColor = color;
                                        break;
                                    case "eyes":
                                        Game1.player.changeEyeColor(color);
                                        break;
                                    case "pants":
                                        Game1.player.pantsColor = color;
                                        break;
                                }
                            }
                            else
                                this.Monitor.Log("<colour> is invalid", LogLevel.Error);
                        }
                        else
                            this.LogObjectInvalid();
                    }
                    else
                        this.Monitor.Log("<object> and <colour> must be specified", LogLevel.Error);
                    break;

                case "player_changestyle":
                    if (args.Length > 1)
                    {
                        string target = args[0];
                        string[] validTargets = { "hair", "shirt", "skin", "acc", "shoe", "swim", "gender" };
                        if (validTargets.Contains(target))
                        {
                            if (args[1].IsInt())
                            {
                                var styleID = args[1].ToInt();
                                switch (target)
                                {
                                    case "hair":
                                        Game1.player.changeHairStyle(styleID);
                                        break;
                                    case "shirt":
                                        Game1.player.changeShirt(styleID);
                                        break;
                                    case "acc":
                                        Game1.player.changeAccessory(styleID);
                                        break;
                                    case "skin":
                                        Game1.player.changeSkinColor(styleID);
                                        break;
                                    case "shoe":
                                        Game1.player.changeShoeColor(styleID);
                                        break;
                                    case "swim":
                                        if (styleID == 0)
                                            Game1.player.changeOutOfSwimSuit();
                                        else if (styleID == 1)
                                            Game1.player.changeIntoSwimsuit();
                                        else
                                            this.Monitor.Log("<value> must be 0 or 1 for this <object>", LogLevel.Error);
                                        break;
                                    case "gender":
                                        if (styleID == 0)
                                            Game1.player.changeGender(true);
                                        else if (styleID == 1)
                                            Game1.player.changeGender(false);
                                        else
                                            this.Monitor.Log("<value> must be 0 or 1 for this <object>", LogLevel.Error);
                                        break;
                                }
                            }
                            else
                                this.LogValueInvalid();
                        }
                        else
                            this.LogObjectInvalid();
                    }
                    else
                        this.LogObjectValueNotSpecified();
                    break;

                case "world_freezetime":
                    if (args.Any())
                    {
                        string valueStr = args[0];
                        if (valueStr.IsInt())
                        {
                            int value = valueStr.ToInt();
                            if (value == 0 || value == 1)
                            {
                                this.FreezeTime = value == 1;
                                this.FrozenTime = this.FreezeTime ? Game1.timeOfDay : 0;
                                this.Monitor.Log("Time is now " + (this.FreezeTime ? "frozen" : "thawed"), LogLevel.Info);
                            }
                            else
                                this.Monitor.Log("<value> should be 0, 1, or empty", LogLevel.Error);
                        }
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        int valu = 1;
                        if (this.FreezeTime == false)
                        {
                            valu = 1;
                        }
                        else
                        {
                            valu = 0;
                        }
                        this.FreezeTime = valu == 1;
                        this.FrozenTime = this.FreezeTime ? Game1.timeOfDay : 0;
                        this.Monitor.Log("Time is now " + (this.FreezeTime ? "frozen" : "thawed"), LogLevel.Info);
                    break;

                case "world_settime":
                    if (args.Any())
                    {
                        string timeStr = args[0];
                        if (timeStr.IsInt())
                        {
                            int time = timeStr.ToInt();

                            if (time <= 2600 && time >= 600)
                            {
                                Game1.timeOfDay = args[0].ToInt();
                                this.FrozenTime = this.FreezeTime ? Game1.timeOfDay : 0;
                                this.Monitor.Log($"Time set to: {Game1.timeOfDay}", LogLevel.Info);
                            }
                            else
                                this.Monitor.Log("<value> should be between 600 and 2600 (06:00 AM - 02:00 AM [NEXT DAY])", LogLevel.Error);
                        }
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        this.LogValueNotSpecified();
                    break;

                case "world_setday":
                    if (args.Any())
                    {
                        string dayStr = args[0];

                        if (dayStr.IsInt())
                        {
                            int day = dayStr.ToInt();
                            if (day <= 28 && day > 0)
                                Game1.dayOfMonth = day;
                            else
                                this.Monitor.Log("<value> must be between 1 and 28", LogLevel.Error);
                        }
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        this.LogValueNotSpecified();
                    break;

                case "world_setseason":
                    if (args.Any())
                    {
                        string season = args[0];
                        string[] validSeasons = { "winter", "spring", "summer", "fall" };
                        if (validSeasons.Contains(season))
                            Game1.currentSeason = season;
                        else
                            this.LogValueInvalid();
                    }
                    else
                        this.LogValueNotSpecified();
                    break;

                case "player_sethealth":
                    if (args.Any())
                    {
                        string amountStr = args[0];

                        if (amountStr == "inf")
                            this.InfiniteHealth = true;
                        else
                        {
                            this.InfiniteHealth = false;
                            if (amountStr.IsInt())
                                Game1.player.health = amountStr.ToInt();
                            else
                                this.LogValueNotInt32();
                        }
                    }
                    else
                        this.Monitor.Log($"Health is: {Game1.player.health}", LogLevel.Info);
                    break;

                case "player_setmaxhealth":
                    if (args.Any())
                    {
                        string amountStr = args[0];
                        if (amountStr.IsInt())
                            Game1.player.maxHealth = amountStr.ToInt();
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        this.Monitor.Log($"MaxHealth is: {Game1.player.maxHealth}", LogLevel.Info);
                    break;

                case "player_setimmunity":
                    if (args.Any())
                    {
                        string amountStr = args[0];
                        if (amountStr.IsInt())
                            Game1.player.immunity = amountStr.ToInt();
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        this.Monitor.Log($"Immunity is: {Game1.player.immunity}", LogLevel.Info);
                    break;

                case "player_additem":
                    if (args.Any())
                    {
                        string itemIdStr = args[0];
                        if (itemIdStr.IsInt())
                        {
                            int itemID = itemIdStr.ToInt();
                            int count = 1;
                            int quality = 0;
                            if (args.Length > 1)
                            {
                                if (args[1].IsInt())
                                    count = args[1].ToInt();
                                else
                                {
                                    this.Monitor.Log("[count] is invalid", LogLevel.Error);
                                    return;
                                }

                                if (args.Length > 2)
                                {
                                    if (args[2].IsInt())
                                        quality = args[2].ToInt();
                                    else
                                    {
                                        this.Monitor.Log("[quality] is invalid", LogLevel.Error);
                                        return;
                                    }
                                }
                            }

                            var item = new Object(itemID, count) { quality = quality };

                            Game1.player.addItemByMenuIfNecessary(item);
                        }
                        else
                            this.Monitor.Log("<item> is invalid", LogLevel.Error);
                    }
                    else
                        this.LogObjectValueNotSpecified();
                    break;

                case "player_addmelee":
                    if (args.Any())
                    {
                        if (args[0].IsInt())
                        {
                            MeleeWeapon weapon = new MeleeWeapon(args[0].ToInt());
                            Game1.player.addItemByMenuIfNecessary(weapon);
                            this.Monitor.Log($"Gave {weapon.Name} to {Game1.player.Name}", LogLevel.Info);
                        }
                        else
                            this.Monitor.Log("<item> is invalid", LogLevel.Error);
                    }
                    else
                        this.LogObjectValueNotSpecified();
                    break;

                case "player_addring":
                    if (args.Any())
                    {
                        if (args[0].IsInt())
                        {
                            Ring ring = new Ring(args[0].ToInt());
                            Game1.player.addItemByMenuIfNecessary(ring);
                            this.Monitor.Log($"Gave {ring.Name} to {Game1.player.Name}", LogLevel.Info);
                        }
                        else
                            this.Monitor.Log("<item> is invalid", LogLevel.Error);
                    }
                    else
                        this.LogObjectValueNotSpecified();
                    break;

                case "list_items":
                    {
                        var matches = this.GetItems(args).ToArray();

                        // show matches
                        string summary = "Searching...\n";
                        if (matches.Any())
                            this.Monitor.Log(summary + this.GetTableString(matches, new[] { "type", "id", "name" }, val => new[] { val.Type.ToString(), val.ID.ToString(), val.Name }), LogLevel.Info);
                        else
                            this.Monitor.Log(summary + "No items found", LogLevel.Info);
                    }
                    break;

                case "world_downminelevel":
                    Game1.nextMineLevel();
                    break;

                case "world_setminelevel":
                    if (args.Any())
                    {
                        if (args[0].IsInt())
                            Game1.enterMine(true, args[0].ToInt(), "");
                        else
                            this.LogValueNotInt32();
                    }
                    else
                        this.LogValueNotSpecified();
                    break;

                case "show_game_files":
                    Process.Start(Constants.ExecutionPath);
                    break;

                case "show_data_files":
                    Process.Start(Constants.DataPath);
                    break;

                default:
                    throw new NotImplementedException($"TrainerMod received unknown command '{name}'.");
            }
        }

        /****
        ** Helpers
        ****/
        /// <summary>Get all items which can be searched and added to the player's inventory through the console.</summary>
        /// <param name="searchWords">The search string to find.</param>
        private IEnumerable<ISearchItem> GetItems(string[] searchWords)
        {
            // normalise search term
            searchWords = searchWords?.Where(word => !string.IsNullOrWhiteSpace(word)).ToArray();
            if (searchWords?.Any() == false)
                searchWords = null;

            // find matches
            return (
                from item in this.GetItems()
                let term = $"{item.ID}|{item.Type}|{item.Name}"
                where searchWords == null || searchWords.All(word => term.IndexOf(word, StringComparison.CurrentCultureIgnoreCase) != -1)
                select item
            );
        }

        /// <summary>Get all items which can be searched and added to the player's inventory through the console.</summary>
        private IEnumerable<ISearchItem> GetItems()
        {
            // objects
            foreach (int id in Game1.objectInformation.Keys)
            {
                ISearchItem obj = id >= Ring.ringLowerIndexRange && id <= Ring.ringUpperIndexRange
                    ? new SearchableRing(id)
                    : (ISearchItem)new SearchableObject(id);
                if (obj.IsValid)
                    yield return obj;
            }

            // weapons
            foreach (int id in Game1.content.Load<Dictionary<int, string>>("Data\\weapons").Keys)
            {
                ISearchItem weapon = new SearchableWeapon(id);
                if (weapon.IsValid)
                    yield return weapon;
            }
        }

        /// <summary>Get an ASCII table for a set of tabular data.</summary>
        /// <typeparam name="T">The data type.</typeparam>
        /// <param name="data">The data to display.</param>
        /// <param name="header">The table header.</param>
        /// <param name="getRow">Returns a set of fields for a data value.</param>
        private string GetTableString<T>(IEnumerable<T> data, string[] header, Func<T, string[]> getRow)
        {
            // get table data
            int[] widths = header.Select(p => p.Length).ToArray();
            string[][] rows = data
                .Select(item =>
                {
                    string[] fields = getRow(item);
                    if (fields.Length != widths.Length)
                        throw new InvalidOperationException($"Expected {widths.Length} columns, but found {fields.Length}: {string.Join(", ", fields)}");

                    for (int i = 0; i < fields.Length; i++)
                        widths[i] = Math.Max(widths[i], fields[i].Length);

                    return fields;
                })
                .ToArray();

            // render fields
            List<string[]> lines = new List<string[]>(rows.Length + 2)
            {
                header,
                header.Select((value, i) => "".PadRight(widths[i], '-')).ToArray()
            };
            lines.AddRange(rows);

            return string.Join(
                Environment.NewLine,
                lines.Select(line => string.Join(" | ",
                    line.Select((field, i) => field.PadRight(widths[i], ' ')).ToArray())
                )
            );
        }

        /****
        ** Logging
        ****/
        /// <summary>Log an error indicating a value must be specified.</summary>
        public void LogValueNotSpecified()
        {
            this.Monitor.Log("<value> must be specified", LogLevel.Error);
        }

        /// <summary>Log an error indicating a target and value must be specified.</summary>
        public void LogObjectValueNotSpecified()
        {
            this.Monitor.Log("<object> and <value> must be specified", LogLevel.Error);
        }

        /// <summary>Log an error indicating a value is invalid.</summary>
        public void LogValueInvalid()
        {
            this.Monitor.Log("<value> is invalid", LogLevel.Error);
        }

        /// <summary>Log an error indicating a target is invalid.</summary>
        public void LogObjectInvalid()
        {
            this.Monitor.Log("<object> is invalid", LogLevel.Error);
        }

        /// <summary>Log an error indicating a value must be an integer.</summary>
        public void LogValueNotInt32()
        {
            this.Monitor.Log("<value> must be a whole number (Int32)", LogLevel.Error);
        }
    }
}
