using System;

namespace StardewModdingAPI
{
    /// <summary>A semantic version with an optional release tag.</summary>
    public interface ISemanticVersion : IComparable<ISemanticVersion>, IEquatable<ISemanticVersion>
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The major version incremented for major API changes.</summary>
        int MajorVersion { get; }

        /// <summary>The minor version incremented for backwards-compatible changes.</summary>
        int MinorVersion { get; }

        /// <summary>The patch version for backwards-compatible bug fixes.</summary>
        int PatchVersion { get; }

        /// <summary>An optional prerelease tag.</summary>
        string PrereleaseTag { get; }


        /*********
        ** Accessors
        *********/
        /// <summary>Whether this is a prerelease version.</summary>
        bool IsPrerelease();

        /// <summary>Get whether this version is older than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        bool IsOlderThan(ISemanticVersion other);

        /// <summary>Get whether this version is older than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        /// <exception cref="FormatException">The specified version is not a valid semantic version.</exception>
        bool IsOlderThan(string other);

        /// <summary>Get whether this version is newer than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        bool IsNewerThan(ISemanticVersion other);

        /// <summary>Get whether this version is newer than the specified version.</summary>
        /// <param name="other">The version to compare with this instance.</param>
        /// <exception cref="FormatException">The specified version is not a valid semantic version.</exception>
        bool IsNewerThan(string other);

        /// <summary>Get whether this version is between two specified versions (inclusively).</summary>
        /// <param name="min">The minimum version.</param>
        /// <param name="max">The maximum version.</param>
        bool IsBetween(ISemanticVersion min, ISemanticVersion max);

        /// <summary>Get whether this version is between two specified versions (inclusively).</summary>
        /// <param name="min">The minimum version.</param>
        /// <param name="max">The maximum version.</param>
        /// <exception cref="FormatException">One of the specified versions is not a valid semantic version.</exception>
        bool IsBetween(string min, string max);

        /// <summary>Get a string representation of the version.</summary>
        string ToString();
    }
}
