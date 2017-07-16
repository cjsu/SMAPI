using System;

namespace StardewModdingAPI
{
    /// <summary>Provides an API for accessing private game code.</summary>
    public interface IReflectionHelper : IModLinked
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get a private instance field.</summary>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="obj">The object which has the field.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        IPrivateField<TValue> GetPrivateField<TValue>(object obj, string name, bool required = true);

        /// <summary>Get a private static field.</summary>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="type">The type which has the field.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        IPrivateField<TValue> GetPrivateField<TValue>(Type type, string name, bool required = true);

        /// <summary>Get a private instance property.</summary>
        /// <typeparam name="TValue">The property type.</typeparam>
        /// <param name="obj">The object which has the property.</param>
        /// <param name="name">The property name.</param>
        /// <param name="required">Whether to throw an exception if the private property is not found.</param>
        IPrivateProperty<TValue> GetPrivateProperty<TValue>(object obj, string name, bool required = true);

        /// <summary>Get a private static property.</summary>
        /// <typeparam name="TValue">The property type.</typeparam>
        /// <param name="type">The type which has the property.</param>
        /// <param name="name">The property name.</param>
        /// <param name="required">Whether to throw an exception if the private property is not found.</param>
        IPrivateProperty<TValue> GetPrivateProperty<TValue>(Type type, string name, bool required = true);

        /// <summary>Get the value of a private instance field.</summary>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="obj">The object which has the field.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        /// <remarks>This is a shortcut for <see cref="GetPrivateField{TValue}(object,string,bool)"/> followed by <see cref="IPrivateField{TValue}.GetValue"/>.</remarks>
        TValue GetPrivateValue<TValue>(object obj, string name, bool required = true);

        /// <summary>Get the value of a private static field.</summary>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="type">The type which has the field.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        /// <remarks>This is a shortcut for <see cref="GetPrivateField{TValue}(Type,string,bool)"/> followed by <see cref="IPrivateField{TValue}.GetValue"/>.</remarks>
        TValue GetPrivateValue<TValue>(Type type, string name, bool required = true);

        /// <summary>Get a private instance method.</summary>
        /// <param name="obj">The object which has the method.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        IPrivateMethod GetPrivateMethod(object obj, string name, bool required = true);

        /// <summary>Get a private static method.</summary>
        /// <param name="type">The type which has the method.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        IPrivateMethod GetPrivateMethod(Type type, string name, bool required = true);
    }
}
