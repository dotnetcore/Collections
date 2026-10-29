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
    /// level down. <see cref="PersistentPayload{TKey,TValue}.Count"/> and the value materialisers
    /// walk the cells once, which restores insertion order.
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
    /// The copies held under one key, in whatever shape the owning collection needs.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// <para>
    /// The trie is the same for every collection in this family; only the payload differs. A multimap
    /// stores one copy per distinct value, which needs a list
    /// (<see cref="ValueCellPayload{TKey,TValue}"/>). A multiset stores a multiplicity of a single
    /// element, which only needs a counter
    /// (<see cref="CounterPayload{TKey,TValue}"/>) - keeping <c>m</c> copies of one element in
    /// <c>m</c> cells would be the one allocation pattern a bag cannot afford.
    /// </para>
    /// <para>
    /// Every member returns a new payload and leaves the receiver untouched, so a payload is shared
    /// between snapshots exactly like the nodes that hold it.
    /// </para>
    /// </remarks>
    internal abstract class PersistentPayload<TKey, TValue>
    {
        /// <summary>Gets the number of copies this payload holds.</summary>
        internal abstract int Count { get; }

        /// <summary>Returns a payload holding more copies.</summary>
        /// <param name="value">The copy to add.</param>
        /// <param name="copies">The number of copies to add; must be positive.</param>
        /// <returns>The new payload.</returns>
        internal abstract PersistentPayload<TKey, TValue> GrowCopies(TValue value, int copies);

        /// <summary>Determines whether a value is among the copies.</summary>
        /// <param name="value">The value to locate.</param>
        /// <returns><c>true</c> when at least one copy equals the value.</returns>
        internal abstract bool Contains(TValue value);

        /// <summary>Returns a payload with copies of a value removed.</summary>
        /// <param name="value">The value whose copies should go.</param>
        /// <param name="copies">The maximum number of copies to remove; must be positive.</param>
        /// <param name="remaining">
        /// Receives the replacement payload, or <c>null</c> when the last copy went.
        /// </param>
        /// <param name="removed">Receives the number of copies actually removed.</param>
        /// <returns><c>true</c> when at least one copy was removed.</returns>
        internal abstract bool TryRemoveCopies(TValue value, int copies, out PersistentPayload<TKey, TValue>? remaining, out int removed);

        /// <summary>Copies every copy into an array, oldest first.</summary>
        /// <param name="target">The array to fill; its length must equal <see cref="Count"/>.</param>
        internal abstract void Expand(TValue[] target);

        /// <summary>Enumerates every copy.</summary>
        /// <returns>The copies, newest first.</returns>
        internal abstract IEnumerable<TValue> Enumerate();
    }

    /// <summary>
    /// A payload that keeps one cell per copy, used by the multimap where every copy is a distinct
    /// value.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    internal sealed class ValueCellPayload<TKey, TValue> : PersistentPayload<TKey, TValue>
    {
        private readonly PersistentValueCell<TValue> _cells;

        internal ValueCellPayload(PersistentValueCell<TValue> cells)
        {
            _cells = cells;
        }

        /// <summary>Gets the cell chain, newest first.</summary>
        internal PersistentValueCell<TValue> Cells => _cells;

        internal override int Count => _cells.Count;

        internal override PersistentPayload<TKey, TValue> GrowCopies(TValue value, int copies)
        {
            var cells = new PersistentValueCell<TValue>(value, _cells);

            for (var copy = 1; copy < copies; copy++)
            {
                cells = new PersistentValueCell<TValue>(value, cells);
            }

            return new ValueCellPayload<TKey, TValue>(cells);
        }

        internal override bool Contains(TValue value)
        {
            var comparer = EqualityComparer<TValue>.Default;

            for (var cell = _cells; cell != null; cell = cell.Next)
            {
                if (comparer.Equals(cell.Value, value))
                {
                    return true;
                }
            }

            return false;
        }

        internal override bool TryRemoveCopies(TValue value, int copies, out PersistentPayload<TKey, TValue>? remaining, out int removed)
        {
            var comparer = EqualityComparer<TValue>.Default;
            var retained = new List<PersistentValueCell<TValue>>(_cells.Count);
            removed = 0;

            // Walk newest-first so the copies removed are the ones most recently added.
            for (var cell = _cells; cell != null; cell = cell.Next)
            {
                if (removed < copies && comparer.Equals(cell.Value, value))
                {
                    removed++;
                    continue;
                }

                retained.Add(cell);
            }

            if (removed == 0)
            {
                remaining = null;
                return false;
            }

            // The chain is newest-first, so the survivors are relinked from the oldest: keeping the
            // walked order would quietly reverse the surviving copies on every removal.
            PersistentValueCell<TValue>? rebuilt = null;
            for (var index = retained.Count - 1; index >= 0; index--)
            {
                rebuilt = new PersistentValueCell<TValue>(retained[index].Value, rebuilt);
            }

            remaining = rebuilt == null ? null : new ValueCellPayload<TKey, TValue>(rebuilt);
            return true;
        }

        internal override void Expand(TValue[] target)
        {
            var index = target.Length - 1;

            for (var cell = _cells; cell != null; cell = cell.Next)
            {
                target[index] = cell.Value;
                index--;
            }
        }

        internal override IEnumerable<TValue> Enumerate()
        {
            for (var cell = _cells; cell != null; cell = cell.Next)
            {
                yield return cell.Value;
            }
        }
    }

    /// <summary>
    /// A payload that keeps a multiplicity instead of a cell per copy, used by the multiset where
    /// every copy under a key <em>is</em> the key.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// <para>
    /// One counter per distinct element instead of one cell per copy: a million copies of one element
    /// cost a single payload, so memory stays proportional to the number of <em>distinct</em> elements
    /// rather than to the number of copies, and the work of adding them does not grow with how many
    /// are already held.
    /// </para>
    /// <para>
    /// The element itself is kept alongside the counter so that materialising the copies needs no
    /// cast and no back-reference to the key. A value handed to <see cref="GrowCopies"/> that differs
    /// from the stored one is ignored: with this payload the value is fixed by the first copy, which
    /// is exactly right for a bag, where the key <em>is</em> the element.
    /// </para>
    /// </remarks>
    internal sealed class CounterPayload<TKey, TValue> : PersistentPayload<TKey, TValue>
    {
        private readonly TValue _value;
        private readonly int _copies;

        internal CounterPayload(TValue value, int copies)
        {
            _value = value;
            _copies = copies;
        }

        internal override int Count => _copies;

        internal override PersistentPayload<TKey, TValue> GrowCopies(TValue value, int copies)
        {
            return new CounterPayload<TKey, TValue>(_value, _copies + copies);
        }

        internal override bool Contains(TValue value)
        {
            // Every copy equals the stored value, so the only two answers are "all of them" or none.
            return _copies > 0 && EqualityComparer<TValue>.Default.Equals(_value, value);
        }

        internal override bool TryRemoveCopies(TValue value, int copies, out PersistentPayload<TKey, TValue>? remaining, out int removed)
        {
            removed = copies < _copies ? copies : _copies;

            if (removed == 0)
            {
                remaining = null;
                return false;
            }

            var left = _copies - removed;
            remaining = left > 0 ? new CounterPayload<TKey, TValue>(_value, left) : null;
            return true;
        }

        internal override void Expand(TValue[] target)
        {
            for (var index = 0; index < target.Length; index++)
            {
                target[index] = _value;
            }
        }

        internal override IEnumerable<TValue> Enumerate()
        {
            for (var index = 0; index < _copies; index++)
            {
                yield return _value;
            }
        }
    }

    /// <summary>
    /// Creates the payload a trie uses for a key that is not present yet.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// The payload kind is a property of the owning collection, so it is supplied by the caller of
    /// the write operation rather than stored on every node: a node then costs no more than it did
    /// before this abstraction existed, and the two collections share one trie instead of keeping
    /// two copies of the same algorithm.
    /// </remarks>
    internal abstract class PersistentPayloadFactory<TKey, TValue>
    {
        /// <summary>A factory for multimap payloads: one cell per copy.</summary>
        internal static readonly PersistentPayloadFactory<TKey, TValue> ValueCells = new ValueCellFactory();

        /// <summary>A factory for multiset payloads: one counter per key.</summary>
        internal static readonly PersistentPayloadFactory<TKey, TValue> Counters = new CounterFactory();

        /// <summary>Creates a payload holding a number of copies of one value.</summary>
        /// <param name="value">The value to store.</param>
        /// <param name="copies">The number of copies; must be positive.</param>
        /// <returns>The new payload.</returns>
        internal abstract PersistentPayload<TKey, TValue> Create(TValue value, int copies);

        /// <summary>Returns a payload with copies added to it.</summary>
        /// <param name="existing">The current payload, or <c>null</c> when the key is new.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="copies">The number of copies to add; must be positive.</param>
        /// <returns>The new payload.</returns>
        internal PersistentPayload<TKey, TValue> Grow(PersistentPayload<TKey, TValue>? existing, TValue value, int copies)
        {
            return existing == null ? Create(value, copies) : existing.GrowCopies(value, copies);
        }

        private sealed class ValueCellFactory : PersistentPayloadFactory<TKey, TValue>
        {
            internal override PersistentPayload<TKey, TValue> Create(TValue value, int copies)
            {
                var cells = new PersistentValueCell<TValue>(value, null);

                for (var copy = 1; copy < copies; copy++)
                {
                    cells = new PersistentValueCell<TValue>(value, cells);
                }

                return new ValueCellPayload<TKey, TValue>(cells);
            }
        }

        private sealed class CounterFactory : PersistentPayloadFactory<TKey, TValue>
        {
            internal override PersistentPayload<TKey, TValue> Create(TValue value, int copies)
            {
                return new CounterPayload<TKey, TValue>(value, copies);
            }
        }
    }

    /// <summary>
    /// One key together with every copy stored under it.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    internal readonly struct PersistentEntry<TKey, TValue>
    {
        internal PersistentEntry(int hash, TKey key, PersistentPayload<TKey, TValue>? payload)
        {
            Hash = hash;
            Key = key;
            Payload = payload;
        }

        /// <summary>Gets the masked hash that placed this entry in its slot.</summary>
        internal int Hash { get; }

        /// <summary>Gets the key.</summary>
        internal TKey Key { get; }

        /// <summary>Gets the copies, or <c>null</c> when the entry holds none.</summary>
        internal PersistentPayload<TKey, TValue>? Payload { get; }

        /// <summary>Gets the number of copies.</summary>
        internal int ValueCount => Payload == null ? 0 : Payload.Count;

        /// <summary>Returns a copy of this entry with a different payload.</summary>
        internal PersistentEntry<TKey, TValue> WithPayload(PersistentPayload<TKey, TValue>? payload)
        {
            return new PersistentEntry<TKey, TValue>(Hash, Key, payload);
        }
    }

    /// <summary>
    /// A node in the compressed hash-array mapped trie behind
    /// <see cref="PersistentMultiDictionary{TKey,TValue}"/> and <see cref="PersistentBag{T}"/>.
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
    /// whole collection. That sharing is the entire point of this structure and is what the
    /// <c>Persistent*</c> family trades its <c>Immutable*</c> predecessor's full clone for.
    /// </para>
    /// <para>
    /// What the copies under a key look like is not this type's business: the write path is handed a
    /// <see cref="PersistentPayloadFactory{TKey,TValue}"/> so the same nodes serve a multimap (one
    /// cell per copy) and a multiset (one counter per key).
    /// </para>
    /// <para>
    /// A mutator returns <c>null</c> to mean "this node is now empty", which is how a parent learns to
    /// drop the corresponding slot.
    /// </para>
    /// </remarks>
    internal abstract class PersistentNode<TKey, TValue>
    {
        /// <summary>Adds copies of a value under a key.</summary>
        /// <param name="shift">The bit offset of the slot space this node owns.</param>
        /// <param name="hash">The masked hash of the key.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to add.</param>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="allowDuplicateValues">
        /// Whether a value already present under this key may be stored again.
        /// </param>
        /// <param name="payloads">The factory that builds the payload for a key that is new here.</param>
        /// <param name="copies">The number of copies to add; must be positive.</param>
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
            PersistentPayloadFactory<TKey, TValue> payloads,
            int copies,
            out bool changed);

        /// <summary>Removes copies of a value from under a key.</summary>
        /// <param name="shift">The bit offset of the slot space this node owns.</param>
        /// <param name="hash">The masked hash of the key.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value to remove copies of.</param>
        /// <param name="copies">The maximum number of copies to remove; must be positive.</param>
        /// <param name="keyComparer">The key comparer.</param>
        /// <param name="removed">Set to the number of copies removed.</param>
        /// <returns>The replacement node, <c>null</c> when empty, or the receiver when nothing changed.</returns>
        internal abstract PersistentNode<TKey, TValue>? RemoveValue(
            int shift,
            int hash,
            TKey key,
            TValue value,
            int copies,
            IEqualityComparer<TKey> keyComparer,
            out int removed);

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
            PersistentPayloadFactory<TKey, TValue> payloads,
            int copies,
            out bool changed)
        {
            var bit = 1 << PersistentNodeMerge<TKey, TValue>.Slot(hash, shift);
            var nodeIndex = BitCount(_nodeMap & (bit - 1));

            if ((_nodeMap & bit) != 0)
            {
                var child = _nodes[nodeIndex];
                var replacement = child.Add(shift + PersistentNodeMerge<TKey, TValue>.SlotBits, hash, key, value, keyComparer, allowDuplicateValues, payloads, copies, out changed);
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
                    if (!allowDuplicateValues && existing.Payload != null && existing.Payload.Contains(value))
                    {
                        changed = false;
                        return this;
                    }

                    changed = true;
                    var grown = existing.WithPayload(payloads.Grow(existing.Payload, value, copies));
                    return new BitmapNode<TKey, TValue>(_dataMap, _nodeMap, Replace(_data, dataIndex, grown), _nodes);
                }

                // Two different keys want the same slot, so the slot has to become a sub-trie.
                var inserted = new PersistentEntry<TKey, TValue>(hash, key, payloads.Create(value, copies));
                var merged = PersistentNodeMerge<TKey, TValue>.Entries(shift + PersistentNodeMerge<TKey, TValue>.SlotBits, existing, inserted);
                changed = true;
                return new BitmapNode<TKey, TValue>(
                    _dataMap & ~bit,
                    _nodeMap | bit,
                    RemoveAt(_data, dataIndex),
                    InsertAt(_nodes, nodeIndex, merged));
            }

            changed = true;
            var first = new PersistentEntry<TKey, TValue>(hash, key, payloads.Create(value, copies));
            return new BitmapNode<TKey, TValue>(
                _dataMap | bit,
                _nodeMap,
                InsertAt(_data, dataIndex, first),
                _nodes);
        }

        internal override PersistentNode<TKey, TValue>? RemoveValue(
            int shift,
            int hash,
            TKey key,
            TValue value,
            int copies,
            IEqualityComparer<TKey> keyComparer,
            out int removed)
        {
            var bit = 1 << PersistentNodeMerge<TKey, TValue>.Slot(hash, shift);
            removed = 0;

            if ((_nodeMap & bit) != 0)
            {
                var nodeIndex = BitCount(_nodeMap & (bit - 1));
                var child = _nodes[nodeIndex];
                var replacement = child.RemoveValue(shift + PersistentNodeMerge<TKey, TValue>.SlotBits, hash, key, value, copies, keyComparer, out removed);

                if (removed == 0)
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

            if (existing.Hash != hash || !keyComparer.Equals(existing.Key, key) || existing.Payload == null)
            {
                return this;
            }

            PersistentPayload<TKey, TValue>? remaining;
            if (!existing.Payload.TryRemoveCopies(value, copies, out remaining, out removed))
            {
                return this;
            }

            if (remaining == null)
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
                Replace(_data, dataIndex, existing.WithPayload(remaining)),
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
            var data2 = RemoveAt(_data, dataIndex);
            if (data2.Length == 0 && _nodes.Length == 0)
            {
                return null;
            }

            return new BitmapNode<TKey, TValue>(_dataMap & ~bit, _nodeMap, data2, _nodes);
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
            PersistentPayloadFactory<TKey, TValue> payloads,
            int copies,
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
                    new PersistentEntry<TKey, TValue>(hash, key, payloads.Create(value, copies)));
            }

            for (var index = 0; index < _entries.Length; index++)
            {
                var existing = _entries[index];

                if (!keyComparer.Equals(existing.Key, key))
                {
                    continue;
                }

                if (!allowDuplicateValues && existing.Payload != null && existing.Payload.Contains(value))
                {
                    changed = false;
                    return this;
                }

                changed = true;
                var grown = existing.WithPayload(payloads.Grow(existing.Payload, value, copies));
                var entries = new PersistentEntry<TKey, TValue>[_entries.Length];
                Array.Copy(_entries, entries, _entries.Length);
                entries[index] = grown;
                return new CollisionNode<TKey, TValue>(_hash, entries);
            }

            changed = true;
            var inserted = new PersistentEntry<TKey, TValue>(hash, key, payloads.Create(value, copies));
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
            int copies,
            IEqualityComparer<TKey> keyComparer,
            out int removed)
        {
            removed = 0;

            if (hash != _hash)
            {
                return this;
            }

            for (var index = 0; index < _entries.Length; index++)
            {
                var existing = _entries[index];

                if (!keyComparer.Equals(existing.Key, key) || existing.Payload == null)
                {
                    continue;
                }

                PersistentPayload<TKey, TValue>? remaining;
                if (!existing.Payload.TryRemoveCopies(value, copies, out remaining, out removed))
                {
                    return this;
                }

                if (remaining != null)
                {
                    var entries = new PersistentEntry<TKey, TValue>[_entries.Length];
                    Array.Copy(_entries, entries, _entries.Length);
                    entries[index] = existing.WithPayload(remaining);
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
        /// <remarks>
        /// The children are ordered by <em>slot number</em>, never by the shifted bit value: slot 31
        /// yields <c>1 &lt;&lt; 31</c>, which is negative, so comparing bits would place the highest
        /// slot before every other one and the index arithmetic that reads the bitmap back would
        /// resolve to the wrong child.
        /// </remarks>
        internal static PersistentNode<TKey, TValue> Entries(int shift, PersistentEntry<TKey, TValue> left, PersistentEntry<TKey, TValue> right)
        {
            if (left.Hash == right.Hash)
            {
                return new CollisionNode<TKey, TValue>(left.Hash, new[] { left, right });
            }

            var leftSlot = Slot(left.Hash, shift);
            var rightSlot = Slot(right.Hash, shift);

            if (leftSlot == rightSlot)
            {
                // Unreachable past the last level: arriving here with equal slot bits at every level
                // would mean equal hashes, which the branch above already took.
                var inner = Entries(shift + SlotBits, left, right);
                return new BitmapNode<TKey, TValue>(0, 1 << leftSlot, new PersistentEntry<TKey, TValue>[0], new[] { inner });
            }

            var bits = (1 << leftSlot) | (1 << rightSlot);
            return leftSlot < rightSlot
                ? new BitmapNode<TKey, TValue>(bits, 0, new[] { left, right }, new PersistentNode<TKey, TValue>[0])
                : new BitmapNode<TKey, TValue>(bits, 0, new[] { right, left }, new PersistentNode<TKey, TValue>[0]);
        }

        /// <summary>Builds the node that holds a collision cluster and one entry with a different hash.</summary>
        /// <param name="shift">The bit offset of the level the two must share.</param>
        /// <param name="cluster">The collision node to keep intact.</param>
        /// <param name="entry">The entry being inserted.</param>
        /// <returns>The smallest sub-trie that separates the cluster from the entry.</returns>
        internal static PersistentNode<TKey, TValue> NodeAndEntry(int shift, CollisionNode<TKey, TValue> cluster, PersistentEntry<TKey, TValue> entry)
        {
            var clusterSlot = Slot(cluster.Hash, shift);
            var entrySlot = Slot(entry.Hash, shift);

            if (clusterSlot == entrySlot)
            {
                if (shift >= LastShift)
                {
                    // Reaching the last level with identical slot bits at every level would mean
                    // identical hashes, which the caller rules out; fail loudly rather than corrupt.
                    throw new InvalidOperationException("Two distinct hashes selected the same slot on every level.");
                }

                var inner = NodeAndEntry(shift + SlotBits, cluster, entry);
                return new BitmapNode<TKey, TValue>(0, 1 << clusterSlot, new PersistentEntry<TKey, TValue>[0], new[] { inner });
            }

            var entryAsNode = new CollisionNode<TKey, TValue>(entry.Hash, new[] { entry });
            var bits = (1 << clusterSlot) | (1 << entrySlot);
            return clusterSlot < entrySlot
                ? new BitmapNode<TKey, TValue>(0, bits, new PersistentEntry<TKey, TValue>[0], new PersistentNode<TKey, TValue>[] { cluster, entryAsNode })
                : new BitmapNode<TKey, TValue>(0, bits, new PersistentEntry<TKey, TValue>[0], new PersistentNode<TKey, TValue>[] { entryAsNode, cluster });
        }
    }
}
