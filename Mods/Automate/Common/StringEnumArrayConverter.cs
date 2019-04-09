using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Pathoschild.Stardew.Common
{
    /// <summary>A variant of <see cref="StringEnumConverter"/> which represents arrays in JSON as a comma-delimited string.</summary>
    internal class StringEnumArrayConverter : StringEnumConverter
    {
        /*********
        ** Fields
        *********/
        /// <summary>Whether to return null values for missing data instead of an empty array.</summary>
        public bool AllowNull { get; set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Get whether this instance can convert the specified object type.</summary>
        /// <param name="type">The object type.</param>
        public override bool CanConvert(Type type)
        {
            if (!type.IsArray)
                return false;

            Type elementType = this.GetElementType(type);
            return elementType != null && base.CanConvert(elementType);
        }

        /// <summary>Read a JSON representation.</summary>
        /// <param name="reader">The JSON reader from which to read.</param>
        /// <param name="valueType">The value type.</param>
        /// <param name="rawValue">The raw value of the object being read.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override object ReadJson(JsonReader reader, Type valueType, object rawValue, JsonSerializer serializer)
        {
            // get element type
            Type elementType = this.GetElementType(valueType);
            if (elementType == null)
                throw new InvalidOperationException("Couldn't extract enum array element type."); // should never happen since we validate in CanConvert

            // parse
            switch (reader.TokenType)
            {
                case JsonToken.Null:
                    return this.GetNullOrEmptyArray(elementType);

                case JsonToken.StartArray:
                    {
                        string[] elements = JArray.Load(reader).Values<string>().ToArray();
                        object[] parsed = elements.Select(raw => this.ParseOne(raw, elementType)).ToArray();
                        return this.Cast(parsed, elementType);
                    }

                case JsonToken.String:
                    {
                        string value = (string)JToken.Load(reader);

                        if (string.IsNullOrWhiteSpace(value))
                            return this.GetNullOrEmptyArray(elementType);

                        object[] parsed = this.ParseMany(value, elementType).ToArray();
                        return this.Cast(parsed, elementType);
                    }

                default:
                    return base.ReadJson(reader, valueType, rawValue, serializer);
            }
        }

        /// <summary>Write a JSON representation.</summary>
        /// <param name="writer">The JSON writer to which to write.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteNull();
            else if (value is IEnumerable list)
            {
                string[] array = (from object element in list where element != null select element.ToString()).ToArray();
                writer.WriteValue(string.Join(", ", array));
            }
            else
                base.WriteJson(writer, value, serializer);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get the underlying array element type (bypassing <see cref="Nullable"/> if necessary).</summary>
        /// <param name="type">The array type.</param>
        private Type GetElementType(Type type)
        {
            if (!type.IsArray)
                return null;

            type = type.GetElementType();
            if (type == null)
                return null;

            type = Nullable.GetUnderlyingType(type) ?? type;

            return type;
        }

        /// <summary>Parse a string into individual values.</summary>
        /// <param name="input">The input string.</param>
        /// <param name="elementType">The enum type.</param>
        private IEnumerable<object> ParseMany(string input, Type elementType)
        {
            string[] values = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string value in values)
                yield return this.ParseOne(value, elementType);
        }

        /// <summary>Parse a string into one value.</summary>
        /// <param name="input">The input string.</param>
        /// <param name="elementType">The enum type.</param>
        private object ParseOne(string input, Type elementType)
        {
            return Enum.Parse(elementType, input, ignoreCase: true);
        }

        /// <summary>Get <c>null</c> or an empty array, depending on the value of <see cref="AllowNull"/>.</summary>
        /// <param name="elementType">The enum type.</param>
        private Array GetNullOrEmptyArray(Type elementType)
        {
            return this.AllowNull
                ? null
                : Array.CreateInstance(elementType, 0);
        }

        /// <summary>Create an array of elements with the given type.</summary>
        /// <param name="elements">The array elements.</param>
        /// <param name="elementType">The array element type.</param>
        private Array Cast(object[] elements, Type elementType)
        {
            if (elements == null)
                return null;

            Array result = Array.CreateInstance(elementType, elements.Length);
            Array.Copy(elements, result, result.Length);
            return result;
        }
    }
}
