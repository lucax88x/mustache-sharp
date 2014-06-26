﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Mustache
{
    internal static class UpcastDictionary
    {
        public static IDictionary<string, object> Create(object source)
        {
            if (source == null)
            {
                return null;
            }
            IDictionary<string, object> sourceDictionary = source as IDictionary<string, object>;
            if (sourceDictionary != null)
            {
                return sourceDictionary;
            }
            Type sourceType = source.GetType();
            var types = getTypes(sourceType);
            return getDictionary(types, source);
        }

        private static IEnumerable<Type> getTypes(Type sourceType)
        {
            Queue<Type> pending = new Queue<Type>();
            HashSet<Type> visited = new HashSet<Type>();
            pending.Enqueue(sourceType);
            foreach (Type type in getTypes(pending, visited))
            {
                yield return type;
            }
        }

        private static IEnumerable<Type> getTypes(Queue<Type> pending, HashSet<Type> visited)
        {
            if (pending.Count == 0)
            {
                yield break;
            }

            Type sourceType = pending.Dequeue();
            visited.Add(sourceType);
            yield return sourceType;

            if (sourceType.BaseType != null)
            {
                if (!visited.Contains(sourceType.BaseType))
                {
                    pending.Enqueue(sourceType.BaseType);
                }
            }

            foreach (Type interfaceType in sourceType.GetInterfaces())
            {
                if (!visited.Contains(interfaceType))
                {
                    pending.Enqueue(interfaceType);
                }
            }

            foreach (Type type in getTypes(pending, visited))
            {
                yield return type;
            }
        }

        private static IDictionary<string, object> getDictionary(IEnumerable<Type> types, object source)
        {
            var dictionaries = from type in types
                               let valueType = getValueType(type)
                               where valueType != null
                               let upcastType = typeof(UpcastDictionary<>).MakeGenericType(valueType)
                               select (IDictionary<string, object>)Activator.CreateInstance(upcastType, source);
            return dictionaries.FirstOrDefault();
        }

        private static Type getValueType(Type type)
        {
            if (!type.IsGenericType)
            {
                return null;
            }
            Type[] argumentTypes = type.GetGenericArguments();
            if (argumentTypes.Length != 2)
            {
                return null;
            }
            Type keyType = argumentTypes[0];
            if (keyType != typeof(string))
            {
                return null;
            }
            Type valueType = argumentTypes[1];
            Type genericType = typeof(IDictionary<,>).MakeGenericType(typeof(string), valueType);
            if (!genericType.IsAssignableFrom(type))
            {
                return null;
            }
            return valueType;
        }
    }

    internal class UpcastDictionary<TValue> : IDictionary<string, object>
    {
        private readonly IDictionary<string, TValue> dictionary;

        public UpcastDictionary(IDictionary<string, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void IDictionary<string, object>.Add(string key, object value)
        {
            throw new NotSupportedException();
        }

        public bool ContainsKey(string key)
        {
            return dictionary.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return dictionary.Keys; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool IDictionary<string, object>.Remove(string key)
        {
            throw new NotSupportedException();
        }

        public bool TryGetValue(string key, out object value)
        {
            TValue result;
            if (dictionary.TryGetValue(key, out result))
            {
                value = result;
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public ICollection<object> Values
        {
            get { return dictionary.Values.Cast<object>().ToArray(); }
        }

        public object this[string key]
        {
            get
            {
                return dictionary[key];
            }
            [EditorBrowsable(EditorBrowsableState.Never)]
            set
            {
                throw new NotSupportedException();
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            if (!(item.Value is TValue))
            {
                return false;
            }
            KeyValuePair<string, TValue> pair = new KeyValuePair<string,TValue>(item.Key, (TValue)item.Value);
            ICollection<KeyValuePair<string, TValue>> collection = dictionary;
            return dictionary.Contains(pair);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            var pairs = dictionary.Select(p => new KeyValuePair<string, object>(p.Key, p.Value)).ToArray();
            pairs.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return true; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return dictionary.Select(p => new KeyValuePair<string, object>(p.Key, p.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
