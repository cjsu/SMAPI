using System;
using System.Linq;
using StardewValley;

namespace StardewModdingAPI.Utilities
{
    /// <summary>Represents a Stardew Valley date.</summary>
    public class SDate
    {
        /*********
        ** Properties
        *********/
        /// <summary>The internal season names in order.</summary>
        private readonly string[] Seasons = { "spring", "summer", "fall", "winter" };

        /// <summary>The number of days in a season.</summary>
        private readonly int DaysInSeason = 28;


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
                // get current season index
                int curSeasonIndex = Array.IndexOf(this.Seasons, this.Season);
                if (curSeasonIndex == -1)
                    throw new InvalidOperationException($"The current season '{this.Season}' wasn't recognised.");

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
            if(year < 1)
                throw new ArithmeticException($"Adding {offset} days to {this} would result in invalid date {day:00} {season} {year}.");

            // return new date
            return new SDate(day, season, year);
        }

        /// <summary>Get a string representation of the date. This is mainly intended for debugging or console messages.</summary>
        public override string ToString()
        {
            return $"{this.Day:00} {this.Season} Y{this.Year}";
        }

        /// <summary>Get the current in-game date.</summary>
        public static SDate Now()
        {
            return new SDate(Game1.dayOfMonth, Game1.currentSeason, Game1.year);
        }

        /*********
        ** Operator methods
        *********/

        /// <summary>
        /// Equality operator. Tests the date being equal to each other
        /// </summary>
        /// <param name="s1">The first date being compared</param>
        /// <param name="s2">The second date being compared</param>
        /// <returns>The equality of the dates</returns>
        public static bool operator ==(SDate s1, SDate s2)
        {
            if (s1.Day == s2.Day && s1.Year == s2.Year && s1.Season == s2.Season)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Inequality operator. Tests the date being not equal to each other
        /// </summary>
        /// <param name="s1">The first date being compared</param>
        /// <param name="s2">The second date being compared</param>
        /// <returns>The inequality of the dates</returns>
        public static bool operator !=(SDate s1, SDate s2)
        {
            if (s1.Day == s2.Day && s1.Year == s2.Year && s1.Season == s2.Season)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Less than operator. Tests the date being less than to each other
        /// </summary>
        /// <param name="s1">The first date being compared</param>
        /// <param name="s2">The second date being compared</param>
        /// <returns>If the dates are less than</returns>
        public static bool operator >(SDate s1, SDate s2)
        {
            if (s1.Year > s2.Year)
                return true;
            else if (s1.Year == s2.Year)
            {
                if (s1.Season == "winter" && s2.Season != "winter")
                    return true;
                else if (s1.Season == s2.Season && s1.Day > s2.Day)
                    return true;
                if (s1.Season == "fall" && (s2.Season == "summer" || s2.Season == "spring"))
                    return true;
                if (s1.Season == "summer" && s2.Season == "spring")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Less or equal than operator. Tests the date being less than or equal to each other
        /// </summary>
        /// <param name="s1">The first date being compared</param>
        /// <param name="s2">The second date being compared</param>
        /// <returns>If the dates are less than or equal than</returns>
        public static bool operator >=(SDate s1, SDate s2)
        {
            if (s1.Year > s2.Year)
                return true;
            else if (s1.Year == s2.Year)
            {
                if (s1.Season == "winter" && s2.Season != "winter")
                    return true;
                else if (s1.Season == s2.Season && s1.Day >= s2.Day)
                    return true;
                if (s1.Season == "fall" && (s2.Season == "summer" || s2.Season == "spring"))
                    return true;
                if (s1.Season == "summer" && s2.Season == "spring")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Greater or equal than operator. Tests the date being greater than or equal to each other
        /// </summary>
        /// <param name="s1">The first date being compared</param>
        /// <param name="s2">The second date being compared</param>
        /// <returns>If the dates are greater than or equal than</returns>
        public static bool operator <=(SDate s1, SDate s2)
        {
            if (s1.Year < s2.Year)
                return true;
            else if (s1.Year == s2.Year)
            {
                if (s1.Season == s2.Season && s1.Day <= s2.Day)
                    return true;
                else if (s1.Season == "spring" && s2.Season != "spring")
                    return true;
                if (s1.Season == "summer" && (s2.Season == "fall" || s2.Season == "winter"))
                    return true;
                if (s1.Season == "fall" && s2.Season == "winter")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Greater than operator. Tests the date being greater than to each other
        /// </summary>
        /// <param name="s1">The first date being compared</param>
        /// <param name="s2">The second date being compared</param>
        /// <returns>If the dates are greater than</returns>
        public static bool operator <(SDate s1, SDate s2)
        {
            if (s1.Year < s2.Year)
                return true;
            else if (s1.Year == s2.Year)
            {
                if (s1.Season == s2.Season && s1.Day < s2.Day)
                    return true;
                else if (s1.Season == "spring" && s2.Season != "spring")
                    return true;
                if (s1.Season == "summer" && (s2.Season == "fall" || s2.Season == "winter"))
                    return true;
                if (s1.Season == "fall" && s2.Season == "winter")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Overrides the equals function.
        /// </summary>
        /// <param name="obj">Object being compared.</param>
        /// <returns>The equalaity of the object.</returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// This returns the hashcode of the object
        /// </summary>
        /// <returns>The hashcode of the object.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /*********
        ** Private methods
        *********/
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
