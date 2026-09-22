using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// The payload type of an <see cref="OrderStatisticTree{TKey,TPayload}"/> that stores no
    /// payload. <see cref="OrderedMultiList{T}"/> keys its tree by the element alone, so its
    /// payload array is never allocated and the tree costs exactly what it did before the
    /// parameter existed.
    /// </summary>
    internal readonly struct NoPayload
    {
    }

    /// <summary>
    /// An order-statistic B+ tree that maps every distinct key to the number of copies stored for
    /// it. This is the storage engine of <see cref="OrderedMultiList{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each distinct key is held in exactly one slot of one leaf, together with its copy count, so
    /// the tree is sized by <em>distinct</em> elements rather than by total copies. Search,
    /// insertion and deletion run in O(log n) worst case, which the B+ invariants guarantee: every
    /// leaf sits at the same depth, every node but the root holds at least <see cref="MinEntries"/>
    /// entries, no node holds more than <see cref="MaxEntries"/> entries once a write is over, and
    /// the keys inside a node are strictly increasing.
    /// </para>
    /// <para>
    /// <b>Order statistics.</b> Every node caches the number of <em>copies</em> stored below it,
    /// and a branch keeps that figure per child (<see cref="BranchNode.Totals"/>). Selecting the
    /// k-th copy and computing the rank of a key are then a single root-to-leaf descent: at each
    /// level the walk skips the children whose totals fit into the remaining rank, and reads the
    /// answer off the leaf it lands on. Nothing is traversed, which is what makes the rank reads
    /// (<c>TryGetByRank</c> and <c>TryGetRankOf</c>) O(log n).
    /// </para>
    /// <para>
    /// <b>Locality.</b> A balanced binary search tree spends one heap object per distinct key and
    /// reaches the next key through a pointer written at an unrelated moment, so a large traversal
    /// chases pointers across the whole heap. Here up to <see cref="MaxEntries"/> keys share one
    /// array, so a single cache line covers many keys, and consecutive leaves are chained
    /// (<see cref="LeafNode.Next"/> / <see cref="LeafNode.Previous"/>) so an in-order sweep never
    /// re-enters the branches. That is why this engine replaced the left-leaning red-black tree
    /// <see cref="OrderedMultiList{T}"/> was built on: the asymptotics of the single-key operations
    /// are unchanged, while iteration, range scans and rank queries stopped paying a pointer hop per
    /// key.
    /// </para>
    /// <para>
    /// <see cref="Validate"/> re-checks each of the invariants above, plus that every separator names
    /// the true smallest key of the sub-tree to its right and that the cached totals still add up.
    /// <see cref="Height"/> is exact and costs one descent, because all leaves are equally deep.
    /// Together they let the O(log n) claim be asserted structurally rather than inferred from
    /// timings, which is what the F6-01 and F6-25 acceptance criteria ask for.
    /// </para>
    /// <para>
    /// <b>Payloads.</b> A caller may attach one value of <c>TPayload</c> to each key, stored beside
    /// the key and its count in the same leaf slot and carried along by every split, borrow and
    /// merge. That is what lets <see cref="OrderedMultiDictionary{TKey,TValue}"/> keep a key's value
    /// bucket in the tree itself: a leaf sweep then yields the key, the bucket and the copy count
    /// together, with no second structure to look the bucket up in and no per-key iterator to build.
    /// The array behind the channel is allocated per leaf on first use, so a tree that never stores a
    /// payload - <see cref="OrderedMultiList{T}"/>'s, whose <c>TPayload</c> is
    /// <see cref="NoPayload"/> - pays nothing for it. The payload is <em>not</em> part of ordering or
    /// identity: a comparer that calls two keys equal makes the first stored payload win, exactly as
    /// it makes the first stored key win.
    /// </para>
    /// <para>
    /// The type is internal and deliberately narrow: it owns the tree, the copy counts and the
    /// sub-tree totals, and knows nothing about the collecting type's bookkeeping beyond
    /// <see cref="Count"/> and <see cref="ElementCount"/>. It is not thread-safe, exactly like the
    /// collection built on top of it.
    /// </para>
    /// </remarks>
    internal sealed class OrderStatisticTree<TKey, TPayload>
    {
        /// <summary>
        /// The most entries a node holds: keys in a leaf, children in a branch. A write is allowed to
        /// overflow by one and the overflowing node is then split, which keeps insertion a single
        /// descent with no look-ahead.
        /// </summary>
        private const int MaxEntries = 32;

        /// <summary>
        /// The fewest entries a node other than the root holds. Two minimal nodes still fit into one
        /// after a merge (2 * 15 &lt;= 32), so repairing an underflow never has to split.
        /// </summary>
        private const int MinEntries = 15;

        /// <summary>
        /// Array slots per node: <see cref="MaxEntries"/> plus the one overflow entry a split starts
        /// from.
        /// </summary>
        private const int Capacity = MaxEntries + 2;

        /// <summary>
        /// The longest root-to-leaf path the descent scratch buffers hold. The bound follows from
        /// the occupancy invariants rather than from taste: a level is gained only when a node
        /// splits, every node but the root keeps at least <see cref="MinEntries"/> entries, and
        /// <see cref="Count"/> is an <see cref="int"/>, so a tree holding the largest representable
        /// number of distinct keys is at most <c>1 + ceil(log15(int.MaxValue)) = 9</c> levels deep.
        /// Twelve leaves a third of the budget unused as margin. The two buffers below are per
        /// instance, so this figure is also what a per-key payload tree pays: 40 levels used to
        /// cost 480 bytes per instance, 12 cost 144.
        /// </summary>
        private const int MaxDepth = 12;

        private readonly IComparer<TKey> _comparer;
        private Node? _root;

        // Scratch buffers holding the root-to-leaf path of the last descending write. Reusing them
        // is the point: a write then allocates nothing but a split, which is part of why this engine
        // beats the pointer-per-key tree it replaced. The tree is not thread-safe, and no operation
        // descends while another is mid-repair, so no locking is involved.
        private readonly Node?[] _path = new Node?[MaxDepth];
        private readonly int[] _childAt = new int[MaxDepth];

        // Whether any payload has ever been stored. A leaf allocates its payload array on first
        // use, so a tree that carries no payload (OrderedMultiList's) never pays for one; this flag
        // is what lets Validate() tell "this tree stores no payloads" apart from "this leaf lost
        // the payload array its siblings have".
        private bool _payloadsInUse;

        // Cursor of the in-flight Validate() walk. Validation is a diagnostic entry point that is
        // never re-entered, so the walk carries its state here instead of through six ref
        // parameters on every recursive call.
        private string? _error;
        private int _leafDepth = -1;
        private LeafNode? _previousLeaf;
        private int _distinctSeen;
        private int _copiesSeen;

        /// <summary>
        /// Initializes an empty tree that orders keys with the specified comparer.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c>.</exception>
        internal OrderStatisticTree(IComparer<TKey> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// Gets the number of distinct keys currently stored.
        /// </summary>
        internal int Count => _root == null ? 0 : _root.KeyCount;

        /// <summary>
        /// Gets the number of copies stored across every key.
        /// </summary>
        internal int ElementCount => _root == null ? 0 : _root.TotalCount;

        /// <summary>
        /// Gets the number of edges on a root-to-leaf path: zero for an empty tree and for a tree
        /// whose root is still a leaf. Every leaf sits at this same depth, so the figure is exact and
        /// costs one descent down the leftmost spine.
        /// </summary>
        internal int Height
        {
            get
            {
                var node = _root;
                var height = 0;
                while (node is BranchNode branch)
                {
                    node = branch.Children[0];
                    height++;
                }

                return height;
            }
        }

        /// <summary>
        /// Removes every key.
        /// </summary>
        internal void Clear()
        {
            _root = null;
        }

        /// <summary>
        /// Gets the copy count stored for the key.
        /// </summary>
        /// <returns><c>true</c> when the key is present, <c>false</c> otherwise.</returns>
        internal bool TryGetCount(TKey key, out int count)
        {
            var leaf = FindLeaf(key);
            if (leaf == null)
            {
                count = 0;
                return false;
            }

            var slot = SlotOf(leaf, key);
            if (slot < 0)
            {
                count = 0;
                return false;
            }

            count = leaf.Counts[slot];
            return true;
        }

        /// <summary>
        /// Gets the payload attached to the key, together with its copy count.
        /// </summary>
        /// <returns><c>true</c> when the key is present, <c>false</c> otherwise.</returns>
        /// <remarks>
        /// This is the lookup a caller makes when it is about to change the payload and will then
        /// have to bring the count back in step with <see cref="AdjustCount"/>: the count it reports
        /// is the figure to compare against afterwards, so the change is
        /// <c>newTotal - reportedCount</c> rather than a fixed one. A tree that stores no payload
        /// reports <c>default</c> and is better served by <see cref="TryGetCount"/>.
        /// </remarks>
        internal bool TryGetEntry(TKey key, out TPayload payload, out int count)
        {
            var leaf = FindLeaf(key);
            if (leaf == null)
            {
                payload = default!;
                count = 0;
                return false;
            }

            var slot = SlotOf(leaf, key);
            if (slot < 0)
            {
                payload = default!;
                count = 0;
                return false;
            }

            payload = leaf.Payloads == null ? default! : leaf.Payloads[slot];
            count = leaf.Counts[slot];
            return true;
        }

        /// <summary>
        /// Gives the leaf a payload array when it needs one. A leaf that carries no payload never
        /// allocates, which is what keeps this channel free for the tree that does not use it; the
        /// tree-wide <c>_payloadsInUse</c> flag is set by the caller once a payload is written, so
        /// the array is also allocated when a payload-carrying tree inserts into a leaf that has not
        /// received one yet.
        /// </summary>
        private void EnsurePayloads(LeafNode leaf, bool hasPayload)
        {
            if (leaf.Payloads == null && (hasPayload || _payloadsInUse))
            {
                leaf.Payloads = new TPayload[Capacity];
            }
        }

        /// <summary>
        /// Adds the specified number of copies to the key, giving it a slot when the key is not
        /// stored yet. The key recorded on a collision is the one already in the tree, so a comparer
        /// that treats two distinct objects as equal keeps the first of them - the same rule a
        /// dictionary keyed by that comparer follows.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is not positive.</exception>
        internal void AddCount(TKey key, int times)
        {
            AddCount(key, times, hasPayload: false, default!);
        }

        /// <summary>
        /// Adds the specified number of copies to the key and attaches <paramref name="payload"/> to
        /// it, giving the key a slot when it is not stored yet. On a collision the stored key and the
        /// stored payload are both kept, so of two keys the comparer deems equal the first one added
        /// owns the slot and everything the caller attached to it - the same rule the key already
        /// follows.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is not positive.</exception>
        internal void AddCount(TKey key, int times, TPayload payload)
        {
            AddCount(key, times, hasPayload: true, payload);
        }

        private void AddCount(TKey key, int times, bool hasPayload, TPayload payload)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), "The number of copies must be positive.");
            }

            if (_root == null)
            {
                _root = new LeafNode();
            }

            var depth = Descend(key);
            var leaf = (LeafNode)_path[depth]!;
            var slot = SlotOf(leaf, key);
            var isNewKey = slot < 0;

            if (isNewKey)
            {
                EnsurePayloads(leaf, hasPayload);

                slot = LowerBound(leaf, key);
                Array.Copy(leaf.Keys, slot, leaf.Keys, slot + 1, leaf.EntryCount - slot);
                Array.Copy(leaf.Counts, slot, leaf.Counts, slot + 1, leaf.EntryCount - slot);
                if (leaf.Payloads != null)
                {
                    Array.Copy(leaf.Payloads, slot, leaf.Payloads, slot + 1, leaf.EntryCount - slot);
                }

                leaf.Keys[slot] = key;
                leaf.Counts[slot] = times;
                if (hasPayload)
                {
                    _payloadsInUse = true;
                    leaf.Payloads![slot] = payload;
                }

                leaf.EntryCount++;
                leaf.KeyCount = leaf.EntryCount;

                for (var level = depth - 1; level >= 0; level--)
                {
                    _path[level]!.KeyCount++;
                }
            }
            else
            {
                leaf.Counts[slot] += times;
            }

            ApplyDelta(depth, times);

            if (leaf.EntryCount > MaxEntries)
            {
                SplitLeaf(depth);
            }
        }

        /// <summary>
        /// Applies a signed change to the copy count of a key that is already stored. The count is
        /// what the rank reads descend on, so this is the call that keeps a caller's own per-key
        /// total - the length of the bucket it attached as the payload - in step with the tree after
        /// the caller mutated that bucket.
        /// </summary>
        /// <returns><c>true</c> when the key was stored and the count was changed.</returns>
        /// <remarks>
        /// Removing a key outright is <see cref="TryRemoveKey"/>'s job: a count that reaches zero
        /// would leave a slot the rank arithmetic can not skip, so it is rejected instead of
        /// accepted and repaired later.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is zero, or would
        /// drive the stored count to zero or below.</exception>
        internal bool AdjustCount(TKey key, int delta)
        {
            if (delta == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delta), "The change must be non-zero.");
            }

            if (_root == null)
            {
                return false;
            }

            var depth = Descend(key);
            var leaf = (LeafNode)_path[depth]!;
            var slot = SlotOf(leaf, key);
            if (slot < 0)
            {
                return false;
            }

            var updated = leaf.Counts[slot] + delta;
            if (updated <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delta), delta, "The copy count must stay positive; remove the key instead.");
            }

            leaf.Counts[slot] = updated;
            ApplyDelta(depth, delta);
            return true;
        }

        /// <summary>
        /// Overwrites the copy count of an existing key.
        /// </summary>
        /// <returns><c>true</c> when the key was present, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is not positive.</exception>
        internal bool TrySetCount(TKey key, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "The number of copies must be positive.");
            }

            if (_root == null)
            {
                return false;
            }

            var depth = Descend(key);
            var leaf = (LeafNode)_path[depth]!;
            var slot = SlotOf(leaf, key);
            if (slot < 0)
            {
                return false;
            }

            ApplyDelta(depth, count - leaf.Counts[slot]);
            leaf.Counts[slot] = count;
            return true;
        }

        /// <summary>
        /// Removes the key and every copy stored for it, then repairs the occupancy invariant from
        /// the leaf upwards.
        /// </summary>
        /// <returns><c>true</c> when the key was present, <c>false</c> otherwise.</returns>
        internal bool TryRemoveKey(TKey key)
        {
            if (_root == null)
            {
                return false;
            }

            var depth = Descend(key);
            var leaf = (LeafNode)_path[depth]!;
            var slot = SlotOf(leaf, key);
            if (slot < 0)
            {
                return false;
            }

            var removed = leaf.Counts[slot];
            var removedTheSmallest = slot == 0;
            var remaining = leaf.EntryCount - 1;

            Array.Copy(leaf.Keys, slot + 1, leaf.Keys, slot, remaining - slot);
            Array.Copy(leaf.Counts, slot + 1, leaf.Counts, slot, remaining - slot);
            if (leaf.Payloads != null)
            {
                Array.Copy(leaf.Payloads, slot + 1, leaf.Payloads, slot, remaining - slot);
                leaf.Payloads[remaining] = default!;
            }

            leaf.Keys[remaining] = default!;
            leaf.Counts[remaining] = 0;
            leaf.EntryCount = remaining;
            leaf.KeyCount = remaining;

            ApplyDelta(depth, -removed);

            for (var level = depth - 1; level >= 0; level--)
            {
                _path[level]!.KeyCount--;
            }

            if (leaf == _root)
            {
                // The root is the whole tree: it is exempt from the occupancy minimum, and an empty
                // root leaf simply means an empty tree.
                if (remaining == 0)
                {
                    _root = null;
                }

                return true;
            }

            if (removedTheSmallest && remaining > 0)
            {
                RetargetSeparators(depth, leaf.Keys[0]);
            }

            if (remaining < MinEntries)
            {
                Rebalance(depth);
            }

            return true;
        }

        /// <summary>
        /// Gets the smallest key.
        /// </summary>
        /// <returns><c>false</c> when the tree is empty.</returns>
        internal bool TryGetFirst(out KeyValuePair<TKey, int> entry)
        {
            var leaf = OutermostLeaf(leftmost: true);
            if (leaf == null)
            {
                entry = default;
                return false;
            }

            entry = new KeyValuePair<TKey, int>(leaf.Keys[0], leaf.Counts[0]);
            return true;
        }

        /// <summary>
        /// Gets the largest key.
        /// </summary>
        /// <returns><c>false</c> when the tree is empty.</returns>
        internal bool TryGetLast(out KeyValuePair<TKey, int> entry)
        {
            var leaf = OutermostLeaf(leftmost: false);
            if (leaf == null)
            {
                entry = default;
                return false;
            }

            var last = leaf.EntryCount - 1;
            entry = new KeyValuePair<TKey, int>(leaf.Keys[last], leaf.Counts[last]);
            return true;
        }

        /// <summary>
        /// A struct enumerator over the entries in ascending key order, walking the leaf chain and
        /// indexing into the node arrays. It exists because <see cref="Ascending"/> is a yield
        /// iterator, and the ordered multimap built on this engine enumerates <em>once per key</em>:
        /// one yield per key is one heap object per key, which is precisely the per-key allocation
        /// this enumerator removes. Being a struct also keeps the sweep a pure array walk, so nothing
        /// is chased through the heap on the way.
        /// </summary>
        internal struct AscendingEnumerator
        {
            private LeafNode? _leaf;
            private int _index;
            private TKey _key;
            private TPayload _payload;
            private int _count;

            internal AscendingEnumerator(LeafNode? first)
            {
                _leaf = first;
                _index = -1;
                _key = default!;
                _payload = default!;
                _count = 0;
            }

            /// <summary>Gets the key the enumerator is positioned on.</summary>
            internal TKey Key => _key;

            /// <summary>Gets the payload attached to the current key, or <c>default</c> when the tree stores none.</summary>
            internal TPayload Payload => _payload;

            /// <summary>Gets the copy count of the current key.</summary>
            internal int Count => _count;

            /// <summary>Advances to the next key.</summary>
            /// <returns><c>false</c> when every key has been visited.</returns>
            internal bool MoveNext()
            {
                _index++;
                while (_leaf != null && _index >= _leaf.EntryCount)
                {
                    _leaf = (LeafNode?)_leaf.Next;
                    _index = 0;
                }

                if (_leaf == null)
                {
                    return false;
                }

                _key = _leaf.Keys[_index];
                _count = _leaf.Counts[_index];
                _payload = _leaf.Payloads == null ? default! : _leaf.Payloads[_index];
                return true;
            }
        }

        /// <summary>
        /// Gets a struct enumerator over the entries in ascending key order. Unlike
        /// <see cref="Ascending"/> it allocates nothing, which is what the per-key enumeration of the
        /// ordered multimap needs.
        /// </summary>
        internal AscendingEnumerator GetAscendingEnumerator()
        {
            return new AscendingEnumerator(OutermostLeaf(leftmost: true));
        }

        /// <summary>
        /// Enumerates the distinct keys in ascending order, each with its copy count. The leaf chain
        /// is followed once, so the sweep reads contiguous arrays instead of re-entering the
        /// branches.
        /// </summary>
        internal IEnumerable<KeyValuePair<TKey, int>> Ascending()
        {
            for (var leaf = OutermostLeaf(leftmost: true); leaf != null; leaf = (LeafNode?)leaf.Next)
            {
                for (var i = 0; i < leaf.EntryCount; i++)
                {
                    yield return new KeyValuePair<TKey, int>(leaf.Keys[i], leaf.Counts[i]);
                }
            }
        }

        /// <summary>
        /// Enumerates the distinct keys in descending order, each with its copy count, by walking the
        /// leaf chain backwards.
        /// </summary>
        internal IEnumerable<KeyValuePair<TKey, int>> Descending()
        {
            for (var leaf = OutermostLeaf(leftmost: false); leaf != null; leaf = (LeafNode?)leaf.Previous)
            {
                for (var i = leaf.EntryCount - 1; i >= 0; i--)
                {
                    yield return new KeyValuePair<TKey, int>(leaf.Keys[i], leaf.Counts[i]);
                }
            }
        }

        /// <summary>
        /// Enumerates the distinct keys that fall inside the given bounds, in ascending order. The
        /// descent stops at the leaf holding the lower bound and the rest is a leaf sweep, so the
        /// cost is O(log n + k) for k reported entries rather than a full traversal.
        /// </summary>
        internal IEnumerable<KeyValuePair<TKey, int>> Range(TKey low, bool lowInclusive, TKey high, bool highInclusive)
        {
            if (_comparer.Compare(low, high) > 0 || _root == null)
            {
                yield break;
            }

            var leaf = (LeafNode)_path[Descend(low)]!;
            for (var i = lowInclusive ? LowerBound(leaf, low) : UpperBound(leaf, low); leaf != null; leaf = (LeafNode?)leaf.Next)
            {
                for (; i < leaf.EntryCount; i++)
                {
                    var againstHigh = _comparer.Compare(leaf.Keys[i], high);
                    if (againstHigh > 0 || (againstHigh == 0 && !highInclusive))
                    {
                        yield break;
                    }

                    yield return new KeyValuePair<TKey, int>(leaf.Keys[i], leaf.Counts[i]);
                }

                i = 0;
            }
        }

        /// <summary>
        /// Gets the key holding the copy at the specified rank, where rank zero is the smallest copy
        /// and rank <c>n - 1</c> the largest. The walk skips whole sub-trees by their cached copy
        /// totals, so it is O(log n).
        /// </summary>
        /// <returns><c>false</c> when <paramref name="rank"/> is outside the stored copies.</returns>
        internal bool TryGetByRank(int rank, out TKey key)
        {
            var node = _root;
            if (node == null || rank < 0 || rank >= node.TotalCount)
            {
                key = default!;
                return false;
            }

            while (true)
            {
                if (node is LeafNode leaf)
                {
                    for (var i = 0; i < leaf.EntryCount; i++)
                    {
                        if (rank < leaf.Counts[i])
                        {
                            key = leaf.Keys[i];
                            return true;
                        }

                        rank -= leaf.Counts[i];
                    }

                    break;
                }

                var branch = (BranchNode)node;
                var child = 0;
                while (rank >= branch.Totals[child])
                {
                    rank -= branch.Totals[child];
                    child++;
                }

                node = branch.Children[child];
            }

            // Unreachable while the cached totals agree with the leaves, which is an invariant
            // Validate() checks.
            key = default!;
            return false;
        }

        /// <summary>
        /// Gets the key holding the copy at the specified rank, the payload attached to that key, and
        /// the rank the copy holds <em>inside</em> that key's own bucket - the offset to hand to the
        /// bucket's order-statistic read. One descent answers all three, which is what makes a
        /// positional read of the ordered multimap a single O(log n) walk rather than a descent plus a
        /// re-descent.
        /// </summary>
        /// <returns><c>false</c> when <paramref name="rank"/> is outside the stored copies.</returns>
        internal bool TryGetByRank(int rank, out TKey key, out TPayload payload, out int offsetInBucket)
        {
            var node = _root;
            if (node == null || rank < 0 || rank >= node.TotalCount)
            {
                key = default!;
                payload = default!;
                offsetInBucket = 0;
                return false;
            }

            while (true)
            {
                if (node is LeafNode leaf)
                {
                    for (var i = 0; i < leaf.EntryCount; i++)
                    {
                        if (rank < leaf.Counts[i])
                        {
                            key = leaf.Keys[i];
                            payload = leaf.Payloads == null ? default! : leaf.Payloads[i];
                            offsetInBucket = rank;
                            return true;
                        }

                        rank -= leaf.Counts[i];
                    }

                    break;
                }

                var branch = (BranchNode)node;
                var child = 0;
                while (rank >= branch.Totals[child])
                {
                    rank -= branch.Totals[child];
                    child++;
                }

                node = branch.Children[child];
            }

            // Unreachable while the cached totals agree with the leaves, which is an invariant
            // Validate() checks.
            key = default!;
            payload = default!;
            offsetInBucket = 0;
            return false;
        }

        /// <summary>
        /// Gets the rank of the first copy of the key: the number of stored copies that sort strictly
        /// below it. The descent accumulates those copies rather than counting them, so it is
        /// O(log n).
        /// </summary>
        /// <returns><c>false</c> when the key is not stored.</returns>
        internal bool TryGetRankOf(TKey key, out int rank)
        {
            rank = 0;
            var node = _root;
            while (true)
            {
                if (node == null)
                {
                    return false;
                }

                if (node is LeafNode leaf)
                {
                    var slot = SlotOf(leaf, key);
                    if (slot < 0)
                    {
                        return false;
                    }

                    for (var i = 0; i < slot; i++)
                    {
                        rank += leaf.Counts[i];
                    }

                    return true;
                }

                var branch = (BranchNode)node;
                var child = ChildIndexFor(branch, key);
                for (var i = 0; i < child; i++)
                {
                    rank += branch.Totals[i];
                }

                node = branch.Children[child];
            }
        }

        /// <summary>
        /// Verifies the B+ invariants: uniform leaf depth, occupancy, strictly increasing keys in and
        /// across leaves, separators that name the true minimum of the sub-tree to their right, the
        /// cached copy totals and key counts, and an unbroken leaf chain.
        /// </summary>
        /// <param name="error">describes the first violation found, or <c>null</c> when valid.</param>
        /// <returns><c>true</c> when the tree is a valid order-statistic B+ tree.</returns>
        internal bool Validate(out string? error)
        {
            _error = null;
            _leafDepth = -1;
            _previousLeaf = null;
            _distinctSeen = 0;
            _copiesSeen = 0;

            if (_root != null)
            {
                Check(_root, 0);
            }

            if (_error == null && _previousLeaf != null && _previousLeaf.Next != null)
            {
                _error = "the last leaf is chained past the end of the tree.";
            }

            if (_error == null && _distinctSeen != Count)
            {
                _error = "the recorded distinct count does not match the keys stored.";
            }

            if (_error == null && _copiesSeen != ElementCount)
            {
                _error = "the recorded copy total does not match the copies stored.";
            }

            error = _error;
            return error == null;
        }

        // ------------------------------------------------------------------ descent
        // Every read that needs no path uses FindLeaf, and every write starts with Descend. The
        // latter fills _path with the root-to-leaf chain and _childAt[level] with the index of
        // _path[level + 1] inside _path[level], so the repair code can walk back up the same arrays.

        private LeafNode? FindLeaf(TKey key)
        {
            var node = _root;
            while (node is BranchNode branch)
            {
                node = branch.Children[ChildIndexFor(branch, key)];
            }

            return (LeafNode?)node;
        }

        private int Descend(TKey key)
        {
            var node = _root!;
            var depth = 0;
            _path[0] = node;
            while (node is BranchNode branch)
            {
                var child = ChildIndexFor(branch, key);
                _childAt[depth] = child;
                node = branch.Children[child];
                depth++;
                _path[depth] = node;
            }

            return depth;
        }

        /// <summary>
        /// Returns the child to descend into: the last one whose separator does not exceed the key,
        /// because a separator is the smallest key of the sub-tree to its right.
        /// </summary>
        private int ChildIndexFor(BranchNode branch, TKey key)
        {
            var low = 0;
            var high = branch.EntryCount - 1;
            while (low < high)
            {
                var middle = low + ((high - low + 1) >> 1);
                if (_comparer.Compare(key, branch.Keys[middle - 1]) >= 0)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low;
        }

        private int LowerBound(LeafNode leaf, TKey key)
        {
            var low = 0;
            var high = leaf.EntryCount;
            while (low < high)
            {
                var middle = low + ((high - low) >> 1);
                if (_comparer.Compare(leaf.Keys[middle], key) < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private int UpperBound(LeafNode leaf, TKey key)
        {
            var low = 0;
            var high = leaf.EntryCount;
            while (low < high)
            {
                var middle = low + ((high - low) >> 1);
                if (_comparer.Compare(leaf.Keys[middle], key) > 0)
                {
                    high = middle;
                }
                else
                {
                    low = middle + 1;
                }
            }

            return low;
        }

        /// <summary>
        /// Returns the slot of the key inside its leaf, or <c>-1</c> when the leaf does not hold it.
        /// </summary>
        private int SlotOf(LeafNode leaf, TKey key)
        {
            var low = 0;
            var high = leaf.EntryCount - 1;
            while (low <= high)
            {
                var middle = low + ((high - low) >> 1);
                var comparison = _comparer.Compare(leaf.Keys[middle], key);
                if (comparison == 0)
                {
                    return middle;
                }

                if (comparison < 0)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return -1;
        }

        private LeafNode? OutermostLeaf(bool leftmost)
        {
            var node = _root;
            if (node == null)
            {
                return null;
            }

            while (node is BranchNode branch)
            {
                node = branch.Children[leftmost ? 0 : branch.EntryCount - 1];
            }

            return (LeafNode)node;
        }

        /// <summary>
        /// Pushes a change of the copy total of the node at <paramref name="depth"/> up to the root,
        /// updating each ancestor's own total and the per-child figure its parent caches.
        /// </summary>
        private void ApplyDelta(int depth, int delta)
        {
            if (delta == 0)
            {
                return;
            }

            for (var level = depth; level >= 0; level--)
            {
                var node = _path[level]!;
                node.TotalCount += delta;
                if (level > 0)
                {
                    ((BranchNode)_path[level - 1]!).Totals[_childAt[level - 1]] += delta;
                }
            }
        }

        /// <summary>
        /// Repairs the separators after the smallest key of the leaf at <paramref name="depth"/> was
        /// removed. The separator naming that leaf has to name its new smallest key; when the leaf
        /// was its parent's leftmost child the parent lost its own minimum too, so the same figure
        /// has to be written one level up, and so on.
        /// </summary>
        private void RetargetSeparators(int depth, TKey newMin)
        {
            for (var level = depth - 1; level >= 0; level--)
            {
                var child = _childAt[level];
                if (child != 0)
                {
                    ((BranchNode)_path[level]!).Keys[child - 1] = newMin;
                    return;
                }
            }
        }

        // ------------------------------------------------------------------ insertion repair

        private void SplitLeaf(int depth)
        {
            var left = (LeafNode)_path[depth]!;
            var keep = (left.EntryCount + 1) >> 1;
            var moved = left.EntryCount - keep;

            var right = new LeafNode();
            Array.Copy(left.Keys, keep, right.Keys, 0, moved);
            Array.Copy(left.Counts, keep, right.Counts, 0, moved);
            Array.Clear(left.Keys, keep, moved);
            Array.Clear(left.Counts, keep, moved);
            if (left.Payloads != null)
            {
                right.Payloads = new TPayload[Capacity];
                Array.Copy(left.Payloads, keep, right.Payloads, 0, moved);
                Array.Clear(left.Payloads, keep, moved);
            }
            left.EntryCount = keep;
            right.EntryCount = moved;
            RecomputeLeaf(left);
            RecomputeLeaf(right);

            right.Next = left.Next;
            right.Previous = left;
            if (left.Next != null)
            {
                ((LeafNode)left.Next).Previous = right;
            }

            left.Next = right;

            InsertIntoParent(depth, right.Keys[0], right);
        }

        private void SplitInternal(int depth)
        {
            var left = (BranchNode)_path[depth]!;
            var keep = left.EntryCount >> 1;
            var moved = left.EntryCount - keep;

            // The separator pushed up is the one that named the child the split hands over, which is
            // precisely the smallest key of the new node's sub-tree.
            var pushed = left.Keys[keep - 1];

            var right = new BranchNode();
            Array.Copy(left.Children, keep, right.Children, 0, moved);
            Array.Copy(left.Totals, keep, right.Totals, 0, moved);
            Array.Copy(left.Keys, keep, right.Keys, 0, moved - 1);
            Array.Clear(left.Children, keep, moved);
            Array.Clear(left.Totals, keep, moved);
            Array.Clear(left.Keys, keep - 1, moved + 1);
            left.EntryCount = keep;
            right.EntryCount = moved;
            RecomputeNode(left);
            RecomputeNode(right);

            InsertIntoParent(depth, pushed, right);
        }

        /// <summary>
        /// Attaches <paramref name="right"/> to the parent of <c>_path[depth]</c>, directly after it,
        /// and writes <paramref name="separator"/> - the smallest key of the new sub-tree - in front
        /// of it. A split moves entries between siblings without adding or dropping any, so only the
        /// shape and the separators change here. A split of the root grows the tree one level.
        /// </summary>
        private void InsertIntoParent(int depth, TKey separator, Node right)
        {
            var child = _path[depth]!;

            if (depth == 0)
            {
                var root = new BranchNode();
                root.Children[0] = child;
                root.Totals[0] = child.TotalCount;
                root.Keys[0] = separator;
                root.Children[1] = right;
                root.Totals[1] = right.TotalCount;
                root.EntryCount = 2;
                RecomputeNode(root);
                _root = root;
                return;
            }

            var parent = (BranchNode)_path[depth - 1]!;
            var at = _childAt[depth - 1] + 1;
            var shifting = parent.EntryCount - at;

            Array.Copy(parent.Children, at, parent.Children, at + 1, shifting);
            Array.Copy(parent.Totals, at, parent.Totals, at + 1, shifting);
            Array.Copy(parent.Keys, at - 1, parent.Keys, at, shifting);
            parent.Keys[at - 1] = separator;
            parent.Children[at] = right;

            // Both siblings changed shape when the split handed entries over, so both cached totals
            // are rewritten rather than only the new child's.
            parent.Totals[at - 1] = child.TotalCount;
            parent.Totals[at] = right.TotalCount;
            parent.EntryCount++;

            if (parent.EntryCount > MaxEntries)
            {
                SplitInternal(depth - 1);
            }
        }

        // ------------------------------------------------------------------ deletion repair

        /// <summary>
        /// Repairs the node at <paramref name="depth"/>, which dropped below the occupancy minimum:
        /// pull an entry from a sibling that can spare one, otherwise merge with a sibling and let
        /// the parent, now one entry shorter, be repaired the same way.
        /// </summary>
        private void Rebalance(int depth)
        {
            var parent = (BranchNode)_path[depth - 1]!;
            var at = _childAt[depth - 1];

            if (at > 0 && parent.Children[at - 1].EntryCount > MinEntries)
            {
                BorrowFromLeft(depth);
                return;
            }

            if (at + 1 < parent.EntryCount && parent.Children[at + 1].EntryCount > MinEntries)
            {
                BorrowFromRight(depth);
                return;
            }

            MergeChildren(depth - 1, at > 0 ? at - 1 : at);
        }

        private void BorrowFromLeft(int depth)
        {
            var parent = (BranchNode)_path[depth - 1]!;
            var at = _childAt[depth - 1];
            var node = _path[depth]!;
            var left = parent.Children[at - 1];

            if (node is LeafNode leaf)
            {
                var donor = (LeafNode)left;
                var from = donor.EntryCount - 1;
                Array.Copy(leaf.Keys, 0, leaf.Keys, 1, leaf.EntryCount);
                Array.Copy(leaf.Counts, 0, leaf.Counts, 1, leaf.EntryCount);
                leaf.Keys[0] = donor.Keys[from];
                leaf.Counts[0] = donor.Counts[from];
                leaf.EntryCount++;

                if (donor.Payloads != null)
                {
                    leaf.Payloads ??= new TPayload[Capacity];
                    Array.Copy(leaf.Payloads, 0, leaf.Payloads, 1, leaf.EntryCount - 1);
                    leaf.Payloads[0] = donor.Payloads[from];
                    donor.Payloads[from] = default!;
                }

                donor.Keys[from] = default!;
                donor.Counts[from] = 0;
                donor.EntryCount = from;

                RecomputeLeaf(donor);
                RecomputeLeaf(leaf);

                // The borrowed key used to be the donor's largest, so it sits below every key the
                // leaf still holds: the separator has to move down to the leaf's new minimum.
                parent.Keys[at - 1] = leaf.Keys[0];
            }
            else
            {
                var branch = (BranchNode)node;
                var donor = (BranchNode)left;
                var moved = donor.Children[donor.EntryCount - 1];

                Array.Copy(branch.Children, 0, branch.Children, 1, branch.EntryCount);
                Array.Copy(branch.Totals, 0, branch.Totals, 1, branch.EntryCount);
                Array.Copy(branch.Keys, 0, branch.Keys, 1, branch.EntryCount - 1);
                branch.Children[0] = moved;
                branch.Totals[0] = moved.TotalCount;

                // What the parent named was the minimum of the sub-tree that used to be first, and
                // that sub-tree is now the node's second child.
                branch.Keys[0] = parent.Keys[at - 1];
                branch.EntryCount++;

                donor.Children[donor.EntryCount - 1] = null!;
                donor.Totals[donor.EntryCount - 1] = 0;
                donor.Keys[donor.EntryCount - 2] = default!;
                donor.EntryCount--;

                parent.Keys[at - 1] = MinKeyOf(moved);
                RecomputeNode(donor);
                RecomputeNode(branch);
            }

            parent.Totals[at - 1] = left.TotalCount;
            parent.Totals[at] = node.TotalCount;
        }

        private void BorrowFromRight(int depth)
        {
            var parent = (BranchNode)_path[depth - 1]!;
            var at = _childAt[depth - 1];
            var node = _path[depth]!;
            var right = parent.Children[at + 1];

            if (node is LeafNode leaf)
            {
                var donor = (LeafNode)right;
                leaf.Keys[leaf.EntryCount] = donor.Keys[0];
                leaf.Counts[leaf.EntryCount] = donor.Counts[0];
                leaf.EntryCount++;

                if (donor.Payloads != null)
                {
                    leaf.Payloads ??= new TPayload[Capacity];
                    leaf.Payloads[leaf.EntryCount - 1] = donor.Payloads[0];
                    Array.Copy(donor.Payloads, 1, donor.Payloads, 0, donor.EntryCount - 1);
                }

                Array.Copy(donor.Keys, 1, donor.Keys, 0, donor.EntryCount - 1);
                Array.Copy(donor.Counts, 1, donor.Counts, 0, donor.EntryCount - 1);
                var tail = donor.EntryCount - 1;
                donor.Keys[tail] = default!;
                donor.Counts[tail] = 0;
                if (donor.Payloads != null)
                {
                    donor.Payloads[tail] = default!;
                }

                donor.EntryCount = tail;

                RecomputeLeaf(donor);
                RecomputeLeaf(leaf);

                parent.Keys[at] = donor.Keys[0];
            }
            else
            {
                var branch = (BranchNode)node;
                var donor = (BranchNode)right;
                var moved = donor.Children[0];

                branch.Children[branch.EntryCount] = moved;
                branch.Totals[branch.EntryCount] = moved.TotalCount;

                // The parent's separator named the handing-over child, so it becomes the separator
                // between the node's old last child and the one moving in.
                branch.Keys[branch.EntryCount - 1] = parent.Keys[at];
                branch.EntryCount++;

                Array.Copy(donor.Children, 1, donor.Children, 0, donor.EntryCount - 1);
                Array.Copy(donor.Totals, 1, donor.Totals, 0, donor.EntryCount - 1);
                Array.Copy(donor.Keys, 1, donor.Keys, 0, donor.EntryCount - 2);
                var last = donor.EntryCount - 1;
                donor.Children[last] = null!;
                donor.Totals[last] = 0;
                donor.Keys[last - 1] = default!;
                donor.EntryCount = last;

                parent.Keys[at] = MinKeyOf(donor);
                RecomputeNode(donor);
                RecomputeNode(branch);
            }

            parent.Totals[at] = node.TotalCount;
            parent.Totals[at + 1] = right.TotalCount;
        }

        /// <summary>
        /// Folds the child after <paramref name="at"/> into the child at <paramref name="at"/> of the
        /// branch at <paramref name="depth"/>, then frees the swallowed slot. The separator that used
        /// to sit between the two becomes an internal separator of the survivor, which is why a merge
        /// of two minimal nodes still fits. When the root is left with a single child it collapses:
        /// keeping it would only add a descent step to every operation.
        /// </summary>
        private void MergeChildren(int depth, int at)
        {
            var parent = (BranchNode)_path[depth]!;
            var left = parent.Children[at];
            var right = parent.Children[at + 1];

            if (left is LeafNode leftLeaf)
            {
                var rightLeaf = (LeafNode)right;
                Array.Copy(rightLeaf.Keys, 0, leftLeaf.Keys, leftLeaf.EntryCount, rightLeaf.EntryCount);
                Array.Copy(rightLeaf.Counts, 0, leftLeaf.Counts, leftLeaf.EntryCount, rightLeaf.EntryCount);
                if (rightLeaf.Payloads != null)
                {
                    leftLeaf.Payloads ??= new TPayload[Capacity];
                    Array.Copy(rightLeaf.Payloads, 0, leftLeaf.Payloads, leftLeaf.EntryCount, rightLeaf.EntryCount);
                }

                leftLeaf.EntryCount += rightLeaf.EntryCount;
                ClearLeaf(rightLeaf);

                leftLeaf.Next = rightLeaf.Next;
                if (rightLeaf.Next != null)
                {
                    ((LeafNode)rightLeaf.Next).Previous = leftLeaf;
                }

                rightLeaf.Next = null;
                rightLeaf.Previous = null;
                RecomputeLeaf(leftLeaf);
            }
            else
            {
                var leftBranch = (BranchNode)left;
                var rightBranch = (BranchNode)right;
                var keep = leftBranch.EntryCount;
                var moved = rightBranch.EntryCount;

                Array.Copy(rightBranch.Children, 0, leftBranch.Children, keep, moved);
                Array.Copy(rightBranch.Totals, 0, leftBranch.Totals, keep, moved);

                // The parent's separator named right's first child, which is now the child sitting
                // directly after the survivor's old last child.
                leftBranch.Keys[keep - 1] = parent.Keys[at];
                Array.Copy(rightBranch.Keys, 0, leftBranch.Keys, keep, moved - 1);

                leftBranch.EntryCount = keep + moved;
                ClearBranch(rightBranch);
                RecomputeNode(leftBranch);
            }

            // The survivor now holds the copies of both nodes, and the parent named it before the
            // slot below is freed, so its cached total has to be rewritten here.
            parent.Totals[at] = left.TotalCount;

            var count = parent.EntryCount;
            Array.Copy(parent.Children, at + 2, parent.Children, at + 1, count - at - 2);
            Array.Copy(parent.Totals, at + 2, parent.Totals, at + 1, count - at - 2);
            Array.Copy(parent.Keys, at + 1, parent.Keys, at, count - at - 2);
            parent.Children[count - 1] = null!;
            parent.Totals[count - 1] = 0;
            parent.Keys[count - 2] = default!;
            parent.EntryCount = count - 1;
            RecomputeNode(parent);

            if (ReferenceEquals(parent, _root))
            {
                if (parent.EntryCount == 1)
                {
                    var newRoot = parent.Children[0];
                    if (newRoot is LeafNode survivor)
                    {
                        survivor.Next = null;
                        survivor.Previous = null;
                    }

                    _root = newRoot;
                }

                return;
            }

            if (parent.EntryCount < MinEntries)
            {
                // The parent sits one step above in the same scratch path, and nothing that happened
                // below it moved it inside its own parent, so the repair just continues upward.
                Rebalance(depth);
            }
        }

        private static TKey MinKeyOf(Node node)
        {
            while (node is BranchNode branch)
            {
                node = branch.Children[0];
            }

            return ((LeafNode)node).Keys[0];
        }

        private static void RecomputeLeaf(LeafNode leaf)
        {
            var total = 0;
            for (var i = 0; i < leaf.EntryCount; i++)
            {
                total += leaf.Counts[i];
            }

            leaf.TotalCount = total;
            leaf.KeyCount = leaf.EntryCount;
        }

        private static void RecomputeNode(BranchNode branch)
        {
            var total = 0;
            var keys = 0;
            for (var i = 0; i < branch.EntryCount; i++)
            {
                total += branch.Totals[i];
                keys += branch.Children[i].KeyCount;
            }

            branch.TotalCount = total;
            branch.KeyCount = keys;
        }

        private static void ClearLeaf(LeafNode leaf)
        {
            Array.Clear(leaf.Keys, 0, leaf.EntryCount);
            Array.Clear(leaf.Counts, 0, leaf.EntryCount);
            if (leaf.Payloads != null)
            {
                Array.Clear(leaf.Payloads, 0, leaf.EntryCount);
            }

            leaf.EntryCount = 0;
            leaf.KeyCount = 0;
            leaf.TotalCount = 0;
        }

        private static void ClearBranch(BranchNode branch)
        {
            Array.Clear(branch.Children, 0, branch.EntryCount);
            Array.Clear(branch.Totals, 0, branch.EntryCount);
            Array.Clear(branch.Keys, 0, branch.EntryCount - 1);
            branch.EntryCount = 0;
            branch.KeyCount = 0;
            branch.TotalCount = 0;
        }

        // ------------------------------------------------------------------ validation

        private void Check(Node node, int level)
        {
            if (node.EntryCount < 1 || node.EntryCount > MaxEntries)
            {
                _error = ReferenceEquals(node, _root)
                    ? "the root holds an out-of-range number of entries."
                    : "a node holds an out-of-range number of entries.";
                return;
            }

            if (!ReferenceEquals(node, _root) && node.EntryCount < MinEntries)
            {
                _error = "a node other than the root is below the occupancy minimum.";
                return;
            }

            if (node is LeafNode leaf)
            {
                CheckLeaf(leaf, level);
                return;
            }

            var branch = (BranchNode)node;
            if (branch.EntryCount < 2)
            {
                _error = "a branch has fewer than two children.";
                return;
            }

            var total = 0;
            var keys = 0;
            for (var i = 0; i < branch.EntryCount; i++)
            {
                var child = branch.Children[i];
                if (child == null)
                {
                    _error = "a branch has a missing child.";
                    return;
                }

                if (child.IsLeaf != branch.Children[branch.EntryCount - 1].IsLeaf)
                {
                    _error = "the children of a branch mix leaves and branches.";
                    return;
                }

                if (i > 0)
                {
                    if (_comparer.Compare(branch.Keys[i - 1], MinKeyOf(child)) != 0)
                    {
                        _error = "a separator does not name the smallest key of its sub-tree.";
                        return;
                    }

                    if (_comparer.Compare(branch.Keys[i - 1], MinKeyOf(branch.Children[i - 1])) <= 0)
                    {
                        _error = "a separator does not exceed the sub-tree to its left.";
                        return;
                    }
                }

                if (branch.Totals[i] != child.TotalCount)
                {
                    _error = "a cached sub-tree total disagrees with the child it names.";
                    return;
                }

                total += branch.Totals[i];
                keys += child.KeyCount;

                Check(child, level + 1);
                if (_error != null)
                {
                    return;
                }
            }

            if (branch.TotalCount != total)
            {
                _error = "a cached total disagrees with the sum of its children.";
                return;
            }

            if (branch.KeyCount != keys)
            {
                _error = "a cached distinct-key count disagrees with the keys stored below.";
            }
        }

        private void CheckLeaf(LeafNode leaf, int level)
        {
            if (_leafDepth >= 0 && level != _leafDepth)
            {
                _error = "the leaves are not all at the same depth.";
                return;
            }

            _leafDepth = level;

            if (_payloadsInUse && leaf.Payloads == null)
            {
                _error = "a leaf of a payload-carrying tree has no payload array.";
                return;
            }

            if (!_payloadsInUse && leaf.Payloads != null)
            {
                _error = "a leaf holds a payload array although the tree stores no payloads.";
                return;
            }

            if (_previousLeaf != null)
            {
                if (!ReferenceEquals(_previousLeaf.Next, leaf))
                {
                    _error = "the leaf chain skips a leaf.";
                    return;
                }

                if (_comparer.Compare(_previousLeaf.Keys[_previousLeaf.EntryCount - 1], leaf.Keys[0]) >= 0)
                {
                    _error = "the leaves are out of order relative to each other.";
                    return;
                }
            }

            for (var i = 0; i < leaf.EntryCount; i++)
            {
                if (i > 0 && _comparer.Compare(leaf.Keys[i - 1], leaf.Keys[i]) >= 0)
                {
                    _error = "the keys of a leaf are not strictly increasing.";
                    return;
                }

                if (leaf.Counts[i] <= 0)
                {
                    _error = "a stored key holds a non-positive number of copies.";
                    return;
                }

                _distinctSeen++;
                _copiesSeen += leaf.Counts[i];
            }

            _previousLeaf = leaf;
        }

        internal abstract class Node
        {
            /// <summary>
            /// The keys of a leaf, or the separators of a branch. A separator at index <c>i</c> is the
            /// smallest key of the child after it, so a branch keeps one separator fewer than it keeps
            /// children.
            /// </summary>
            internal readonly TKey[] Keys = new TKey[Capacity];

            /// <summary>
            /// The number of live entries: keys in a leaf, children in a branch.
            /// </summary>
            internal int EntryCount;

            /// <summary>
            /// The number of distinct keys in this sub-tree.
            /// </summary>
            internal int KeyCount;

            /// <summary>
            /// The number of copies in this sub-tree. A branch keeps the same figure per child in
            /// <see cref="BranchNode.Totals"/>; the pair is what makes rank and select a descent.
            /// </summary>
            internal int TotalCount;

            internal abstract bool IsLeaf { get; }
        }

        internal sealed class LeafNode : Node
        {
            internal readonly int[] Counts = new int[Capacity];

            /// <summary>
            /// The payload of each key, aligned with <see cref="Node.Keys"/> and
            /// <see cref="Counts"/>. Allocated the first time this leaf stores one, so a tree that
            /// carries no payload - <see cref="OrderedMultiList{T}"/>'s - never pays for the array.
            /// </summary>
            internal TPayload[]? Payloads;

            internal Node? Next;

            internal Node? Previous;

            internal override bool IsLeaf => true;
        }

        internal sealed class BranchNode : Node
        {
            internal readonly Node[] Children = new Node[Capacity];

            internal readonly int[] Totals = new int[Capacity];

            internal override bool IsLeaf => false;
        }
    }
}
