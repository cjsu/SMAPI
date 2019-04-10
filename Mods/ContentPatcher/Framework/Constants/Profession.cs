using StardewValley;

namespace ContentPatcher.Framework.Constants
{
    /// <summary>A player profession.</summary>
    internal enum Profession
    {
        /***
        ** Combat
        ***/
        /// <summary>The acrobat profession for the combat skill.</summary>
        Acrobat = Farmer.acrobat,

        /// <summary>The brute profession for the combat skill.</summary>
        Brute = Farmer.brute,

        /// <summary>The defender profession for the combat skill.</summary>
        Defender = Farmer.defender,

        /// <summary>The desperado profession for the combat skill.</summary>
        Desperado = Farmer.desperado,

        /// <summary>The fighter profession for the combat skill.</summary>
        Fighter = Farmer.fighter,

        /// <summary>The scout profession for the combat skill.</summary>
        Scout = Farmer.scout,

        /***
        ** Farming
        ***/
        /// <summary>The agriculturist profession for the farming skill.</summary>
        Agriculturist = Farmer.agriculturist,

        /// <summary>The shepherd profession for the farming skill.</summary>
        Artisan = Farmer.artisan,

        /// <summary>The coopmaster profession for the farming skill.</summary>
        Coopmaster = Farmer.butcher, // game's constant name doesn't match usage

        /// <summary>The rancher profession for the farming skill.</summary>
        Rancher = Farmer.rancher,

        /// <summary>The shepherd profession for the farming skill.</summary>
        Shepherd = Farmer.shepherd,

        /// <summary>The tiller profession for the farming skill.</summary>
        Tiller = Farmer.tiller,

        /***
        ** Fishing
        ***/
        /// <summary>The angler profession for the fishing skill.</summary>
        Angler = Farmer.angler,

        /// <summary>The fisher profession for the fishing skill.</summary>
        Fisher = Farmer.fisher,

        /// <summary>The mariner profession for the fishing skill.</summary>
        Mariner = Farmer.baitmaster, // game's constant name is confusing

        /// <summary>The pirate profession for the fishing skill.</summary>
        Pirate = Farmer.pirate,

        /// <summary>The luremaster profession for the fishing skill.</summary>
        Luremaster = Farmer.mariner, // game's constant name is confusing

        /// <summary>The trapper profession for the fishing skill.</summary>
        Trapper = Farmer.trapper,

        /***
        ** Foraging
        ***/
        /// <summary>The botanist profession for the foraging skill.</summary>
        Botanist = Farmer.botanist,

        /// <summary>The forester profession for the foraging skill.</summary>
        Forester = Farmer.forester,

        /// <summary>The gatherer profession for the foraging skill.</summary>
        Gatherer = Farmer.gatherer,

        /// <summary>The lumberjack profession for the foraging skill.</summary>
        Lumberjack = Farmer.lumberjack,

        /// <summary>The tapper profession for the foraging skill.</summary>
        Tapper = Farmer.tapper,

        /// <summary>The tracker profession for the foraging skill.</summary>
        Tracker = Farmer.tracker,

        /***
        ** Mining
        ***/
        /// <summary>The blacksmith profession for the foraging skill.</summary>
        Blacksmith = Farmer.blacksmith,

        /// <summary>The excavator profession for the foraging skill.</summary>
        Excavator = Farmer.excavator,

        /// <summary>The gemologist profession for the foraging skill.</summary>
        Gemologist = Farmer.gemologist,

        /// <summary>The geologist profession for the foraging skill.</summary>
        Geologist = Farmer.geologist,

        /// <summary>The miner profession for the foraging skill.</summary>
        Miner = Farmer.miner,

        /// <summary>The prospector profession for the foraging skill.</summary>
        Prospector = Farmer.burrower, // game's constant name is confusing
    }
}
