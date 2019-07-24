using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace StardewModdingAPI.Web.Framework.Caching
{
    /// <summary>Serialises <see cref="DateTimeOffset"/> to a UTC date field instead of the default array.</summary>
    public class UtcDateTimeOffsetSerializer : StructSerializerBase<DateTimeOffset>
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying date serializer.</summary>
        private static readonly DateTimeSerializer DateTimeSerializer = new DateTimeSerializer(DateTimeKind.Utc, BsonType.DateTime);


        /*********
        ** Public methods
        *********/
        /// <summary>Deserializes a value.</summary>
        /// <param name="context">The deserialization context.</param>
        /// <param name="args">The deserialization args.</param>
        /// <returns>A deserialized value.</returns>
        public override DateTimeOffset Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            DateTime date = UtcDateTimeOffsetSerializer.DateTimeSerializer.Deserialize(context, args);
            return new DateTimeOffset(date, TimeSpan.Zero);
        }

        /// <summary>Serializes a value.</summary>
        /// <param name="context">The serialization context.</param>
        /// <param name="args">The serialization args.</param>
        /// <param name="value">The object.</param>
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateTimeOffset value)
        {
            UtcDateTimeOffsetSerializer.DateTimeSerializer.Serialize(context, args, value.UtcDateTime);
        }
    }
}
