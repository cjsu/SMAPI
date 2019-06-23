using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Netcode;
using StardewModdingAPI.Framework.StateTracking.Comparers;

namespace StardewModdingAPI.Framework.StateTracking.FieldWatchers
{
    /// <summary>Provides convenience wrappers for creating watchers.</summary>
    internal static class WatcherFactory
    {
        /*********
        ** Public methods
        *********/
        /// <summary>Get a watcher which compares values using their <see cref="object.Equals(object)"/> method. This method should only be used when <see cref="ForEquatable{T}"/> won't work, since this doesn't validate whether they're comparable.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="getValue">Get the current value.</param>
        public static ComparableWatcher<T> ForGenericEquality<T>(Func<T> getValue) where T : struct
        {
            return new ComparableWatcher<T>(getValue, new GenericEqualsComparer<T>());
        }

        /// <summary>Get a watcher for an <see cref="IEquatable{T}"/> value.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="getValue">Get the current value.</param>
        public static ComparableWatcher<T> ForEquatable<T>(Func<T> getValue) where T : IEquatable<T>
        {
            return new ComparableWatcher<T>(getValue, new EquatableComparer<T>());
        }

        /// <summary>Get a watcher which detects when an object reference changes.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="getValue">Get the current value.</param>
        public static ComparableWatcher<T> ForReference<T>(Func<T> getValue)
        {
            return new ComparableWatcher<T>(getValue, new ObjectReferenceComparer<T>());
        }

        /// <summary>Get a watcher which detects when an object reference in a collection changes.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="collection">The observable collection.</param>
        public static ComparableListWatcher<T> ForReferenceList<T>(ICollection<T> collection)
        {
            return new ComparableListWatcher<T>(collection, new ObjectReferenceComparer<T>());
        }

        /// <summary>Get a watcher for an observable collection.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="collection">The observable collection.</param>
        public static ObservableCollectionWatcher<T> ForObservableCollection<T>(ObservableCollection<T> collection)
        {
            return new ObservableCollectionWatcher<T>(collection);
        }

        /// <summary>Get a watcher for a net collection.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <typeparam name="TSelf">The net field instance type.</typeparam>
        /// <param name="field">The net collection.</param>
        public static NetValueWatcher<T, TSelf> ForNetValue<T, TSelf>(NetFieldBase<T, TSelf> field) where TSelf : NetFieldBase<T, TSelf>
        {
            return new NetValueWatcher<T, TSelf>(field);
        }

        /// <summary>Get a watcher for a net collection.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="collection">The net collection.</param>
        public static NetCollectionWatcher<T> ForNetCollection<T>(NetCollection<T> collection) where T : class, INetObject<INetSerializable>
        {
            return new NetCollectionWatcher<T>(collection);
        }

        /// <summary>Get a watcher for a net dictionary.</summary>
        /// <typeparam name="TKey">The dictionary key type.</typeparam>
        /// <typeparam name="TValue">The dictionary value type.</typeparam>
        /// <typeparam name="TField">The net type equivalent to <typeparamref name="TValue"/>.</typeparam>
        /// <typeparam name="TSerialDict">The serializable dictionary type that can store the keys and values.</typeparam>
        /// <typeparam name="TSelf">The net field instance type.</typeparam>
        /// <param name="field">The net field.</param>
        public static NetDictionaryWatcher<TKey, TValue, TField, TSerialDict, TSelf> ForNetDictionary<TKey, TValue, TField, TSerialDict, TSelf>(NetDictionary<TKey, TValue, TField, TSerialDict, TSelf> field)
            where TField : class, INetObject<INetSerializable>, new()
            where TSerialDict : IDictionary<TKey, TValue>, new()
            where TSelf : NetDictionary<TKey, TValue, TField, TSerialDict, TSelf>
        {
            return new NetDictionaryWatcher<TKey, TValue, TField, TSerialDict, TSelf>(field);
        }
    }
}
