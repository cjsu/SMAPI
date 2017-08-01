using System;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Utilities
{
    /// <summary>Represents a Stardew Valley date.</summary>
    public class SDate : IEquatable<SDate>
    {
        /*********
        ** Properties
        *********/
        /// <summary>The internal season names in order.</summary>
        private readonly string[] Seasons = { "spring", "summer", "fall", "winter" };

        /// <summary>The number of seasons in a year.</summary>
        private int SeasonsInYear => this.Seasons.Length;

        /// <summary>The number of days in a season.</summary>
        private readonly int DaysInSeason = 28;

        /// <summary>The Day of the Week this date has</summary>
        public DayOfWeek Weekday;

        /*********
        ** Accessors
        *********/
        /// <summary>The day of month.</summary>
        public int Day { get; }

        /// <summary>The season name.</summary>
        public string Season { get; }

        /// <summary>The year.</summary>
        public int Year { get; }

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="day">The day of month.</param>
        /// <param name="season">The season name.</param>
        /// <exception cref="ArgumentException">One of the arguments has an invalid value (like day 35).</exception>
        public SDate(int day, string season)
            : this(day, season, Game1.year) { }

        /// <summary>Construct an instance.</summary>
        /// <param name="day">The day of month.</param>
        /// <param name="season">The season name.</param>
        /// <param name="year">The year.</param>
        /// <exception cref="ArgumentException">One of the arguments has an invalid value (like day 35).</exception>
        public SDate(int day, string season, int year)
        {
            // validate
            if (season == null)
                throw new ArgumentNullException(nameof(season));
            if (!this.Seasons.Contains(season))
                throw new ArgumentException($"Unknown season '{season}', must be one of [{string.Join(", ", this.Seasons)}].");
            if (day < 1 || day > this.DaysInSeason)
                throw new ArgumentException($"Invalid day '{day}', must be a value from 1 to {this.DaysInSeason}.");
            if (year < 1)
                throw new ArgumentException($"Invalid year '{year}', must be at least 1.");

            // initialise
            this.Day = day;
            this.Season = season;
            this.Year = year;

            this.Weekday = GetDayOfWeek();
        }

        /// <summary>Get the current in-game date.</summary>
        public static SDate Now()
        {
            return new SDate(Game1.dayOfMonth, Game1.currentSeason, Game1.year);
        }

        /// <summary>Get a new date with the given number of days added.</summary>
        /// <param name="offset">The number of days to add.</param>
        /// <returns>Returns the resulting date.</returns>
        /// <exception cref="ArithmeticException">The offset would result in an invalid date (like year 0).</exception>
        public SDate AddDays(int offset)
        {
            // simple case
            int day = this.Day + offset;
            string season = this.Season;
            int year = this.Year;

            // handle season transition
            if (day > this.DaysInSeason || day < 1)
            {
                // get season index
                int curSeasonIndex = this.GetSeasonIndex();

                // get season offset
                int seasonOffset = day / this.DaysInSeason;
                if (day < 1)
                    seasonOffset -= 1;

                // get new date
                day = this.GetWrappedIndex(day, this.DaysInSeason);
                season = this.Seasons[this.GetWrappedIndex(curSeasonIndex + seasonOffset, this.Seasons.Length)];
                year += seasonOffset / this.Seasons.Length;
            }

            // validate
            if (year < 1)
                throw new ArithmeticException($"Adding {offset} days to {this} would result in invalid date {day:00} {season} {year}.");

            // return new date
            return new SDate(day, season, year);
        }

        /// <summary>Get a string representation of the date. This is mainly intended for debugging or console messages.</summary>
        public override string ToString()
        {
            return $"{this.Day:00} {this.Season} Y{this.Year}";
        }

        /// <summary>
        /// This gets the day of the week from the date
        /// </summary>
        /// <returns>A constant describing the day</returns>
        private DayOfWeek GetDayOfWeek()
        {
            switch (this.Day % 7)
            {
                case 0:
                    return DayOfWeek.Sunday;
                case 1:
                    return DayOfWeek.Monday;
                case 2:
                    return DayOfWeek.Tuesday;
                case 3:
                    return DayOfWeek.Wednesday;
                case 4:
                    return DayOfWeek.Thursday;
                case 5:
                    return DayOfWeek.Friday;
                case 6:
                    return DayOfWeek.Saturday;
                default:
                    return 0;
            }
        }

        /****
        ** IEquatable
        ****/
        /// <summary>Get whether this instance is equal to another.</summary>
        /// <param name="other">The other value to compare.</param>
        public bool Equals(SDate other)
        {
            return this == other;
        }

        /// <summary>Get whether this instance is equal to another.</summary>
        /// <param name="obj">The other value to compare.</param>
        public override bool Equals(object obj)
        {
            return obj is SDate other && this == other;
        }

        /// <summary>Get a hash code which uniquely identifies a date.</summary>
        public override int GetHashCode()
        {
            // return the number of days since 01 spring Y1
            int yearIndex = this.Year - 1;
            return
                yearIndex * this.DaysInSeason * this.SeasonsInYear
                + this.GetSeasonIndex() * this.DaysInSeason
                + this.Day;
        }

        /****
        ** Operators
        ****/
        /// <summary>Get whether one date is equal to another.</summary>
        /// <param name="date">The base date to compare.</param>
        /// <param name="other">The other date to compare.</param>
        /// <returns>The equality of the dates</returns>
        public static bool operator ==(SDate date, SDate other)
        {
            return date?.GetHashCode() == other?.GetHashCode();
        }

        /// <summary>Get whether one date is not equal to another.</summary>
        /// <param name="date">The base date to compare.</param>
        /// <param name="other">The other date to compare.</param>
        public static bool operator !=(SDate date, SDate other)
        {
            return date?.GetHashCode() != other?.GetHashCode();
        }

        /// <summary>Get whether one date is more than another.</summary>
        /// <param name="date">The base date to compare.</param>
        /// <param name="other">The other date to compare.</param>
        public static bool operator >(SDate date, SDate other)
        {
            return date?.GetHashCode() > other?.GetHashCode();
        }

        /// <summary>Get whether one date is more than or equal to another.</summary>
        /// <param name="date">The base date to compare.</param>
        /// <param name="other">The other date to compare.</param>
        public static bool operator >=(SDate date, SDate other)
        {
            return date?.GetHashCode() >= other?.GetHashCode();
        }

        /// <summary>Get whether one date is less than or equal to another.</summary>
        /// <param name="date">The base date to compare.</param>
        /// <param name="other">The other date to compare.</param>
        public static bool operator <=(SDate date, SDate other)
        {
            return date?.GetHashCode() <= other?.GetHashCode();
        }

        /// <summary>Get whether one date is less than another.</summary>
        /// <param name="date">The base date to compare.</param>
        /// <param name="other">The other date to compare.</param>
        public static bool operator <(SDate date, SDate other)
        {
            return date?.GetHashCode() < other?.GetHashCode();
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the current season index.</summary>
        /// <exception cref="InvalidOperationException">The current season wasn't recognised.</exception>
        private int GetSeasonIndex()
        {
            int index = Array.IndexOf(this.Seasons, this.Season);
            if (index == -1)
                throw new InvalidOperationException($"The current season '{this.Season}' wasn't recognised.");
            return index;
        }

        /// <summary>Get the real index in an array which should be treated as a two-way loop.</summary>
        /// <param name="index">The index in the looped array.</param>
        /// <param name="length">The number of elements in the array.</param>
        private int GetWrappedIndex(int index, int length)
        {
            int wrapped = index % length;
            if (wrapped < 0)
                wrapped += length;
            return wrapped;
        }
    }
}
