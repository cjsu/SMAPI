using StardewModdingAPI;
using StardewValley;

namespace TrainerMod.Framework.Commands.Player
{
    /// <summary>A command which edits the player's current level for a skill.</summary>
    internal class SetLevelCommand : TrainerCommand
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        public SetLevelCommand()
            : base("player_setlevel", "Sets the player's specified skill to the specified value.\n\nUsage: player_setlevel <skill> <value>\n- skill: the skill to set (one of 'luck', 'mining', 'combat', 'farming', 'fishing', or 'foraging').\n- value: the target level (a number from 1 to 10).") { }

        /// <summary>Handle the command.</summary>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        /// <param name="command">The command name.</param>
        /// <param name="args">The command arguments.</param>
        public override void Handle(IMonitor monitor, string command, ArgumentParser args)
        {
            // validate
            if (!args.TryGet(0, "skill", out string skill, oneOf: new[] { "luck", "mining", "combat", "farming", "fishing", "foraging" }))
                return;
            if (!args.TryGetInt(1, "level", out int level, min: 0, max: 10))
                return;

            // handle
            switch (skill)
            {
                case "luck":
                    Game1.player.LuckLevel = level;
                    monitor.Log($"OK, your luck skill is now {Game1.player.LuckLevel}.", LogLevel.Info);
                    break;

                case "mining":
                    Game1.player.MiningLevel = level;
                    monitor.Log($"OK, your mining skill is now {Game1.player.MiningLevel}.", LogLevel.Info);
                    break;

                case "combat":
                    Game1.player.CombatLevel = level;
                    monitor.Log($"OK, your combat skill is now {Game1.player.CombatLevel}.", LogLevel.Info);
                    break;

                case "farming":
                    Game1.player.FarmingLevel = level;
                    monitor.Log($"OK, your farming skill is now {Game1.player.FarmingLevel}.", LogLevel.Info);
                    break;

                case "fishing":
                    Game1.player.FishingLevel = level;
                    monitor.Log($"OK, your fishing skill is now {Game1.player.FishingLevel}.", LogLevel.Info);
                    break;

                case "foraging":
                    Game1.player.ForagingLevel = level;
                    monitor.Log($"OK, your foraging skill is now {Game1.player.ForagingLevel}.", LogLevel.Info);
                    break;
            }
        }
    }
}
