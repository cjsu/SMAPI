using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using StardewModdingAPI.Utilities;

namespace StardewModdingAPI.Tests
{
    /// <summary>Unit tests for <see cref="SDate"/>.</summary>
    [TestFixture]
    internal class SDateTests
    {
        /*********
        ** Properties
        *********/
        /// <summary>All valid seasons.</summary>
        private static string[] ValidSeasons = { "spring", "summer", "fall", "winter" };

        /// <summary>All valid days of a month.</summary>
        private static int[] ValidDays = Enumerable.Range(1, 28).ToArray();


        /*********
        ** Unit tests
        *********/
        /****
        ** Constructor
        ****/
        [Test(Description = "Assert that the constructor sets the expected values for all valid dates.")]
        public void Constructor_SetsExpectedValues([ValueSource(nameof(SDateTests.ValidSeasons))] string season, [ValueSource(nameof(SDateTests.ValidDays))] int day, [Values(1, 2, 100)] int year)
        {
            // act
            SDate date = new SDate(day, season, year);

            // assert
            Assert.AreEqual(day, date.Day);
            Assert.AreEqual(season, date.Season);
            Assert.AreEqual(year, date.Year);
        }

        [Test(Description = "Assert that the constructor throws an exception if the values are invalid.")]
        [TestCase(01, "Spring", 1)] // seasons are case-sensitive
        [TestCase(01, "springs", 1)] // invalid season name
        [TestCase(-1, "spring", 1)] // day < 0
        [TestCase(29, "spring", 1)] // day > 28
        [TestCase(01, "spring", -1)] // year < 1
        [TestCase(01, "spring", 0)] // year < 1
        [SuppressMessage("ReSharper", "AssignmentIsFullyDiscarded", Justification = "Deliberate for unit test.")]
        public void Constructor_RejectsInvalidValues(int day, string season, int year)
        {
            // act & assert
            Assert.Throws<ArgumentException>(() => _ = new SDate(day, season, year), "Constructing the invalid date didn't throw the expected exception.");
        }

        /****
        ** ToString
        ****/
        [Test(Description = "Assert that ToString returns the expected string.")]
        [TestCase("14 spring Y1", ExpectedResult = "14 spring Y1")]
        [TestCase("01 summer Y16", ExpectedResult = "01 summer Y16")]
        [TestCase("28 fall Y10", ExpectedResult = "28 fall Y10")]
        [TestCase("01 winter Y1", ExpectedResult = "01 winter Y1")]
        public string ToString(string dateStr)
        {
            return this.ParseDate(dateStr).ToString();
        }

        /****
        ** AddDays
        ****/
        [Test(Description = "Assert that AddDays returns the expected date.")]
        [TestCase("01 spring Y1", 15, ExpectedResult = "16 spring Y1")] // day transition
        [TestCase("01 spring Y1", 28, ExpectedResult = "01 summer Y1")] // season transition
        [TestCase("01 spring Y1", 28 * 4, ExpectedResult = "01 spring Y2")] // year transition
        [TestCase("01 spring Y1", 28 * 7 + 17, ExpectedResult = "18 winter Y2")] // year transition
        [TestCase("15 spring Y1", -14, ExpectedResult = "01 spring Y1")] // negative day transition
        [TestCase("15 summer Y1", -28, ExpectedResult = "15 spring Y1")] // negative season transition
        [TestCase("15 summer Y2", -28 * 4, ExpectedResult = "15 summer Y1")] // negative year transition
        [TestCase("01 spring Y3", -(28 * 7 + 17), ExpectedResult = "12 spring Y1")] // negative year transition
        public string AddDays(string dateStr, int addDays)
        {
            return this.ParseDate(dateStr).AddDays(addDays).ToString();
        }

        [Test(Description = "Assert that the equality operators work as expected")]
        public void EqualityOperators()
        {
            SDate s1 = new SDate(1, "spring", 2);
            SDate s2 = new SDate(1, "spring", 2);
            SDate s3 = new SDate(1, "spring", 3);
            SDate s4 = new SDate(12, "spring", 2);
            SDate s5 = new SDate(1, "summer", 2);

            Assert.AreEqual(true, s1 == s2);
            Assert.AreNotEqual(true, s1 == s3);
            Assert.AreNotEqual(true, s1 == s4);
            Assert.AreNotEqual(true, s1 == s5);
        }

        [Test(Description = "Assert that the comparison operators work as expected")]
        public void ComparisonOperators()
        {
            SDate s1 = new SDate(1, "spring", 2);
            SDate s2 = new SDate(1, "spring", 2);
            SDate s3 = new SDate(1, "spring", 3);
            SDate s4 = new SDate(12, "spring", 2);
            SDate s5 = new SDate(1, "summer", 2);
            SDate s6 = new SDate(1, "winter", 1);
            SDate s7 = new SDate(13, "fall", 1);

            Assert.AreEqual(true, s1 <= s2);
            Assert.AreEqual(true, s1 >= s2);
            Assert.AreEqual(true, s1 < s4);
            Assert.AreEqual(true, s1 <= s4);
            Assert.AreEqual(true, s4 > s1);
            Assert.AreEqual(true, s4 >= s1);
            Assert.AreEqual(true, s5 > s7);
            Assert.AreEqual(true, s5 >= s7);
            Assert.AreEqual(true, s6 < s5);
            Assert.AreEqual(true, s6 <= s5);
            Assert.AreEqual(true, s1 < s5);
            Assert.AreEqual(true, s1 <= s5);
            Assert.AreEqual(true, s5 > s1);
            Assert.AreEqual(true, s5 >= s1);
        }

        /*********
        ** Private methods
        *********/
        /// <summary>Convert a string date into a game date, to make unit tests easier to read.</summary>
        /// <param name="dateStr">The date string like "dd MMMM yy".</param>
        private SDate ParseDate(string dateStr)
        {
            void Fail(string reason) => throw new AssertionException($"Couldn't parse date '{dateStr}' because {reason}.");

            // parse
            Match match = Regex.Match(dateStr, @"^(?<day>\d+) (?<season>\w+) Y(?<year>\d+)$");
            if (!match.Success)
                Fail("it doesn't match expected pattern (should be like 28 spring Y1)");

            // extract parts
            string season = match.Groups["season"].Value;
            if (!int.TryParse(match.Groups["day"].Value, out int day))
                Fail($"'{match.Groups["day"].Value}' couldn't be parsed as a day.");
            if (!int.TryParse(match.Groups["year"].Value, out int year))
                Fail($"'{match.Groups["year"].Value}' couldn't be parsed as a year.");

            // build date
            return new SDate(day, season, year);
        }
    }
}
