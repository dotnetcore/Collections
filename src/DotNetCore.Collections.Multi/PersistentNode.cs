using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// A cons cell in the immutable list of copies held under one key.
    /// </summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// A linked list rather than an array: appending one copy then costs O(1) instead of copying the
    /// whole array, so adding <c>m</c> copies under a single key is O(<c>m</c>) rather than
    /// O(<c>m</c><sup>2</sup>) - the same quadratic shape this type family exists to remove, only one
    /// level down. <see cref="PersistentMultiDictionary{TKey,TValue}.ValueCount"/> and the value
    /// materialisers walk the cells once and reverse, which restores insertion order.
    /// </remarks>
    internal sealed class PersistentValueCell<TValue>
    {
        internal PersistentValueCell(TValue value, PersistentValueCell<TValue>? next)
        {
            Value = value;
            Next = next;
            Count = next == null ? 1 : next.Count + 1;
        }

        /// <summary>Gets the value held by this cell.</summary>
        internal TValue Value { get; }

        /// <summary>Gets the rest of the list, or <c>null</c> at the end.</summary>
        internal PersistentValueCell<TValue>? Next { get; }

        /// <summary>Gets the number of cells from here to the end.</summary>
        internal int Count { get; }
    }

    /// <summary>
    /// One key together with every copy stored under it.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    internal readonly struct PersistentEntry<TKey, TValue>
    {
        internal PersistentEntry(int hash, TKey key, PersistentValueCell<TValue>? values)
        {
            Hash = hash;
            Key = key;
            Values = values;
        }

        /// <summary>Gets the masked hash that placed this entry in its slot.</summary>
        internal int Hash { get; }

        /// <summary>Gets the key.</summary>
        internal TKey Key { get; }

        /// <summary>Gets the copies, or <c>null</c> when the entry holds none.</summary>
        internal PersistentValueCell<TValue>? Values { get; }

        /// <summary>Gets the number of copies.</summary>
        internal int ValueCount => Values == null ? 0 : Values.Count;

        /// <summary>Returns a copy of this entry with a different copy list.</summary>
        internal PersistentEntry<TKey, TValue> WithValues(PersistentValueCell<TValue>? values)
        {
            return new PersistentEntry<TKey, TValue>(Hash, Key, values);
        }
    }

    /// <summary>
    /// A node in the compressed hash-array mapped trie behind
    /// <see cref="PersistentMultiDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// <para>
    /// Two node shapes exist. A <see cref="BitmapNode{TKey,TValue}"/> owns a 32-way slot space split
    /// in two bitmaps: slots that hold a single entry inline, and slots that hold a sub-trie. That
    /// split is what keeps a sparsely populated level from allocating thirty-two pointers per node.
    /// A <see cref="CollisionNode{TKey,TValue}"/> holds the entries whose masked hashes are identical,
    /// which is the only case a trie cannot separate.
    /// </para>
    /// <para>
    /// Every mutator returns a new node and leaves the receiver untouched; untouched children are
    /// shared rather than copied, so a write copies one path of at most seven levels instead of the
    /// whole collection. That sharing is the entire point of this structure and is what
    /// <c>PersistentMultiDictionary</c> trades its <c>Immutable*</c> predecessor's full clone for.
    /// </para>
    /// <para>
    /// A mutator returns <c>null</c> to mean "this node is now empty", which is how a parent learns to
    /// drop the corresponding slot.
    /// </para>
    /// </remarks>
    internal abstract class PersistentNode<TKey, TValue>
    {
        /// <summary>Adds one copy of a value under a key.</summary>
        /// <param name="shift">The bit offset of the slot space this node owns.</param>
        /// <param name="hash">The masked hash of the key.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="allowDuplicateValues">
        /// Whether a value already present under this key may be stored again.
        /// </param>
        /// <param name="changed">
        /// Set to <c>true</c> when the collection actually changed; <c>false</c> means the value was
        /// rejected as a duplicate and the caller should keep the instance it already holds.
        /// </param>
        /// <returns>The replacement node, or the receiver when nothing changed.</returns>
        internal abstract PersistentNode<TKey, TValue> Add(
            int shift,
            int hash,
            TKey key,
            TValue value,
            IEqualityComparer<TKey> keyComparer,
            bool allowDuplicateValues,
            out bool changed);

        /// <summary>Removes one copy of a value from under a key.</summary>
        /// <param name="shift">The bit offset of the slot space this node owns.</param>
        /// <param name="hash">The masked hash of the key.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to remove one copy of.</param>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="removed">Set to <c>true</c> when a copy was removed.</param>
        /// <returns>The replacement node, <c>null</c> when empty, or the receiver when nothing changed.</returns>
        internal abstract PersistentNode<TKey, TValue>? RemoveValue(
            int shift,
            int hash,
            TKey key,
            TValue value,
            IEqualityComparer<TKey> keyComparer,
            out bool removed);

        /// <summary>Removes every copy held under a key.</summary>
        /// <param name="shift">The bit offset of the slot space this node owns.</param>
        /// <param name="hash">The masked hash of the key.</param>
        /// <param name="key">The key.</param>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="removedValues">Set to the number of copies removed.</param>
        /// <returns>The replacement node, <c>null</c> when empty, or the receiver when nothing changed.</returns>
        internal abstract PersistentNode<TKey, TValue>? RemoveKey(
            int shift,
            int hash,
            TKey key,
            IEqualityComparer<TKey> keyComparer,
            out int removedValues);

        /// <summary>Locates the entry stored under a key.</summary>
        /// <param name="hash">The masked hash of the key.</param>
        /// <param name="key">The key.</param>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="entry">Receives the entry when found.</param>
        /// <returns><c>true</c> when the key is present.</returns>
        internal abstract bool TryGetEntry(
            int hash,
            TKey key,
            IEqualityComparer<TKey> keyComparer,
            out PersistentEntry<TKey, TValue> entry);

        /// <summary>Appends every entry held below this node to a sink.</summary>
        /// <param name="sink">The list to append to.</param>
        internal abstract void CollectEntries(List<PersistentEntry<TKey, TValue>> sink);
    }

    /// <summary>
    /// A node that owns a 32-way slot space, split into inline entries and sub-tries.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    internal sealed class BitmapNode<TKey, TValue> : PersistentNode<TKey, TValue>
    {
        private static readonly PersistentEntry<TKey, TValue>[] NoEntries = new PersistentEntry<TKey, TValue>[0];
        private static readonly PersistentNode<TKey, TValue>[] NoNodes = new PersistentNode<TKey, TValue>[0];

        private readonly int _dataMap;
        private readonly int _nodeMap;
        private readonly PersistentEntry<TKey, TValue>[] _data;
        private readonly PersistentNode<TKey, TValue>[] _nodes;

        internal BitmapNode(
            int dataMap,
            int nodeMap,
            PersistentEntry<TKey, TValue>[] data,
            PersistentNode<TKey, TValue>[] nodes)
        {
            _dataMap = dataMap;
            _nodeMap = nodeMap;
            _data = data;
            _nodes = nodes;
        }

        /// <summary>Gets the number of inline entries and sub-tries this node holds.</summary>
        internal int ChildCount => _data.Length + _nodes.Length;

        internal override PersistentNode<TKey, TValue> Add(
            int shift,
            int hash,
            TKey key,
            TValue value,
            IEqualityComparer<TKey> keyComparer,
            bool allowDuplicateValues,
            out bool changed)
        {
            var bit = 1 << PersistentNodeMerge<TKey, TValue>.Slot(hash, shift);
            var nodeIndex = BitCount(_nodeMap & (bit - 1));

            if ((_nodeMap & bit) != 0)
            {
                var child = _nodes[nodeIndex];
                var replacement = child.Add(shift + PersistentNodeMerge<TKey, TValue>.SlotBits, hash, key, value, keyComparer, allowDuplicateValues, out changed);
                if (!changed)
                {
                    return this;
                }

                return new BitmapNode<TKey, TValue>(_dataMap, _nodeMap, _data, Replace(_nodes, nodeIndex, replacement));
            }

            var dataIndex = BitCount(_dataMap & (bit - 1));

            if ((_dataMap & bit) != 0)
            {
                var existing = _data[dataIndex];

                if (existing.Hash == hash && keyComparer.Equals(existing.Key, key))
                {
                    if (!allowDuplicateValues && ContainsValue(existing.Values, value))
                    {
                        changed = false;
                        return this;
                    }

                    changed = true;
                    var grown = existing.WithValues(new PersistentValueCell<TValue>(value, existing.Values));
                    return new BitmapNode<TKey, TValue>(_dataMap, _nodeMap, Replace(_data, dataIndex, grown), _nodes);
                }

                // Two different keys want the same slot, so the slot has to become a sub-trie.
                var merged = PersistentNodeMerge<TKey, TValue>.Entries(shift + PersistentNodeMerge<TKey, TValue>.SlotBits, existing, new PersistentEntry<TKey, TValue>(hash, key, new PersistentValueCell<TValue>(value, null)));
                changed = true;
                return new BitmapNode<TKey, TValue>(
                    _dataMap & ~bit,
                    _nodeMap | bit,
                    RemoveAt(_data, dataIndex),
                    InsertAt(_nodes, nodeIndex, merged));
            }

            changed = true;
            var inserted = new PersistentEntry<TKey, TValue>(hash, key, new PersistentValueCell<TValue>(value, null));
            return new BitmapNode<TKey, TValue>(
                _dataMap | bit,
                _nodeMap,
                InsertAt(_data, dataIndex, inserted),
                _nodes);
        }

        internal override PersistentNode<TKey, TValue>? RemoveValue(
            int shift,
            int hash,
            TKey key,
            TValue value,
            IEqualityComparer<TKey> keyComparer,
            out bool removed)
        {
            var bit = 1 << PersistentNodeMerge<TKey, TValue>.Slot(hash, shift);
            removed = false;

            if ((_nodeMap & bit) != 0)
            {
                var nodeIndex = BitCount(_nodeMap & (bit - 1));
                var child = _nodes[nodeIndex];
                var replacement = child.RemoveValue(shift + PersistentNodeMerge<TKey, TValue>.SlotBits, hash, key, value, keyComparer, out removed);

                if (!removed)
                {
                    return this;
                }

                if (replacement == null)
                {
                    var nodes = RemoveAt(_nodes, nodeIndex);
                    if (_data.Length == 0 && nodes.Length == 0)
                    {
                        return null;
                    }

                    return new BitmapNode<TKey, TValue>(_dataMap, _nodeMap & ~bit, _data, nodes);
                }

                return new BitmapNode<TKey, TValue>(_dataMap, _nodeMap, _data, Replace(_nodes, nodeIndex, replacement));
            }

            if ((_dataMap & bit) == 0)
            {
                return this;
            }

            var dataIndex = BitCount(_dataMap & (bit - 1));
            var existing = _data[dataIndex];

            if (existing.Hash != hash || !keyComparer.Equals(existing.Key, key))
            {
                return this;
            }

            var trimmed = RemoveOneCopy(existing.Values, value);
            if (!trimmed.Found)
            {
                return this;
            }

            removed = true;

            if (trimmed.Remaining == null)
            {
                var data = RemoveAt(_data, dataIndex);
                if (data.Length == 0 && _nodes.Length == 0)
                {
                    return null;
                }

                return new BitmapNode<TKey, TValue>(_dataMap & ~bit, _nodeMap, data, _nodes);
            }

            return new BitmapNode<TKey, TValue>(
                _dataMap,
                _nodeMap,
                Replace(_data, dataIndex, existing.WithValues(trimmed.Remaining)),
                _nodes);
        }

        internal override PersistentNode<TKey, TValue>? RemoveKey(
            int shift,
            int hash,
            TKey key,
            IEqualityComparer<TKey> keyComparer,
            out int removedValues)
        {
            var bit = 1 << PersistentNodeMerge<TKey, TValue>.Slot(hash, shift);
            removedValues = 0;

            if ((_nodeMap & bit) != 0)
            {
                var nodeIndex = BitCount(_nodeMap & (bit - 1));
                var child = _nodes[nodeIndex];
                var replacement = child.RemoveKey(shift + PersistentNodeMerge<TKey, TValue>.SlotBits, hash, key, keyComparer, out removedValues);

                if (removedValues == 0)
                {
                    return this;
                }

                if (replacement == null)
                {
                    var nodes = RemoveAt(_nodes, nodeIndex);
                    if (_data.Length == 0 && nodes.Length == 0)
                    {
                        return null;
                    }

                    return new BitmapNode<TKey, TValue>(_dataMap, _nodeMap & ~bit, _data, nodes);
                }

                return new BitmapNode<TKey, TValue>(_dataMap, _nodeMap, _data, Replace(_nodes, nodeIndex, replacement));
            }

            if ((_dataMap & bit) == 0)
            {
                return this;
            }

            var dataIndex = BitCount(_dataMap & (bit - 1));
            var existing = _data[dataIndex];

            if (existing.Hash != hash || !keyComparer.Equals(existing.Key, key))
            {
                return this;
            }

            removedValues = existing.ValueCount;
            var data = RemoveAt(_data, dataIndex);
            if (data.Length == 0 && _nodes.Length == 0)
            {
                return null;
            }

            return new BitmapNode<TKey, TValue>(_dataMap & ~bit, _nodeMap, data, _nodes);
        }

        internal override bool TryGetEntry(
            int hash,
            TKey key,
            IEqualityComparer<TKey> keyComparer,
            out PersistentEntry<TKey, TValue> entry)
        {
            var shift = 0;
            PersistentNode<TKey, TValue> current = this;

            while (true)
            {
                var bitmap = current as BitmapNode<TKey, TValue>;
                if (bitmap == null)
                {
                    var collision = (CollisionNode<TKey, TValue>)current;
                    return collision.TryGetEntry(hash, key, keyComparer, out entry);
                }

                var bit = 1 << PersistentNodeMerge<TKey, TValue>.Slot(hash, shift);

                if ((bitmap._nodeMap & bit) != 0)
                {
                    current = bitmap._nodes[BitCount(bitmap._nodeMap & (bit - 1))];
                    shift += PersistentNodeMerge<TKey, TValue>.SlotBits;
                    continue;
                }

                if ((bitmap._dataMap & bit) == 0)
                {
                    entry = default(PersistentEntry<TKey, TValue>);
                    return false;
                }

                var existing = bitmap._data[BitCount(bitmap._dataMap & (bit - 1))];
                if (existing.Hash == hash && keyComparer.Equals(existing.Key, key))
                {
                    entry = existing;
                    return true;
                }

                entry = default(PersistentEntry<TKey, TValue>);
                return false;
            }
        }

        internal override void CollectEntries(List<PersistentEntry<TKey, TValue>> sink)
        {
            for (var index = 0; index < _data.Length; index++)
            {
                sink.Add(_data[index]);
            }

            for (var index = 0; index < _nodes.Length; index++)
            {
                _nodes[index].CollectEntries(sink);
            }
        }

        private static TrimResult RemoveOneCopy(PersistentValueCell<TValue>? values, TValue value)
        {
            var comparer = EqualityComparer<TValue>.Default;
            var matched = false;

            if (values == null)
            {
                return TrimResult.NotFound;
            }

            PersistentValueCell<TValue>? remaining = null;
            var cells = new List<PersistentValueCell<TValue>>(values.Count);

            for (var cell = values; cell != null; cell = cell.Next)
            {
                cells.Add(cell);
            }

            // Walk newest-first so the copy removed is the one most recently added, which keeps the
            // observable order of the survivors identical to removing an earlier copy.
            for (var index = 0; index < cells.Count; index++)
            {
                var cell = cells[index];

                if (!matched && comparer.Equals(cell.Value, value))
                {
                    matched = true;
                    continue;
                }

                remaining = new PersistentValueCell<TValue>(cell.Value, remaining);
            }

            return matched ? TrimResult.Removed(remaining) : TrimResult.NotFound;
        }

        private static bool ContainsValue(PersistentValueCell<TValue>? values, TValue value)
        {
            var comparer = EqualityComparer<TValue>.Default;

            for (var cell = values; cell != null; cell = cell.Next)
            {
                if (comparer.Equals(cell.Value, value))
                {
                    return true;
                }
            }

            return false;
        }

        private static int BitCount(int bits)
        {
            var count = 0;
            while (bits != 0)
            {
                bits &= bits - 1;
                count++;
            }

            return count;
        }

        private static PersistentEntry<TKey, TValue>[] Replace(PersistentEntry<TKey, TValue>[] source, int index, PersistentEntry<TKey, TValue> value)
        {
            var copy = new PersistentEntry<TKey, TValue>[source.Length];
            Array.Copy(source, copy, source.Length);
            copy[index] = value;
            return copy;
        }

        private static PersistentNode<TKey, TValue>[] Replace(PersistentNode<TKey, TValue>[] source, int index, PersistentNode<TKey, TValue> value)
        {
            var copy = new PersistentNode<TKey, TValue>[source.Length];
            Array.Copy(source, copy, source.Length);
            copy[index] = value;
            return copy;
        }

        private static PersistentEntry<TKey, TValue>[] InsertAt(PersistentEntry<TKey, TValue>[] source, int index, PersistentEntry<TKey, TValue> value)
        {
            var copy = new PersistentEntry<TKey, TValue>[source.Length + 1];
            Array.Copy(source, 0, copy, 0, index);
            copy[index] = value;
            Array.Copy(source, index, copy, index + 1, source.Length - index);
            return copy;
        }

        private static PersistentNode<TKey, TValue>[] InsertAt(PersistentNode<TKey, TValue>[] source, int index, PersistentNode<TKey, TValue> value)
        {
            var copy = new PersistentNode<TKey, TValue>[source.Length + 1];
            Array.Copy(source, 0, copy, 0, index);
            copy[index] = value;
            Array.Copy(source, index, copy, index + 1, source.Length - index);
            return copy;
        }

        private static PersistentEntry<TKey, TValue>[] RemoveAt(PersistentEntry<TKey, TValue>[] source, int index)
        {
            var copy = new PersistentEntry<TKey, TValue>[source.Length - 1];
            Array.Copy(source, 0, copy, 0, index);
            Array.Copy(source, index + 1, copy, index, source.Length - index - 1);
            return copy;
        }

        private static PersistentNode<TKey, TValue>[] RemoveAt(PersistentNode<TKey, TValue>[] source, int index)
        {
            var copy = new PersistentNode<TKey, TValue>[source.Length - 1];
            Array.Copy(source, 0, copy, 0, index);
            Array.Copy(source, index + 1, copy, index, source.Length - index - 1);
            return copy;
        }

        private readonly struct TrimResult
        {
            private TrimResult(bool found, PersistentValueCell<TValue>? remaining)
            {
                Found = found;
                Remaining = remaining;
            }

            internal static TrimResult NotFound { get; } = new TrimResult(false, null);

            internal static TrimResult Removed(PersistentValueCell<TValue>? remaining)
            {
                return new TrimResult(true, remaining);
            }

            internal bool Found { get; }

            internal PersistentValueCell<TValue>? Remaining { get; }
        }
    }

    /// <summary>
    /// A node holding the entries whose masked hashes are identical.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// This is the only place where lookups degrade to a scan, and it is reachable only when the
    /// comparer produces the same 32-bit hash for several keys - with a well-behaved comparer the
    /// node holds a single entry and never grows.
    /// </remarks>
    internal sealed class CollisionNode<TKey, TValue> : PersistentNode<TKey, TValue>
    {
        private readonly int _hash;
        private readonly PersistentEntry<TKey, TValue>[] _entries;

        internal CollisionNode(int hash, PersistentEntry<TKey, TValue>[] entries)
        {
            _hash = hash;
            _entries = entries;
        }

        /// <summary>Gets the masked hash every entry in this node shares.</summary>
        internal int Hash => _hash;

        internal override PersistentNode<TKey, TValue> Add(
            int shift,
            int hash,
            TKey key,
            TValue value,
            IEqualityComparer<TKey> keyComparer,
            bool allowDuplicateValues,
            out bool changed)
        {
            if (hash != _hash)
            {
                // Every entry here shares _hash, so they all land in the same slot at every level.
                // Meeting a different hash therefore means the slot has to grow one level, with this
                // node kept wholesale as one of the two children.
                changed = true;
                return PersistentNodeMerge<TKey, TValue>.NodeAndEntry(
                    shift,
                    this,
                    new PersistentEntry<TKey, TValue>(hash, key, new PersistentValueCell<TValue>(value, null)));
            }

            for (var index = 0; index < _entries.Length; index++)
            {
                var existing = _entries[index];

                if (!keyComparer.Equals(existing.Key, key))
                {
                    continue;
                }

                if (!allowDuplicateValues && ContainsValue(existing.Values, value))
                {
                    changed = false;
                    return this;
                }

                changed = true;
                var grown = existing.WithValues(new PersistentValueCell<TValue>(value, existing.Values));
                var entries = new PersistentEntry<TKey, TValue>[_entries.Length];
                Array.Copy(_entries, entries, _entries.Length);
                entries[index] = grown;
                return new CollisionNode<TKey, TValue>(_hash, entries);
            }

            changed = true;
            var inserted = new PersistentEntry<TKey, TValue>(hash, key, new PersistentValueCell<TValue>(value, null));
            var appended = new PersistentEntry<TKey, TValue>[_entries.Length + 1];
            Array.Copy(_entries, appended, _entries.Length);
            appended[_entries.Length] = inserted;
            return new CollisionNode<TKey, TValue>(_hash, appended);
        }

        internal override PersistentNode<TKey, TValue>? RemoveValue(
            int shift,
            int hash,
            TKey key,
            TValue value,
            IEqualityComparer<TKey> keyComparer,
            out bool removed)
        {
            removed = false;

            if (hash != _hash)
            {
                return this;
            }

            for (var index = 0; index < _entries.Length; index++)
            {
                var existing = _entries[index];

                if (!keyComparer.Equals(existing.Key, key))
                {
                    continue;
                }

                var comparer = EqualityComparer<TValue>.Default;
                PersistentValueCell<TValue>? remaining = null;
                PersistentValueCell<TValue>? hit = null;

                for (var cell = existing.Values; cell != null; cell = cell.Next)
                {
                    if (hit == null && comparer.Equals(cell.Value, value))
                    {
                        hit = cell;
                        continue;
                    }

                    remaining = new PersistentValueCell<TValue>(cell.Value, remaining);
                }

                if (hit == null)
                {
                    return this;
                }

                removed = true;

                if (remaining != null)
                {
                    var entries = new PersistentEntry<TKey, TValue>[_entries.Length];
                    Array.Copy(_entries, entries, _entries.Length);
                    entries[index] = existing.WithValues(remaining);
                    return new CollisionNode<TKey, TValue>(_hash, entries);
                }

                if (_entries.Length == 1)
                {
                    return null;
                }

                var smaller = new PersistentEntry<TKey, TValue>[_entries.Length - 1];
                Array.Copy(_entries, 0, smaller, 0, index);
                Array.Copy(_entries, index + 1, smaller, index, _entries.Length - index - 1);
                return new CollisionNode<TKey, TValue>(_hash, smaller);
            }

            return this;
        }

        internal override PersistentNode<TKey, TValue>? RemoveKey(
            int shift,
            int hash,
            TKey key,
            IEqualityComparer<TKey> keyComparer,
            out int removedValues)
        {
            removedValues = 0;

            if (hash != _hash)
            {
                return this;
            }

            for (var index = 0; index < _entries.Length; index++)
            {
                if (!keyComparer.Equals(_entries[index].Key, key))
                {
                    continue;
                }

                removedValues = _entries[index].ValueCount;

                if (_entries.Length == 1)
                {
                    return null;
                }

                var smaller = new PersistentEntry<TKey, TValue>[_entries.Length - 1];
                Array.Copy(_entries, 0, smaller, 0, index);
                Array.Copy(_entries, index + 1, smaller, index, _entries.Length - index - 1);
                return new CollisionNode<TKey, TValue>(_hash, smaller);
            }

            return this;
        }

        internal override bool TryGetEntry(
            int hash,
            TKey key,
            IEqualityComparer<TKey> keyComparer,
            out PersistentEntry<TKey, TValue> entry)
        {
            if (hash == _hash)
            {
                for (var index = 0; index < _entries.Length; index++)
                {
                    if (keyComparer.Equals(_entries[index].Key, key))
                    {
                        entry = _entries[index];
                        return true;
                    }
                }
            }

            entry = default(PersistentEntry<TKey, TValue>);
            return false;
        }

        internal override void CollectEntries(List<PersistentEntry<TKey, TValue>> sink)
        {
            for (var index = 0; index < _entries.Length; index++)
            {
                sink.Add(_entries[index]);
            }
        }

        private static bool ContainsValue(PersistentValueCell<TValue>? values, TValue value)
        {
            var comparer = EqualityComparer<TValue>.Default;

            for (var cell = values; cell != null; cell = cell.Next)
            {
                if (comparer.Equals(cell.Value, value))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Builds the node that replaces a slot when two incompatible things want it.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// Both entry points exist because a slot can already hold a <see cref="CollisionNode{TKey,TValue}"/>
    /// - a whole cluster of entries sharing one hash - when a new key with a different hash arrives.
    /// The cluster cannot be split, so it is carried down as a single child while the newcomer takes
    /// the other child at the first level where the two hashes disagree.
    /// </remarks>
    internal static class PersistentNodeMerge<TKey, TValue>
    {
        /// <summary>The number of bits consumed per trie level.</summary>
        internal const int SlotBits = 5;

        private const int SlotMask = 0x1F;
        private const int LastShift = 30;

        /// <summary>Returns the slot index a hash selects at a given level.</summary>
        /// <param name="hash">The masked hash.</param>
        /// <param name="shift">The bit offset of the level.</param>
        /// <returns>A value in <c>[0, 31]</c>.</returns>
        internal static int Slot(int hash, int shift)
        {
            return (hash >> shift) & SlotMask;
        }

        /// <summary>Builds the node that holds two entries wanting the same slot.</summary>
        /// <param name="shift">The bit offset of the level the two must share.</param>
        /// <param name="left">The entry already in place.</param>
        /// <param name="right">The entry being inserted.</param>
        /// <returns>A collision node when the hashes are equal, otherwise the smallest sub-trie that separates them.</returns>
        internal static PersistentNode<TKey, TValue> Entries(int shift, PersistentEntry<TKey, TValue> left, PersistentEntry<TKey, TValue> right)
        {
            if (left.Hash == right.Hash)
            {
                return new CollisionNode<TKey, TValue>(left.Hash, new[] { left, right });
            }

            var leftBit = 1 << Slot(left.Hash, shift);
            var rightBit = 1 << Slot(right.Hash, shift);

            if (leftBit == rightBit)
            {
                // Unreachable past the last level: arriving here with equal slot bits at every level
                // would mean equal hashes, which the branch above already took.
                var inner = Entries(shift + SlotBits, left, right);
                return new BitmapNode<TKey, TValue>(0, leftBit, new PersistentEntry<TKey, TValue>[0], new[] { inner });
            }

            return leftBit < rightBit
                ? new BitmapNode<TKey, TValue>(leftBit | rightBit, 0, new[] { left, right }, new PersistentNode<TKey, TValue>[0])
                : new BitmapNode<TKey, TValue>(leftBit | rightBit, 0, new[] { right, left }, new PersistentNode<TKey, TValue>[0]);
        }

        /// <summary>Builds the node that holds a collision cluster and one entry with a different hash.</summary>
        /// <param name="shift">The bit offset of the level the two must share.</param>
        /// <param name="cluster">The collision node to keep intact.</param>
        /// <param name="entry">The entry being inserted.</param>
        /// <returns>The smallest sub-trie that separates the cluster from the entry.</returns>
        internal static PersistentNode<TKey, TValue> NodeAndEntry(int shift, CollisionNode<TKey, TValue> cluster, PersistentEntry<TKey, TValue> entry)
        {
            var clusterBit = 1 << Slot(cluster.Hash, shift);
            var entryBit = 1 << Slot(entry.Hash, shift);

            if (clusterBit == entryBit)
            {
                if (shift >= LastShift)
                {
                    // Reaching the last level with identical slot bits at every level would mean
                    // identical hashes, which the caller rules out; fail loudly rather than corrupt.
                    throw new InvalidOperationException("Two distinct hashes selected the same slot on every level.");
                }

                var inner = NodeAndEntry(shift + SlotBits, cluster, entry);
                return new BitmapNode<TKey, TValue>(0, clusterBit, new PersistentEntry<TKey, TValue>[0], new[] { inner });
            }

            var entryAsNode = new CollisionNode<TKey, TValue>(entry.Hash, new[] { entry });
            return clusterBit < entryBit
                ? new BitmapNode<TKey, TValue>(0, clusterBit | entryBit, new PersistentEntry<TKey, TValue>[0], new PersistentNode<TKey, TValue>[] { cluster, entryAsNode })
                : new BitmapNode<TKey, TValue>(0, clusterBit | entryBit, new PersistentEntry<TKey, TValue>[0], new PersistentNode<TKey, TValue>[] { entryAsNode, cluster });
        }
    }
}
