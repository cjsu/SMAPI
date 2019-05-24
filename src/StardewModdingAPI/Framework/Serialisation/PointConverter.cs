using System;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;
using StardewModdingAPI.Toolkit.Serialisation;
using StardewModdingAPI.Toolkit.Serialisation.Converters;

namespace StardewModdingAPI.Framework.Serialisation
{
    /// <summary>Handles deserialisation of <see cref="PointConverter"/> for crossplatform compatibility.</summary>
    /// <remarks>
    /// - Linux/Mac format: { "X": 1, "Y": 2 }
    /// - Windows format:   "1, 2"
    /// </remarks>
    internal class PointConverter : SimpleReadOnlyConverter<Point>
    {
        /*********
        ** Protected methods
        *********/
        /// <summary>Read a JSON object.</summary>
        /// <param name="obj">The JSON object to read.</param>
        /// <param name="path">The path to the current JSON node.</param>
        protected override Point ReadObject(JObject obj, string path)
        {
            int x = obj.ValueIgnoreCase<int>(nameof(Point.X));
            int y = obj.ValueIgnoreCase<int>(nameof(Point.Y));
            return new Point(x, y);
        }

        /// <summary>Read a JSON string.</summary>
        /// <param name="str">The JSON string value.</param>
        /// <param name="path">The path to the current JSON node.</param>
        protected override Point ReadString(string str, string path)
        {
            string[] parts = str.Split(',');
            if (parts.Length != 2)
                throw new SParseException($"Can't parse {typeof(Point).Name} from invalid value '{str}' (path: {path}).");

            int x = Convert.ToInt32(parts[0]);
            int y = Convert.ToInt32(parts[1]);
            return new Point(x, y);
        }
    }
}
