using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace StardewModdingAPI.Toolkit.Utilities
{
    /// <summary>Provides utilities for normalizing file paths.</summary>
    public static class PathUtilities
    {
        /*********
        ** Accessors
        *********/
        /// <summary>The possible directory separator characters in a file path.</summary>
        public static readonly char[] PossiblePathSeparators = new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }.Distinct().ToArray();

        /// <summary>The preferred directory separator character in an asset key.</summary>
        public static readonly string PreferredPathSeparator = Path.DirectorySeparatorChar.ToString();


        /*********
        ** Public methods
        *********/
        /// <summary>Get the segments from a path (e.g. <c>/usr/bin/example</c> => <c>usr</c>, <c>bin</c>, and <c>example</c>).</summary>
        /// <param name="path">The path to split.</param>
        /// <param name="limit">The number of segments to match. Any additional segments will be merged into the last returned part.</param>
        public static string[] GetSegments(string path, int? limit = null)
        {
            return limit.HasValue
                ? path.Split(PathUtilities.PossiblePathSeparators, limit.Value, StringSplitOptions.RemoveEmptyEntries)
                : path.Split(PathUtilities.PossiblePathSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Normalize path separators in a file path.</summary>
        /// <param name="path">The file path to normalize.</param>
        [Pure]
        public static string NormalizePathSeparators(string path)
        {
            string[] parts = PathUtilities.GetSegments(path);
            string normalized = string.Join(PathUtilities.PreferredPathSeparator, parts);
            if (path.StartsWith(PathUtilities.PreferredPathSeparator))
                normalized = PathUtilities.PreferredPathSeparator + normalized; // keep root slash
            return normalized;
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
            string relative = PathUtilities.NormalizePathSeparators(Uri.UnescapeDataString(from.MakeRelativeUri(to).ToString()));
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
