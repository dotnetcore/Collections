using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.Collections.Multi
{
    public class MultiList<T>
    {
        private readonly List<int> _hashcodeList;
        private readonly Dictionary<int, Entry> _entries;
        private readonly object _lockObj = new object();

        public MultiList()
        {
            _hashcodeList = new List<int>();
            _entries = new Dictionary<int, Entry>();
        }

        public void Add(T element)
        {
            Add(element, 1);
        }

        public void Add(T element, int times)
        {
            var hashcode = GetValueHashCode(element);
            lock (_lockObj)
            {
                var safeTimes = FixTimes(times);
                if (_hashcodeList.Contains(hashcode))
                {
                    _entries[hashcode].Count += safeTimes;
                }
                else
                {
                    _hashcodeList.Add(hashcode);
                    _entries.Add(hashcode, new Entry(element, safeTimes));
                }
            }
        }

        public bool Contains(T value)
        {
            var hashcode = GetValueHashCode(value);
            return _hashcodeList.Contains(hashcode);
        }

        public bool ContainsAll(IEnumerable<T> elements)
        {
            if (elements == null)
                return false;
            foreach (var item in elements)
                if (!Contains(item))
                    return false;
            return true;
        }

        public int Count(T element)
        {
            var hashcode = GetValueHashCode(element);
            lock (_hashcodeList)
            {
                if (!_hashcodeList.Contains(hashcode))
                    return 0;
                return _entries[hashcode].Count;
            }
        }

        public int SetCount(T element, int times)
        {
            var hashcode = GetValueHashCode(element);
            var result = 0;
            lock (_lockObj)
            {
                var safeTimes = FixTimes(times);
                if (_hashcodeList.Contains(hashcode))
                {
                    if (safeTimes > 0)
                    {
                        var entry = _entries[hashcode];
                        entry.Count = safeTimes;
                    }
                    else
                    {
                        _hashcodeList.Remove(hashcode);
                        _entries.Remove(hashcode);
                    }
                }
            }

            return result;
        }

        public List<T> ToList()
        {
            lock (_hashcodeList)
            {
                return _entries.Values.Select(x => x.Value).ToList();
            }
        }

        public List<Entry> EntrySet()
        {
            lock (_hashcodeList)
            {
                return _entries.Values.ToList();
            }
        }

        public bool Remove(T element)
        {
            var hashcode = GetValueHashCode(element);
            lock (_hashcodeList)
            {
                if (_hashcodeList.Contains(hashcode))
                {
                    return _hashcodeList.Remove(hashcode) && _entries.Remove(hashcode);
                }
            }

            return true;
        }

        public int Remove(T element, int times)
        {
            var hashcode = GetValueHashCode(element);
            var result = 0;
            lock (_lockObj)
            {
                var safeTimes = FixTimes(times);
                if (_hashcodeList.Contains(hashcode))
                {
                    var entry = _entries[hashcode];
                    var temp = entry.Count - safeTimes;
                    if (temp > 0)
                    {
                        entry.Count = temp;
                        result = temp;
                    }
                    else
                    {
                        _hashcodeList.Remove(hashcode);
                        _entries.Remove(hashcode);
                    }
                }
            }

            return result;
        }

        public bool RemoveAll(IEnumerable<T> collection)
        {
            if (collection == null)
                return true;
            var hashcodeList = collection.Select(GetValueHashCode).ToList();
            lock (_lockObj)
            {
                foreach (var hashcode in hashcodeList)
                {
                    if (_hashcodeList.Contains(hashcode))
                    {
                        _hashcodeList.Remove(hashcode);
                        _entries.Remove(hashcode);
                    }
                }
            }

            return true;
        }

        public class Entry
        {
            public Entry(T value, int times = 1)
            {
                Value = value;
                Count = times;
            }

            public int Count { get; internal set; }

            public T Value { get; }
        }

        private int GetValueHashCode(T value)
        {
            return value == null ? 0 : value.GetHashCode();
        }

        private int FixTimes(int uncheckedTimes)
        {
            return uncheckedTimes <= 0 ? 1 : uncheckedTimes;
        }

        public override string ToString()
        {
            return string.Join(",", ToList());
        }
    }
}
