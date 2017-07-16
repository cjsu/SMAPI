using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;

namespace StardewModdingAPI.Framework.Reflection
{
    /// <summary>Provides helper methods for accessing private game code.</summary>
    /// <remarks>This implementation searches up the type hierarchy, and caches the reflected fields and methods with a sliding expiry (to optimise performance without unnecessary memory usage).</remarks>
    internal class Reflector
    {
        /*********
        ** Properties
        *********/
        /// <summary>The cached fields and methods found via reflection.</summary>
        private readonly MemoryCache Cache = new MemoryCache(typeof(Reflector).FullName);

        /// <summary>The sliding cache expiration time.</summary>
        private readonly TimeSpan SlidingCacheExpiry = TimeSpan.FromMinutes(5);


        /*********
        ** Public methods
        *********/
        /****
        ** Fields
        ****/
        /// <summary>Get a private instance field.</summary>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="obj">The object which has the field.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        /// <returns>Returns the field wrapper, or <c>null</c> if the field doesn't exist and <paramref name="required"/> is <c>false</c>.</returns>
        public IPrivateField<TValue> GetPrivateField<TValue>(object obj, string name, bool required = true)
        {
            // validate
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Can't get a private instance field from a null object.");

            // get field from hierarchy
            IPrivateField<TValue> field = this.GetFieldFromHierarchy<TValue>(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (required && field == null)
                throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a private '{name}' instance field.");
            return field;
        }

        /// <summary>Get a private static field.</summary>
        /// <typeparam name="TValue">The field type.</typeparam>
        /// <param name="type">The type which has the field.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        public IPrivateField<TValue> GetPrivateField<TValue>(Type type, string name, bool required = true)
        {
            // get field from hierarchy
            IPrivateField<TValue> field = this.GetFieldFromHierarchy<TValue>(type, null, name, BindingFlags.NonPublic | BindingFlags.Static);
            if (required && field == null)
                throw new InvalidOperationException($"The {type.FullName} object doesn't have a private '{name}' static field.");
            return field;
        }

        /****
        ** Properties
        ****/
        /// <summary>Get a private instance property.</summary>
        /// <typeparam name="TValue">The property type.</typeparam>
        /// <param name="obj">The object which has the property.</param>
        /// <param name="name">The property name.</param>
        /// <param name="required">Whether to throw an exception if the private property is not found.</param>
        public IPrivateProperty<TValue> GetPrivateProperty<TValue>(object obj, string name, bool required = true)
        {
            // validate
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Can't get a private instance property from a null object.");

            // get property from hierarchy
            IPrivateProperty<TValue> property = this.GetPropertyFromHierarchy<TValue>(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (required && property == null)
                throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a private '{name}' instance property.");
            return property;
        }

        /// <summary>Get a private static property.</summary>
        /// <typeparam name="TValue">The property type.</typeparam>
        /// <param name="type">The type which has the property.</param>
        /// <param name="name">The property name.</param>
        /// <param name="required">Whether to throw an exception if the private property is not found.</param>
        public IPrivateProperty<TValue> GetPrivateProperty<TValue>(Type type, string name, bool required = true)
        {
            // get field from hierarchy
            IPrivateProperty<TValue> property = this.GetPropertyFromHierarchy<TValue>(type, null, name, BindingFlags.NonPublic | BindingFlags.Static);
            if (required && property == null)
                throw new InvalidOperationException($"The {type.FullName} object doesn't have a private '{name}' static property.");
            return property;
        }

        /****
        ** Methods
        ****/
        /// <summary>Get a private instance method.</summary>
        /// <param name="obj">The object which has the method.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        public IPrivateMethod GetPrivateMethod(object obj, string name, bool required = true)
        {
            // validate
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Can't get a private instance method from a null object.");

            // get method from hierarchy
            IPrivateMethod method = this.GetMethodFromHierarchy(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (required && method == null)
                throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a private '{name}' instance method.");
            return method;
        }

        /// <summary>Get a private static method.</summary>
        /// <param name="type">The type which has the method.</param>
        /// <param name="name">The field name.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        public IPrivateMethod GetPrivateMethod(Type type, string name, bool required = true)
        {
            // get method from hierarchy
            IPrivateMethod method = this.GetMethodFromHierarchy(type, null, name, BindingFlags.NonPublic | BindingFlags.Static);
            if (required && method == null)
                throw new InvalidOperationException($"The {type.FullName} object doesn't have a private '{name}' static method.");
            return method;
        }

        /****
        ** Methods by signature
        ****/
        /// <summary>Get a private instance method.</summary>
        /// <param name="obj">The object which has the method.</param>
        /// <param name="name">The field name.</param>
        /// <param name="argumentTypes">The argument types of the method signature to find.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        public IPrivateMethod GetPrivateMethod(object obj, string name, Type[] argumentTypes, bool required = true)
        {
            // validate parent
            if (obj == null)
                throw new ArgumentNullException(nameof(obj), "Can't get a private instance method from a null object.");

            // get method from hierarchy
            PrivateMethod method = this.GetMethodFromHierarchy(obj.GetType(), obj, name, BindingFlags.Instance | BindingFlags.NonPublic, argumentTypes);
            if (required && method == null)
                throw new InvalidOperationException($"The {obj.GetType().FullName} object doesn't have a private '{name}' instance method with that signature.");
            return method;
        }

        /// <summary>Get a private static method.</summary>
        /// <param name="type">The type which has the method.</param>
        /// <param name="name">The field name.</param>
        /// <param name="argumentTypes">The argument types of the method signature to find.</param>
        /// <param name="required">Whether to throw an exception if the private field is not found.</param>
        public IPrivateMethod GetPrivateMethod(Type type, string name, Type[] argumentTypes, bool required = true)
        {
            // get field from hierarchy
            PrivateMethod method = this.GetMethodFromHierarchy(type, null, name, BindingFlags.NonPublic | BindingFlags.Static, argumentTypes);
            if (required && method == null)
                throw new InvalidOperationException($"The {type.FullName} object doesn't have a private '{name}' static method with that signature.");
            return method;
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get a field from the type hierarchy.</summary>
        /// <typeparam name="TValue">The expected field type.</typeparam>
        /// <param name="type">The type which has the field.</param>
        /// <param name="obj">The object which has the field.</param>
        /// <param name="name">The field name.</param>
        /// <param name="bindingFlags">The reflection binding which flags which indicates what type of field to find.</param>
        private IPrivateField<TValue> GetFieldFromHierarchy<TValue>(Type type, object obj, string name, BindingFlags bindingFlags)
        {
            bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
            FieldInfo field = this.GetCached<FieldInfo>($"field::{isStatic}::{type.FullName}::{name}", () =>
            {
                FieldInfo fieldInfo = null;
                for (; type != null && fieldInfo == null; type = type.BaseType)
                    fieldInfo = type.GetField(name, bindingFlags);
                return fieldInfo;
            });

            return field != null
                ? new PrivateField<TValue>(type, obj, field, isStatic)
                : null;
        }

        /// <summary>Get a property from the type hierarchy.</summary>
        /// <typeparam name="TValue">The expected property type.</typeparam>
        /// <param name="type">The type which has the property.</param>
        /// <param name="obj">The object which has the property.</param>
        /// <param name="name">The property name.</param>
        /// <param name="bindingFlags">The reflection binding which flags which indicates what type of property to find.</param>
        private IPrivateProperty<TValue> GetPropertyFromHierarchy<TValue>(Type type, object obj, string name, BindingFlags bindingFlags)
        {
            bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
            PropertyInfo property = this.GetCached<PropertyInfo>($"property::{isStatic}::{type.FullName}::{name}", () =>
            {
                PropertyInfo propertyInfo = null;
                for (; type != null && propertyInfo == null; type = type.BaseType)
                    propertyInfo = type.GetProperty(name, bindingFlags);
                return propertyInfo;
            });

            return property != null
                ? new PrivateProperty<TValue>(type, obj, property, isStatic)
                : null;
        }

        /// <summary>Get a method from the type hierarchy.</summary>
        /// <param name="type">The type which has the method.</param>
        /// <param name="obj">The object which has the method.</param>
        /// <param name="name">The method name.</param>
        /// <param name="bindingFlags">The reflection binding which flags which indicates what type of method to find.</param>
        private IPrivateMethod GetMethodFromHierarchy(Type type, object obj, string name, BindingFlags bindingFlags)
        {
            bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
            MethodInfo method = this.GetCached($"method::{isStatic}::{type.FullName}::{name}", () =>
            {
                MethodInfo methodInfo = null;
                for (; type != null && methodInfo == null; type = type.BaseType)
                    methodInfo = type.GetMethod(name, bindingFlags);
                return methodInfo;
            });

            return method != null
                ? new PrivateMethod(type, obj, method, isStatic: bindingFlags.HasFlag(BindingFlags.Static))
                : null;
        }

        /// <summary>Get a method from the type hierarchy.</summary>
        /// <param name="type">The type which has the method.</param>
        /// <param name="obj">The object which has the method.</param>
        /// <param name="name">The method name.</param>
        /// <param name="bindingFlags">The reflection binding which flags which indicates what type of method to find.</param>
        /// <param name="argumentTypes">The argument types of the method signature to find.</param>
        private PrivateMethod GetMethodFromHierarchy(Type type, object obj, string name, BindingFlags bindingFlags, Type[] argumentTypes)
        {
            bool isStatic = bindingFlags.HasFlag(BindingFlags.Static);
            MethodInfo method = this.GetCached($"method::{isStatic}::{type.FullName}::{name}({string.Join(",", argumentTypes.Select(p => p.FullName))})", () =>
            {
                MethodInfo methodInfo = null;
                for (; type != null && methodInfo == null; type = type.BaseType)
                    methodInfo = type.GetMethod(name, bindingFlags, null, argumentTypes, null);
                return methodInfo;
            });
            return method != null
                ? new PrivateMethod(type, obj, method, isStatic)
                : null;
        }

        /// <summary>Get a method or field through the cache.</summary>
        /// <typeparam name="TMemberInfo">The expected <see cref="MemberInfo"/> type.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="fetch">Fetches a new value to cache.</param>
        private TMemberInfo GetCached<TMemberInfo>(string key, Func<TMemberInfo> fetch) where TMemberInfo : MemberInfo
        {
            // get from cache
            if (this.Cache.Contains(key))
            {
                CacheEntry entry = (CacheEntry)this.Cache[key];
                return entry.IsValid
                    ? (TMemberInfo)entry.MemberInfo
                    : default(TMemberInfo);
            }

            // fetch & cache new value
            TMemberInfo result = fetch();
            CacheEntry cacheEntry = new CacheEntry(result != null, result);
            this.Cache.Add(key, cacheEntry, new CacheItemPolicy { SlidingExpiration = this.SlidingCacheExpiry });
            return result;
        }
    }
}
