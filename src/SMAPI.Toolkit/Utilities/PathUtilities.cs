using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StardewModdingAPI.Toolkit.Utilities
{
    /// <summary>Provides utilities for normalising file paths.</summary>
    public static class PathUtilities
    {
        /*********
        ** Fields
        *********/
        /// <summary>The possible directory separator characters in a file path.</summary>
        private static readonly char[] PossiblePathSeparators = new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.Distinct().ToArray();

        /// <summary>The preferred directory separator chaeacter in an asset key.</summary>
        private static readonly string PreferredPathSeparator = Path.DirectorySeparatorChar.ToString();


        /*********
        ** Public methods
        *********/
        /// <summary>Get the segments from a path (e.g. <c>/usr/bin/boop</c> => <c>usr</c>, <c>bin</c>, and <c>boop</c>).</summary>
        /// <param name="path">The path to split.</param>
        /// <param name="limit">The number of segments to match. Any additional segments will be merged into the last returned part.</param>
        public static string[] GetSegments(string path, int? limit = null)
        {
            return limit.HasValue
                ? path.Split(PathUtilities.PossiblePathSeparators, limit.Value, StringSplitOptions.RemoveEmptyEntries)
                : path.Split(PathUtilities.PossiblePathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Normalise path separators in a file path.</summary>
        /// <param name="path">The file path to normalise.</param>
        [Pure]
        public static string NormalisePathSeparators(string path)
        {
            string[] parts = PathUtilities.GetSegments(path);
            string normalised = string.Join(PathUtilities.PreferredPathSeparator, parts);
            if (path.StartsWith(PathUtilities.PreferredPathSeparator))
                normalised = PathUtilities.PreferredPathSeparator + normalised; // keep root slash
            return normalised;
        }

        /// <summary>Get a directory or file path relative to a given source path.</summary>
        /// <param name="sourceDir">The source folder path.</param>
        /// <param name="targetPath">The target folder or file path.</param>
        [Pure]
        public static string GetRelativePath(string sourceDir, string targetPath)
        {
            // convert to URIs
            Uri from = new Uri(sourceDir.TrimEnd(PathUtilities.PossiblePathSeparators) + "/");
            Uri to = new Uri(targetPath.TrimEnd(PathUtilities.PossiblePathSeparators) + "/");
            if (from.Scheme != to.Scheme)
                throw new InvalidOperationException($"Can't get path for '{targetPath}' relative to '{sourceDir}'.");

            // get relative path
            string relative = PathUtilities.NormalisePathSeparators(Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString()));
            if (relative == "")
                relative = "./";
            return relative;
        }

        /// <summary>Get whether a path is relative and doesn't try to climb out of its containing folder (e.g. doesn't contain <c>../</c>).</summary>
        /// <param name="path">The path to check.</param>
        public static bool IsSafeRelativePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true;

            return
                !Path.IsPathRooted(path)
                && PathUtilities.GetSegments(path).All(segment => segment.Trim() != "..");
        }

        /// <summary>Get whether a string is a valid 'slug', containing only basic characters that are safe in all contexts (e.g. filenames, URLs, etc).</summary>
        /// <param name="str">The string to check.</param>
        public static bool IsSlug(string str)
        {
            return !Regex.IsMatch(str, "[^a-z0-9_.-]", RegexOptions.IgnoreCase);
        }
    }
}
