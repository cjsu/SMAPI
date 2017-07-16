using System;
using System.Text.RegularExpressions;

namespace StardewModdingAPI
{
    /// <summary>A semantic version with an optional release tag.</summary>
    public class SemanticVersion : ISemanticVersion
    {
        /*********
        ** Properties
        *********/
        /// <summary>A regular expression matching a semantic version string.</summary>
        /// <remarks>
        /// This pattern is derived from the BNF documentation in the <a href="https://github.com/mojombo/semver">semver repo</a>,
        /// with three important deviations intended to support Stardew Valley mod conventions:
        /// - allows short-form "x.y" versions;
        /// - allows hyphens in prerelease tags as synonyms for dots (like "-unofficial-update.3");
        /// - doesn't allow '+build' suffixes.
        /// </remarks>
        private static readonly Regex Regex = new Regex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)(\.(?<patch>0|[1-9]\d*))?(?:-(?<prerelease>([a-z0-9]+[\-\.]?)+))?$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);


        /*********
        ** Accessors
        *********/
        /// <summary>The major version incremented for major API changes.</summary>
        public int MajorVersion { get; }

        /// <summary>The minor version incremented for backwards-compatible changes.</summary>
        public int MinorVersion { get; }

        /// <summary>The patch version for backwards-compatible bug fixes.</summary>
        public int PatchVersion { get; }

        /// <summary>An optional build tag.</summary>
        public string Build { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="major">The major version incremented for major API changes.</param>
        /// <param name="minor">The minor version incremented for backwards-compatible changes.</param>
        /// <param name="patch">The patch version for backwards-compatible bug fixes.</param>
        /// <param name="build">An optional build tag.</param>
        public SemanticVersion(int major, int minor, int patch, string build = null)
        {
            this.MajorVersion = major;
            this.MinorVersion = minor;
            this.PatchVersion = patch;
            this.Build = this.GetNormalisedTag(build);
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="version">The semantic version string.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="version"/> is null.</exception>
        /// <exception cref="FormatException">The <paramref name="version"/> is not a valid semantic version.</exception>
        public SemanticVersion(string version)
        {
            // parse
            if (version == null)
                throw new ArgumentNullException(nameof(version), "The input version string can't be null.");
            var match = SemanticVersion.Regex.Match(version.Trim());
            if (!match.Success)
                throw new FormatException($"The input '{version}' isn't a valid semantic version.");

            // initialise
            this.MajorVersion = int.Parse(match.Groups["major"].Value);
            this.MinorVersion = match.Groups["minor"].Success ? int.Parse(match.Groups["minor"].Value) : 0;
            this.PatchVersion = match.Groups["patch"].Success ? int.Parse(match.Groups["patch"].Value) : 0;
            this.Build = match.Groups["prerelease"].Success ? this.GetNormalisedTag(match.Groups["prerelease"].Value) : null;
        }

        /// <summary>Get an integer indicating whether this version precedes (less than 0), supercedes (more than 0), or is equivalent to (0) the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="other"/> value is null.</exception>
        /// <remarks>The implementation is defined by Semantic Version 2.0 (http://semver.org/).</remarks>
        public int CompareTo(ISemanticVersion other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            const int same = 0;
            const int curNewer = 1;
            const int curOlder = -1;

            // compare stable versions
            if (this.MajorVersion != other.MajorVersion)
                return this.MajorVersion.CompareTo(other.MajorVersion);
            if (this.MinorVersion != other.MinorVersion)
                return this.MinorVersion.CompareTo(other.MinorVersion);
            if (this.PatchVersion != other.PatchVersion)
                return this.PatchVersion.CompareTo(other.PatchVersion);
            if (this.Build == other.Build)
                return same;

            // stable supercedes pre-release
            bool curIsStable = string.IsNullOrWhiteSpace(this.Build);
            bool otherIsStable = string.IsNullOrWhiteSpace(other.Build);
            if (curIsStable)
                return curNewer;
            if (otherIsStable)
                return curOlder;

            // compare two pre-release tag values
            string[] curParts = this.Build.Split('.', '-');
            string[] otherParts = other.Build.Split('.', '-');
            for (int i = 0; i < curParts.Length; i++)
            {
                // longer prerelease tag supercedes if otherwise equal
                if (otherParts.Length <= i)
                    return curNewer;

                // compare if different
                if (curParts[i] != otherParts[i])
                {
                    // compare numerically if possible
                    {
                        int curNum, otherNum;
                        if (int.TryParse(curParts[i], out curNum) && int.TryParse(otherParts[i], out otherNum))
                            return curNum.CompareTo(otherNum);
                    }

                    // else compare lexically
                    return string.Compare(curParts[i], otherParts[i], StringComparison.OrdinalIgnoreCase);
                }
            }

            // fallback (this should never happen)
            return string.Compare(this.ToString(), other.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>Get whether this version is older than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        public bool IsOlderThan(ISemanticVersion other)
        {
            return this.CompareTo(other) < 0;
        }

        /// <summary>Get whether this version is older than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        /// <exception cref="FormatException">The specified version is not a valid semantic version.</exception>
        public bool IsOlderThan(string other)
        {
            return this.IsOlderThan(new SemanticVersion(other));
        }

        /// <summary>Get whether this version is newer than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        public bool IsNewerThan(ISemanticVersion other)
        {
            return this.CompareTo(other) > 0;
        }

        /// <summary>Get whether this version is newer than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        /// <exception cref="FormatException">The specified version is not a valid semantic version.</exception>
        public bool IsNewerThan(string other)
        {
            return this.IsNewerThan(new SemanticVersion(other));
        }

        /// <summary>Get whether this version is between two specified versions (inclusively).</summary>
        /// <param name="min">The minimum version.</param>
        /// <param name="max">The maximum version.</param>
        public bool IsBetween(ISemanticVersion min, ISemanticVersion max)
        {
            return this.CompareTo(min) >= 0 && this.CompareTo(max) <= 0;
        }

        /// <summary>Get whether this version is between two specified versions (inclusively).</summary>
        /// <param name="min">The minimum version.</param>
        /// <param name="max">The maximum version.</param>
        /// <exception cref="FormatException">One of the specified versions is not a valid semantic version.</exception>
        public bool IsBetween(string min, string max)
        {
            return this.IsBetween(new SemanticVersion(min), new SemanticVersion(max));
        }

        /// <summary>Get a string representation of the version.</summary>
        public override string ToString()
        {
            // version
            string result = this.PatchVersion != 0
                ? $"{this.MajorVersion}.{this.MinorVersion}.{this.PatchVersion}"
                : $"{this.MajorVersion}.{this.MinorVersion}";

            // tag
            string tag = this.Build;
            if (tag != null)
                result += $"-{tag}";
            return result;
        }

        /// <summary>Parse a version string without throwing an exception if it fails.</summary>
        /// <param name="version">The version string.</param>
        /// <param name="parsed">The parsed representation.</param>
        /// <returns>Returns whether parsing the version succeeded.</returns>
        internal static bool TryParse(string version, out ISemanticVersion parsed)
        {
            try
            {
                parsed = new SemanticVersion(version);
                return true;
            }
            catch
            {
                parsed = null;
                return false;
            }
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a normalised build tag.</summary>
        /// <param name="tag">The tag to normalise.</param>
        private string GetNormalisedTag(string tag)
        {
            tag = tag?.Trim();
            if (string.IsNullOrWhiteSpace(tag) || tag == "0") // '0' from incorrect examples in old SMAPI documentation
                return null;
            return tag;
        }
    }
}
