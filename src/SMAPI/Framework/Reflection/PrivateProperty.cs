using System;
using System.Reflection;

namespace StardewModdingAPI.Framework.Reflection
{
    /// <summary>A private property obtained through reflection.</summary>
    /// <typeparam name="TValue">The property value type.</typeparam>
    internal class PrivateProperty<TValue> : IPrivateProperty<TValue>
    {
        /*********
        ** Properties
        *********/
        /// <summary>The type that has the field.</summary>
        private readonly Type ParentType;

        /// <summary>The object that has the instance field (if applicable).</summary>
        private readonly object Parent;

        /// <summary>The display name shown in error messages.</summary>
        private string DisplayName => $"{this.ParentType.FullName}::{this.PropertyInfo.Name}";

        private readonly Func<object, TValue> GetterDelegate;
        private readonly Action<object, TValue> SetterDelegate;


        /*********
        ** Accessors
        *********/
        /// <summary>The reflection metadata.</summary>
        public PropertyInfo PropertyInfo { get; }


        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="parentType">The type that has the field.</param>
        /// <param name="obj">The object that has the instance field (if applicable).</param>
        /// <param name="property">The reflection metadata.</param>
        /// <param name="isStatic">Whether the field is static.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="parentType"/> or <paramref name="property"/> is null.</exception>
        /// <exception cref="ArgumentException">The <paramref name="obj"/> is null for a non-static field, or not null for a static field.</exception>
        public PrivateProperty(Type parentType, object obj, PropertyInfo property, bool isStatic)
        {
            if (isStatic && obj != null)
                throw new ArgumentException("A static property cannot have an object instance.");
            if (!isStatic && obj == null)
                throw new ArgumentException("A non-static property must have an object instance.");

            this.ParentType = parentType ?? throw new ArgumentNullException(nameof(parentType));
            this.Parent = obj;
            this.PropertyInfo = property ?? throw new ArgumentNullException(nameof(property));

            Type[] types = new Type[] { this.PropertyInfo.DeclaringType, typeof(TValue)};

            this.GetterDelegate = (Func<object, TValue>)Delegate.CreateDelegate(typeof(Func<,>).MakeGenericType(types), this.PropertyInfo.GetMethod);
            this.SetterDelegate = (Action<object, TValue>)Delegate.CreateDelegate(typeof(Action<,>).MakeGenericType(types), this.PropertyInfo.SetMethod);
        }

        /// <summary>Get the property value.</summary>
        public TValue GetValue()
        {
            try
            {
                return this.GetterDelegate(this.Parent);
                // Old version: Commented out in case of issues with new version
                //return (TValue)this.PropertyInfo.GetValue(this.Parent);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Can't convert the private {this.DisplayName} property from {this.PropertyInfo.PropertyType.FullName} to {typeof(TValue).FullName}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't get the value of the private {this.DisplayName} property", ex);
            }
        }

        /// <summary>Set the property value.</summary>
        //// <param name="value">The value to set.</param>
        public void SetValue(TValue value)
        {
            try
            {
                this.SetterDelegate(this.Parent, value);
                // Old version: Commented out in case of issues with new version
                //this.PropertyInfo.SetValue(this.Parent, value);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Can't assign the private {this.DisplayName} property a {typeof(TValue).FullName} value, must be compatible with {this.PropertyInfo.PropertyType.FullName}.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Couldn't set the value of the private {this.DisplayName} property", ex);
            }
        }
    }
}
