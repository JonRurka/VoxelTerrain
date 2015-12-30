using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SafeDictionary<TKey, TValue> {
    private readonly object _padLock = new object();
    private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

    public TValue this[TKey key]
    {
        get
        {
            lock (_padLock)
                return _dictionary[key];
        }
        set
        {
            lock (_padLock)
                _dictionary[key] = value;
        }
    }

    public Dictionary<TKey, TValue>.KeyCollection Keys 
    {
        get
        {
            lock(_padLock)
                return _dictionary.Keys;
        }
    }

    public Dictionary<TKey, TValue>.ValueCollection Values
    {
        get
        {
            lock (_padLock)
                return _dictionary.Values;
        }
    }

    public int Count
    {
        get
        {
            lock (_padLock)
                return _dictionary.Count;
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_padLock)
        {
            return _dictionary.TryGetValue(key, out value);
        }
    }

    public void Clear()
    {
        lock (_padLock)
            _dictionary.Clear();
    }

    public bool ContainsKey(TKey key)
    {
        lock (_padLock)
            return _dictionary.ContainsKey(key);
    }

    public bool ContainsValue(TValue value)
    {
        lock (_padLock)
            return _dictionary.ContainsValue(value);
    }

    public void Remove(TKey key)
    {
        lock (_padLock)
            _dictionary.Remove(key);
    }

    public void Add(TKey key, TValue value)
    {
        lock (_padLock)
            _dictionary.Add(key, value);
    }

    public TValue[] GetValues(TKey[] keys) {
        lock (_padLock) {
            List<TValue> result = new List<TValue>();
            for (int i = 0; i < keys.Length; i++) {
                if (_dictionary.ContainsKey(keys[i]))
                    result.Add(_dictionary[keys[i]]);
            }
            return result.ToArray();
        }
    }
}
