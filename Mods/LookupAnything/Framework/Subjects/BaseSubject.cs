using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using Pathoschild.Stardew.LookupAnything.Framework.DebugFields;
using Pathoschild.Stardew.LookupAnything.Framework.Fields;
using StardewModdingAPI;

namespace Pathoschild.Stardew.LookupAnything.Framework.Subjects
{
    /// <summary>The base class for object metadata.</summary>
    internal abstract class BaseSubject : ISubject
    {
        /*********
        ** Fields
        *********/
        /// <summary>Provides translations stored in the mod folder.</summary>
        protected ITranslationHelper Text { get; }

        /// <summary>Provides utility methods for interacting with the game code.</summary>
        protected GameHelper GameHelper { get; }


        /*********
        ** Accessors
        *********/
        /// <summary>The display name.</summary>
        public string Name { get; protected set; }

        /// <summary>The object description (if applicable).</summary>
        public string Description { get; protected set; }

        /// <summary>The object type.</summary>
        public string Type { get; protected set; }


        /*********
        ** Public methods
        *********/
        /// <summary>Get the data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public abstract IEnumerable<ICustomField> GetData(Metadata metadata);

        /// <summary>Get raw debug data to display for this subject.</summary>
        /// <param name="metadata">Provides metadata that's not available from the game data directly.</param>
        public abstract IEnumerable<IDebugField> GetDebugFields(Metadata metadata);

        /// <summary>Draw the subject portrait (if available).</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="position">The position at which to draw.</param>
        /// <param name="size">The size of the portrait to draw.</param>
        /// <returns>Returns <c>true</c> if a portrait was drawn, else <c>false</c>.</returns>
        public abstract bool DrawPortrait(SpriteBatch spriteBatch, Vector2 position, Vector2 size);


        /*********
        ** Protected methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        protected BaseSubject(GameHelper gameHelper, ITranslationHelper translations)
        {
            this.GameHelper = gameHelper;
            this.Text = translations;
        }

        /// <summary>Construct an instance.</summary>
        /// <param name="gameHelper">Provides utility methods for interacting with the game code.</param>
        /// <param name="name">The display name.</param>
        /// <param name="description">The object description (if applicable).</param>
        /// <param name="type">The object type.</param>
        /// <param name="translations">Provides translations stored in the mod folder.</param>
        protected BaseSubject(GameHelper gameHelper, string name, string description, string type, ITranslationHelper translations)
            : this(gameHelper, translations)
        {
            this.Initialise(name, description, type);
        }

        /// <summary>Initialise the base values.</summary>
        /// <param name="name">The display name.</param>
        /// <param name="description">The object description (if applicable).</param>
        /// <param name="type">The object type.</param>
        protected void Initialise(string name, string description, string type)
        {
            this.Name = name;
            this.Description = description;
            this.Type = type;
        }

        /// <summary>Get all debug fields by reflecting over an instance.</summary>
        /// <param name="obj">The object instance over which to reflect.</param>
        protected IEnumerable<IDebugField> GetDebugFieldsFrom(object obj)
        {
            if (obj == null)
                yield break;

            for (Type type = obj.GetType(); type != null; type = type.BaseType)
            {
                // get fields & properties
                var fields =
                    (
                        from field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                        where !field.IsLiteral // exclude constants
                        select new { field.Name, Type = field.FieldType, Value = this.GetDebugValue(obj, field) }
                    )
                    .Concat(
                        from property in type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                        where property.CanRead
                        select new { property.Name, Type = property.PropertyType, Value = this.GetDebugValue(obj, property) }
                    )
                    .OrderBy(field => field.Name, StringComparer.InvariantCultureIgnoreCase);

                // yield valid values
                IDictionary<string, string> seenValues = new Dictionary<string, string>(StringComparer.InvariantCulture);
                foreach (var field in fields)
                {
                    if (seenValues.TryGetValue(field.Name, out string value) && value == field.Value)
                        continue; // key/value pair differs only in the key case
                    if (field.Value == field.Type.ToString())
                        continue; // can't be displayed

                    yield return new GenericDebugField($"{type.Name}::{field.Name}", field.Value);
                }
            }
        }

        /// <summary>Get a human-readable representation of a value.</summary>
        /// <param name="value">The underlying value.</param>
        protected string Stringify(object value)
        {
            return this.Text.Stringify(value);
        }

        /// <summary>Get a translation for the current locale.</summary>
        /// <param name="key">The translation key.</param>
        /// <param name="tokens">An anonymous object containing token key/value pairs, like <c>new { value = 42, name = "Cranberries" }</c>.</param>
        /// <exception cref="KeyNotFoundException">The <paramref name="key" /> doesn't match an available translation.</exception>
        protected Translation Translate(string key, object tokens = null)
        {
            return this.Text.Get(key, tokens);
        }

        /// <summary>Get a human-readable value for a debug value.</summary>
        /// <param name="obj">The object whose values to read.</param>
        /// <param name="field">The field to read.</param>
        private string GetDebugValue(object obj, FieldInfo field)
        {
            try
            {
                return this.Stringify(field.GetValue(obj));
            }
            catch (Exception ex)
            {
                return $"error reading field: {ex.Message}";
            }
        }

        /// <summary>Get a human-readable value for a debug value.</summary>
        /// <param name="obj">The object whose values to read.</param>
        /// <param name="property">The property to read.</param>
        private string GetDebugValue(object obj, PropertyInfo property)
        {
            try
            {
                return this.Stringify(property.GetValue(obj));
            }
            catch (Exception ex)
            {
                return $"error reading property: {ex.Message}";
            }
        }
    }
}
