using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Netcode;
using Pathoschild.Stardew.LookupAnything.Framework.Constants;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Network;

namespace Pathoschild.Stardew.LookupAnything.Framework
{
    /// <summary>Provides extension methods for <see cref="ITranslationHelper"/>.</summary>
    internal static class TranslationHelperExtensions
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Select the correct translation based on the plural form.</summary>
        /// <param name="translations">The translation helper.</param>
        /// <param name="count">The number.</param>
        /// <param name="singleKey">The singular form.</param>
        /// <param name="pluralKey">The plural form.</param>
        public static Translation GetPlural(this ITranslationHelper translations, int count, string singleKey, string pluralKey)
        {
            return translations.Get(count == 1 ? singleKey : pluralKey);
        }

        /// <summary>Get a translated season name from the game.</summary>
        /// <param name="translations">The translation helper.</param>
        /// <param name="season">The English season name.</param>
        public static string GetSeasonName(this ITranslationHelper translations, string season)
        {
            if (string.IsNullOrWhiteSpace(season))
                return season;

            int id = Utility.getSeasonNumber(season);
            if (id == -1)
                throw new InvalidOperationException($"Can't translate unknown season '{season}'.");
            return Utility.getSeasonNameFromNumber(id);
        }

        /// <summary>Get translated season names from the game.</summary>
        /// <param name="translations">The translation helper.</param>
        /// <param name="seasons">The English season names.</param>
        public static IEnumerable<string> GetSeasonNames(this ITranslationHelper translations, IEnumerable<string> seasons)
        {
            foreach (string season in seasons)
                yield return translations.GetSeasonName(season);
        }

        /// <summary>Get a human-readable representation of a value.</summary>
        /// <param name="translations">The translation helper.</param>
        /// <param name="date">The game date.</param>
        /// <param name="withYear">Whether to include the year number.</param>
        public static string Stringify(this ITranslationHelper translations, SDate date, bool withYear)
        {
            string key = withYear ? L10n.Generic.DateWithYear : L10n.Generic.Date;
            return translations.Get(key, new
            {
                seasonNumber = Utility.getSeasonNumber(date.Season),
                seasonName = Utility.getSeasonNameFromNumber(Utility.getSeasonNumber(date.Season)),
                dayNumber = date.Day,
                year = date.Year
            });
        }

        /// <summary>Get a human-readable representation of a value.</summary>
        /// <param name="translations">The translation helper.</param>
        /// <param name="value">The underlying value.</param>
        public static string Stringify(this ITranslationHelper translations, object value)
        {
            switch (value)
            {
                case null:
                    return null;

                // net types
                case NetBool net:
                    return translations.Stringify(net.Value);
                case NetByte net:
                    return translations.Stringify(net.Value);
                case NetColor net:
                    return translations.Stringify(net.Value);
                case NetDancePartner net:
                    return translations.Stringify(net.Value?.Name);
                case NetDouble net:
                    return translations.Stringify(net.Value);
                case NetFloat net:
                    return translations.Stringify(net.Value);
                case NetGuid net:
                    return translations.Stringify(net.Value);
                case NetInt net:
                    return translations.Stringify(net.Value);
                case NetLocationRef net:
                    return translations.Stringify(net.Value?.uniqueName ?? net.Value?.Name);
                case NetLong net:
                    return translations.Stringify(net.Value);
                case NetPoint net:
                    return translations.Stringify(net.Value);
                case NetPosition net:
                    return translations.Stringify(net.Value);
                case NetRectangle net:
                    return translations.Stringify(net.Value);
                case NetString net:
                    return translations.Stringify(net.Value);
                case NetVector2 net:
                    return translations.Stringify(net.Value);

                // core types
                case bool boolean:
                    return translations.Get(boolean ? L10n.Generic.Yes : L10n.Generic.No);
                case Color color:
                    return $"(r:{color.R} g:{color.G} b:{color.B} a:{color.A})";
                case SDate date:
                    return translations.Stringify(date, withYear: false);
                case TimeSpan span:
                    {
                        List<string> parts = new List<string>();
                        if (span.Days > 0)
                            parts.Add(translations.Get(L10n.Generic.Days, new { count = span.Days }));
                        if (span.Hours > 0)
                            parts.Add(translations.Get(L10n.Generic.Hours, new { count = span.Hours }));
                        if (span.Minutes > 0)
                            parts.Add(translations.Get(L10n.Generic.Minutes, new { count = span.Minutes }));
                        return string.Join(", ", parts);
                    }
                case Vector2 vector:
                    return $"({vector.X}, {vector.Y})";
                case Rectangle rect:
                    return $"(x:{rect.X}, y:{rect.Y}, width:{rect.Width}, height:{rect.Height})";

                // game types
                case AnimatedSprite sprite:
                    return $"(textureName: {sprite.textureName.Value}, currentFrame:{sprite.currentFrame}, loop:{sprite.loop}, sourceRect:{translations.Stringify(sprite.sourceRect)})";
                case Stats stats:
                    {
                        StringBuilder str = new StringBuilder();
                        foreach (FieldInfo field in stats.GetType().GetFields())
                            str.AppendLine($"- {field.Name}: {translations.Stringify(field.GetValue(stats))}");
                        return str.ToString();
                    }

                // enumerable
                case IEnumerable array when !(value is string):
                    {
                        string[] values = (from val in array.Cast<object>() select translations.Stringify(val)).ToArray();
                        return "(" + string.Join(", ", values) + ")";
                    }

                default:
                    // key/value pair
                    {
                        Type type = value.GetType();
                        if (type.IsGenericType)
                        {
                            Type genericType = type.GetGenericTypeDefinition();
                            if (genericType == typeof(NetDictionary<,,,,>))
                            {
                                object dict = type.GetProperty("FieldDict").GetValue(value);
                                return translations.Stringify(dict);
                            }
                            if (genericType == typeof(KeyValuePair<,>))
                            {
                                string k = translations.Stringify(type.GetProperty(nameof(KeyValuePair<byte, byte>.Key)).GetValue(value));
                                string v = translations.Stringify(type.GetProperty(nameof(KeyValuePair<byte, byte>.Value)).GetValue(value));
                                return $"({k}: {v})";
                            }
                        }
                    }

                    // anything else
                    return value.ToString();
            }
        }
    }
}
