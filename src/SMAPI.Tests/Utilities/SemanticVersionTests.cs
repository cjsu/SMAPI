using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using NUnit.Framework;
using StardewModdingAPI.Framework;

namespace StardewModdingAPI.Tests.Utilities
{
    /// <summary>Unit tests for <see cref="SemanticVersion"/>.</summary>
    [TestFixture]
    internal class SemanticVersionTests
    {
        /*********
        ** Unit tests
        *********/
        /****
        ** Constructor
        ****/
        [Test(Description = "Assert that the constructor sets the expected values for all valid versions when constructed from a string.")]
        [TestCase("1.0", ExpectedResult = "1.0")]
        [TestCase("1.0.0", ExpectedResult = "1.0")]
        [TestCase("3000.4000.5000", ExpectedResult = "3000.4000.5000")]
        [TestCase("1.2-some-tag.4", ExpectedResult = "1.2-some-tag.4")]
        [TestCase("1.2.3-some-tag.4", ExpectedResult = "1.2.3-some-tag.4")]
        [TestCase("1.2.3-SoME-tAg.4", ExpectedResult = "1.2.3-SoME-tAg.4")]
        [TestCase("1.2.3-some-tag.4      ", ExpectedResult = "1.2.3-some-tag.4")]
        public string Constructor_FromString(string input)
        {
            return new SemanticVersion(input).ToString();
        }

        [Test(Description = "Assert that the constructor sets the expected values for all valid versions when constructed from the individual numbers.")]
        [TestCase(1, 0, 0, null, ExpectedResult = "1.0")]
        [TestCase(3000, 4000, 5000, null, ExpectedResult = "3000.4000.5000")]
        [TestCase(1, 2, 3, "", ExpectedResult = "1.2.3")]
        [TestCase(1, 2, 3, "    ", ExpectedResult = "1.2.3")]
        [TestCase(1, 2, 3, "0", ExpectedResult = "1.2.3-0")]
        [TestCase(1, 2, 3, "some-tag.4", ExpectedResult = "1.2.3-some-tag.4")]
        [TestCase(1, 2, 3, "sOMe-TaG.4", ExpectedResult = "1.2.3-sOMe-TaG.4")]
        [TestCase(1, 2, 3, "some-tag.4   ", ExpectedResult = "1.2.3-some-tag.4")]
        public string Constructor_FromParts(int major, int minor, int patch, string tag)
        {
            // act
            ISemanticVersion version = new SemanticVersion(major, minor, patch, tag);

            // assert
            Assert.AreEqual(major, version.MajorVersion, "The major version doesn't match the given value.");
            Assert.AreEqual(minor, version.MinorVersion, "The minor version doesn't match the given value.");
            Assert.AreEqual(patch, version.PatchVersion, "The patch version doesn't match the given value.");
            Assert.AreEqual(string.IsNullOrWhiteSpace(tag) ? null : tag.Trim(), version.PrereleaseTag, "The tag doesn't match the given value.");
            return version.ToString();
        }

        [Test(Description = "Assert that the constructor throws the expected exception for invalid versions when constructed from the individual numbers.")]
        [TestCase(0, 0, 0, null)]
        [TestCase(-1, 0, 0, null)]
        [TestCase(0, -1, 0, null)]
        [TestCase(0, 0, -1, null)]
        [TestCase(1, 0, 0, "-tag")]
        [TestCase(1, 0, 0, "tag spaces")]
        [TestCase(1, 0, 0, "tag~")]
        public void Constructor_FromParts_WithInvalidValues(int major, int minor, int patch, string tag)
        {
            this.AssertAndLogException<FormatException>(() => new SemanticVersion(major, minor, patch, tag));
        }

        [Test(Description = "Assert that the constructor sets the expected values for all valid versions when constructed from an assembly version.")]
        [TestCase(1, 0, 0, ExpectedResult = "1.0")]
        [TestCase(1, 2, 3, ExpectedResult = "1.2.3")]
        [TestCase(3000, 4000, 5000, ExpectedResult = "3000.4000.5000")]
        public string Constructor_FromAssemblyVersion(int major, int minor, int patch)
        {
            // act
            ISemanticVersion version = new SemanticVersion(new Version(major, minor, patch));

            // assert
            Assert.AreEqual(major, version.MajorVersion, "The major version doesn't match the given value.");
            Assert.AreEqual(minor, version.MinorVersion, "The minor version doesn't match the given value.");
            Assert.AreEqual(patch, version.PatchVersion, "The patch version doesn't match the given value.");
            return version.ToString();
        }

        [Test(Description = "Assert that the constructor throws the expected exception for invalid versions.")]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("1")]
        [TestCase("01.0")]
        [TestCase("1.05")]
        [TestCase("1.5.06")] // leading zeros specifically prohibited by spec
        [TestCase("1.2.3.4")]
        [TestCase("1.apple")]
        [TestCase("1.2.apple")]
        [TestCase("1.2.3.apple")]
        [TestCase("1..2..3")]
        [TestCase("1.2.3-")]
        [TestCase("1.2.3--some-tag")]
        [TestCase("1.2.3-some-tag...")]
        [TestCase("1.2.3-some-tag...4")]
        [TestCase("apple")]
        [TestCase("-apple")]
        [TestCase("-5")]
        public void Constructor_FromString_WithInvalidValues(string input)
        {
            if (input == null)
                this.AssertAndLogException<ArgumentNullException>(() => new SemanticVersion(input));
            else
                this.AssertAndLogException<FormatException>(() => new SemanticVersion(input));
        }

        /****
        ** CompareTo
        ****/
        [Test(Description = "Assert that version.CompareTo returns the expected value.")]
        // equal
        [TestCase("0.5.7", "0.5.7", ExpectedResult = 0)]
        [TestCase("1.0", "1.0", ExpectedResult = 0)]
        [TestCase("1.0-beta", "1.0-beta", ExpectedResult = 0)]
        [TestCase("1.0-beta.10", "1.0-beta.10", ExpectedResult = 0)]
        [TestCase("1.0-beta", "1.0-beta   ", ExpectedResult = 0)]

        // less than
        [TestCase("0.5.7", "0.5.8", ExpectedResult = -1)]
        [TestCase("1.0", "1.1", ExpectedResult = -1)]
        [TestCase("1.0-beta", "1.0", ExpectedResult = -1)]
        [TestCase("1.0-beta", "1.0-beta.2", ExpectedResult = -1)]
        [TestCase("1.0-beta.1", "1.0-beta.2", ExpectedResult = -1)]
        [TestCase("1.0-beta.2", "1.0-beta.10", ExpectedResult = -1)]
        [TestCase("1.0-beta-2", "1.0-beta-10", ExpectedResult = -1)]
        [TestCase("1.0-unofficial.1", "1.0-beta.1", ExpectedResult = -1)] // special case: 'unofficial' has lower priority than official releases

        // more than
        [TestCase("0.5.8", "0.5.7", ExpectedResult = 1)]
        [TestCase("1.1", "1.0", ExpectedResult = 1)]
        [TestCase("1.0", "1.0-beta", ExpectedResult = 1)]
        [TestCase("1.0-beta.2", "1.0-beta", ExpectedResult = 1)]
        [TestCase("1.0-beta.2", "1.0-beta.1", ExpectedResult = 1)]
        [TestCase("1.0-beta.10", "1.0-beta.2", ExpectedResult = 1)]
        [TestCase("1.0-beta-10", "1.0-beta-2", ExpectedResult = 1)]
        public int CompareTo(string versionStrA, string versionStrB)
        {
            ISemanticVersion versionA = new SemanticVersion(versionStrA);
            ISemanticVersion versionB = new SemanticVersion(versionStrB);
            return versionA.CompareTo(versionB);
        }

        /****
        ** IsOlderThan
        ****/
        [Test(Description = "Assert that version.IsOlderThan returns the expected value.")]
        // keep test cases in sync with CompareTo for simplicity.
        // equal
        [TestCase("0.5.7", "0.5.7", ExpectedResult = false)]
        [TestCase("1.0", "1.0", ExpectedResult = false)]
        [TestCase("1.0-beta", "1.0-beta", ExpectedResult = false)]
        [TestCase("1.0-beta.10", "1.0-beta.10", ExpectedResult = false)]
        [TestCase("1.0-beta", "1.0-beta   ", ExpectedResult = false)]

        // less than
        [TestCase("0.5.7", "0.5.8", ExpectedResult = true)]
        [TestCase("1.0", "1.1", ExpectedResult = true)]
        [TestCase("1.0-beta", "1.0", ExpectedResult = true)]
        [TestCase("1.0-beta", "1.0-beta.2", ExpectedResult = true)]
        [TestCase("1.0-beta.1", "1.0-beta.2", ExpectedResult = true)]
        [TestCase("1.0-beta.2", "1.0-beta.10", ExpectedResult = true)]
        [TestCase("1.0-beta-2", "1.0-beta-10", ExpectedResult = true)]

        // more than
        [TestCase("0.5.8", "0.5.7", ExpectedResult = false)]
        [TestCase("1.1", "1.0", ExpectedResult = false)]
        [TestCase("1.0", "1.0-beta", ExpectedResult = false)]
        [TestCase("1.0-beta.2", "1.0-beta", ExpectedResult = false)]
        [TestCase("1.0-beta.2", "1.0-beta.1", ExpectedResult = false)]
        [TestCase("1.0-beta.10", "1.0-beta.2", ExpectedResult = false)]
        [TestCase("1.0-beta-10", "1.0-beta-2", ExpectedResult = false)]
        public bool IsOlderThan(string versionStrA, string versionStrB)
        {
            ISemanticVersion versionA = new SemanticVersion(versionStrA);
            ISemanticVersion versionB = new SemanticVersion(versionStrB);
            return versionA.IsOlderThan(versionB);
        }

        /****
        ** IsNewerThan
        ****/
        [Test(Description = "Assert that version.IsNewerThan returns the expected value.")]
        // keep test cases in sync with CompareTo for simplicity.
        // equal
        [TestCase("0.5.7", "0.5.7", ExpectedResult = false)]
        [TestCase("1.0", "1.0", ExpectedResult = false)]
        [TestCase("1.0-beta", "1.0-beta", ExpectedResult = false)]
        [TestCase("1.0-beta.10", "1.0-beta.10", ExpectedResult = false)]
        [TestCase("1.0-beta", "1.0-beta   ", ExpectedResult = false)]

        // less than
        [TestCase("0.5.7", "0.5.8", ExpectedResult = false)]
        [TestCase("1.0", "1.1", ExpectedResult = false)]
        [TestCase("1.0-beta", "1.0", ExpectedResult = false)]
        [TestCase("1.0-beta", "1.0-beta.2", ExpectedResult = false)]
        [TestCase("1.0-beta.1", "1.0-beta.2", ExpectedResult = false)]
        [TestCase("1.0-beta.2", "1.0-beta.10", ExpectedResult = false)]
        [TestCase("1.0-beta-2", "1.0-beta-10", ExpectedResult = false)]

        // more than
        [TestCase("0.5.8", "0.5.7", ExpectedResult = true)]
        [TestCase("1.1", "1.0", ExpectedResult = true)]
        [TestCase("1.0", "1.0-beta", ExpectedResult = true)]
        [TestCase("1.0-beta.2", "1.0-beta", ExpectedResult = true)]
        [TestCase("1.0-beta.2", "1.0-beta.1", ExpectedResult = true)]
        [TestCase("1.0-beta.10", "1.0-beta.2", ExpectedResult = true)]
        [TestCase("1.0-beta-10", "1.0-beta-2", ExpectedResult = true)]
        public bool IsNewerThan(string versionStrA, string versionStrB)
        {
            ISemanticVersion versionA = new SemanticVersion(versionStrA);
            ISemanticVersion versionB = new SemanticVersion(versionStrB);
            return versionA.IsNewerThan(versionB);
        }

        /****
        ** IsBetween
        ****/
        [Test(Description = "Assert that version.IsNewerThan returns the expected value.")]
        // is between
        [TestCase("0.5.7-beta.3", "0.5.7-beta.3", "0.5.7-beta.3", ExpectedResult = true)]
        [TestCase("1.0", "1.0", "1.1", ExpectedResult = true)]
        [TestCase("1.0", "1.0-beta", "1.1", ExpectedResult = true)]
        [TestCase("1.0", "0.5", "1.1", ExpectedResult = true)]
        [TestCase("1.0-beta.2", "1.0-beta.1", "1.0-beta.3", ExpectedResult = true)]
        [TestCase("1.0-beta-2", "1.0-beta-1", "1.0-beta-3", ExpectedResult = true)]

        // is not between
        [TestCase("1.0-beta", "1.0", "1.1", ExpectedResult = false)]
        [TestCase("1.0", "1.1", "1.0", ExpectedResult = false)]
        [TestCase("1.0-beta.2", "1.1", "1.0", ExpectedResult = false)]
        [TestCase("1.0-beta.2", "1.0-beta.10", "1.0-beta.3", ExpectedResult = false)]
        [TestCase("1.0-beta-2", "1.0-beta-10", "1.0-beta-3", ExpectedResult = false)]
        public bool IsBetween(string versionStr, string lowerStr, string upperStr)
        {
            ISemanticVersion lower = new SemanticVersion(lowerStr);
            ISemanticVersion upper = new SemanticVersion(upperStr);
            ISemanticVersion version = new SemanticVersion(versionStr);
            return version.IsBetween(lower, upper);
        }

        /****
        ** Serializable
        ****/
        [Test(Description = "Assert that SemanticVersion can be round-tripped through JSON with no special configuration.")]
        [TestCase("1.0")]
        public void Serializable(string versionStr)
        {
            // act
            string json = JsonConvert.SerializeObject(new SemanticVersion(versionStr));
            SemanticVersion after = JsonConvert.DeserializeObject<SemanticVersion>(json);

            // assert
            Assert.IsNotNull(after, "The semantic version after deserialization is unexpectedly null.");
            Assert.AreEqual(versionStr, after.ToString(), "The semantic version after deserialization doesn't match the input version.");
        }

        /****
        ** GameVersion
        ****/
        [Test(Description = "Assert that the GameVersion subclass correctly parses legacy game versions.")]
        [TestCase("1.0")]
        [TestCase("1.01")]
        [TestCase("1.02")]
        [TestCase("1.03")]
        [TestCase("1.04")]
        [TestCase("1.05")]
        [TestCase("1.051")]
        [TestCase("1.051b")]
        [TestCase("1.06")]
        [TestCase("1.07")]
        [TestCase("1.07a")]
        [TestCase("1.08")]
        [TestCase("1.1")]
        [TestCase("1.11")]
        [TestCase("1.2")]
        [TestCase("1.2.15")]
        public void GameVersion(string versionStr)
        {
            // act
            GameVersion version = new GameVersion(versionStr);

            // assert
            Assert.AreEqual(versionStr, version.ToString(), "The game version did not round-trip to the same value.");
            Assert.IsTrue(version.IsOlderThan(new SemanticVersion("1.2.30")), "The game version should be considered older than the later semantic versions.");
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Assert that the expected exception type is thrown, and log the action output and thrown exception.</summary>
        /// <typeparam name="T">The expected exception type.</typeparam>
        /// <param name="action">The action which may throw the exception.</param>
        /// <param name="message">The message to log if the expected exception isn't thrown.</param>
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The message argument is deliberately only used in precondition checks since this is an assertion method.")]
        private void AssertAndLogException<T>(Func<object> action, string message = null)
            where T : Exception
        {
            this.AssertAndLogException<T>(() =>
            {
                object result = action();
                TestContext.WriteLine($"Func result: {result}");
            });
        }

        /// <summary>Assert that the expected exception type is thrown, and log the thrown exception.</summary>
        /// <typeparam name="T">The expected exception type.</typeparam>
        /// <param name="action">The action which may throw the exception.</param>
        /// <param name="message">The message to log if the expected exception isn't thrown.</param>
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = "The message argument is deliberately only used in precondition checks since this is an assertion method.")]
        private void AssertAndLogException<T>(Action action, string message = null)
            where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                TestContext.WriteLine($"Exception thrown:\n{ex}");
                return;
            }
            catch (Exception ex) when (!(ex is AssertionException))
            {
                TestContext.WriteLine($"Exception thrown:\n{ex}");
                Assert.Fail(message ?? $"Didn't throw the expected exception; expected {typeof(T).FullName}, got {ex.GetType().FullName}.");
            }

            // no exception thrown
            Assert.Fail(message ?? "Didn't throw an exception.");
        }
    }
}
