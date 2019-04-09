using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using StardewValley;
using StardewValley.Locations;

namespace TimeSpeed.Framework
{
    /// <summary>The mod configuration model.</summary>
    internal class ModConfig
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The default number of seconds per 10-game-minutes, or <c>null</c> to freeze time globally. The game uses 7 seconds by default.</summary>
        public double? DefaultTickLength { get; set; } = 7.0;

        /// <summary>The number of seconds per 10-game-minutes (or <c>null</c> to freeze time) for each location. The key can be a location name, 'Mine', or <see cref="LocationType"/>.</summary>
        /// <remarks>Most location names can be found at "\Stardew Valley\Content\Maps" directory. They usually match the file name without its extension. 'Mine' is a special case which includes all mine maps.</remarks>
        /// <example>
        /// This will set the Mines and Skull Cavern to 28 seconds per 10-game-minutes, freeze time indoors and use <see cref="DefaultTickLength"/> for outdoors:
        /// <code>
        /// "TickLengthByLocation": {
        ///     "Mine": 28,
        ///     "Indoors": null
        /// }
        /// </code>
        /// 
        /// This will freeze time on your farm and set it to 14 seconds per 10-game-minutes elsewhere.
        /// <code>
        /// "TickLengthByLocation": {
        ///     "Indoors": 14,
        ///     "Outdoors": 14,
        ///     "Farm":null
        /// }
        /// </code>
        /// 
        /// This will freeze time in the Saloon. All other locations will default to <see cref="DefaultTickLength"/>.
        /// <code>
        /// "TickLengthByLocation": {
        ///     "Saloon": null
        /// }
        /// </code>
        /// </example>
        public Dictionary<string, double?> TickLengthByLocation { get; set; } = new Dictionary<string, double?>
        {
            { LocationType.Indoors.ToString(), 14 },
            { LocationType.Outdoors.ToString(), 7 },
            { LocationType.Mine.ToString(), 7 }
        };

        /// <summary>Whether to change tick length on festival days.</summary>
        public bool EnableOnFestivalDays { get; set; } = false;

        /// <summary>The time at which to freeze time everywhere (or <c>null</c> to disable this). This should be 24-hour military time (e.g. 800 for 8am, 1600 for 8pm, etc).</summary>
        public int? FreezeTimeAt { get; set; } = null;

        /// <summary>Whether to show a message about the time settings when you enter a location.</summary>
        public bool LocationNotify { get; set; } = false;

        /// <summary>The keyboard bindings used to control the flow of time. See available keys at <a href="https://msdn.microsoft.com/en-us/library/microsoft.xna.framework.input.keys.aspx" />. Set a key to null to disable it.</summary>
        public ModControlsConfig Keys { get; set; } = new ModControlsConfig();


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether time should be frozen at a given location.</summary>
        /// <param name="location">The game location.</param>
        public bool ShouldFreeze(GameLocation location)
        {
            return this.GetTickLengthOrFreeze(location) == null;
        }

        /// <summary>Get whether the time should be frozen at a given time of day.</summary>
        /// <param name="time">The time of day in 24-hour military format (e.g. 1600 for 8pm).</param>
        public bool ShouldFreeze(int time)
        {
            return this.FreezeTimeAt == time;
        }

        /// <summary>Get whether time settings should be applied on a given day.</summary>
        /// <param name="season">The season to check.</param>
        /// <param name="dayOfMonth">The day of month to check.</param>
        public bool ShouldScale(string season, int dayOfMonth)
        {
            return this.EnableOnFestivalDays || !Utility.isFestivalDay(dayOfMonth, season);
        }

        /// <summary>Get the tick interval to apply for a location.</summary>
        /// <param name="location">The game location.</param>
        public int? GetTickInterval(GameLocation location)
        {
            return (int?)((this.GetTickLengthOrFreeze(location) ?? this.DefaultTickLength) * 1000);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>The method called after the config file is deserialised.</summary>
        /// <param name="context">The deserialisation context.</param>
        [OnDeserialized]
        private void OnDeserializedMethod(StreamingContext context)
        {
            this.TickLengthByLocation = new Dictionary<string, double?>(this.TickLengthByLocation, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Get the tick length to apply for a given location, or <c>null</c> to freeze time.</summary>
        /// <param name="location">The game location.</param>
        private double? GetTickLengthOrFreeze(GameLocation location)
        {
            // check by location name
            if (this.TickLengthByLocation.TryGetValue(location.Name, out double? tickLength))
                return tickLength;
            if (location is MineShaft && this.TickLengthByLocation.TryGetValue(LocationType.Mine.ToString(), out tickLength))
                return tickLength;

            // check by location type
            if (this.TickLengthByLocation.TryGetValue((location.IsOutdoors ? LocationType.Outdoors : LocationType.Indoors).ToString(), out tickLength))
                return tickLength;

            // default
            return this.DefaultTickLength;
        }
    }
}
