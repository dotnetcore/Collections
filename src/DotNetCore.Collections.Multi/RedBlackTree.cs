using System;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// A left-leaning red-black tree (Sedgewick) that maps every distinct key to the number of
    /// copies stored for it. This is the storage engine of <see cref="OrderedMultiList{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each distinct key is held in exactly one node together with its copy count, so the tree is
    /// sized by <em>distinct</em> elements rather than by total copies. Search, insertion and
    /// deletion run in O(log n) worst case, which the red-black invariants guarantee: the root is
    /// black, no red node has a red child, and both sides of every node have the same black
    /// height. The left-leaning variant adds a fourth rule - no red link may lean to the right -
    /// which keeps insertion and deletion short enough to verify by inspection.
    /// </para>
    /// <para>
    /// <see cref="Validate"/> re-checks every invariant (and the binary-search ordering), and
    /// <see cref="Height"/> reports the depth. Together they let the O(log n) claim be asserted
    /// as <c>height &lt;= 2 * log2(n + 1)</c> rather than inferred from timings, which is what
    /// the F6-01 acceptance criteria ask for.
    /// </para>
    /// <para>
    /// The type is internal and deliberately narrow: it owns the tree and the copy counts and
    /// knows nothing about the collecting type's bookkeeping beyond <see cref="Count"/>. It is
    /// not thread-safe, exactly like the collection built on top of it.
    /// </para>
    /// </remarks>
    internal sealed class RedBlackTree<TKey>
    {
        private readonly IComparer<TKey> _comparer;
        private Node? _root;

        /// <summary>
        /// Initializes an empty tree that orders keys with the specified comparer.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="comparer"/> is <c>null</c>.</exception>
        internal RedBlackTree(IComparer<TKey> comparer)
        {
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// Gets the number of distinct keys currently stored.
        /// </summary>
        internal int Count { get; private set; }

        /// <summary>
        /// Gets the number of edges on the longest root-to-leaf path; zero for an empty tree.
        /// This walks the whole tree, so it is O(n) - it exists for invariant assertions, not for
        /// everyday use.
        /// </summary>
        internal int Height => HeightOf(_root);

        /// <summary>
        /// Removes every key.
        /// </summary>
        internal void Clear()
        {
            _root = null;
            Count = 0;
        }

        /// <summary>
        /// Gets the copy count stored for the key.
        /// </summary>
        /// <returns><c>true</c> when the key is present, <c>false</c> otherwise.</returns>
        internal bool TryGetCount(TKey key, out int count)
        {
            var node = Find(key);
            if (node == null)
            {
                count = 0;
                return false;
            }

            count = node.Count;
            return true;
        }

        /// <summary>
        /// Adds the specified number of copies to the key, creating its node when the key is not
        /// stored yet. The key recorded on a collision is the one already in the tree, so a
        /// comparer that treats two distinct objects as equal keeps the first of them - the same
        /// rule a dictionary keyed by that comparer follows.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="times"/> is not positive.</exception>
        internal void AddCount(TKey key, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(times), "The number of copies must be positive.");
            }

            _root = Add(_root, key, times, out var created);
            if (_root != null)
            {
                _root.IsRed = false;
            }

            if (created)
            {
                Count++;
            }
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

            var node = Find(key);
            if (node == null)
            {
                return false;
            }

            node.Count = count;
            return true;
        }

        /// <summary>
        /// Removes the key and every copy stored for it.
        /// </summary>
        /// <returns><c>true</c> when the key was present, <c>false</c> otherwise.</returns>
        internal bool TryRemoveKey(TKey key)
        {
            if (Find(key) == null)
            {
                return false;
            }

            _root = Delete(_root, key);
            if (_root != null)
            {
                _root.IsRed = false;
            }

            Count--;
            return true;
        }

        /// <summary>
        /// Gets the smallest key.
        /// </summary>
        /// <returns><c>false</c> when the tree is empty.</returns>
        internal bool TryGetFirst(out KeyValuePair<TKey, int> entry)
        {
            if (_root == null)
            {
                entry = default;
                return false;
            }

            var node = _root;
            while (node.Left != null)
            {
                node = node.Left;
            }

            entry = new KeyValuePair<TKey, int>(node.Key, node.Count);
            return true;
        }

        /// <summary>
        /// Gets the largest key.
        /// </summary>
        /// <returns><c>false</c> when the tree is empty.</returns>
        internal bool TryGetLast(out KeyValuePair<TKey, int> entry)
        {
            if (_root == null)
            {
                entry = default;
                return false;
            }

            var node = _root;
            while (node.Right != null)
            {
                node = node.Right;
            }

            entry = new KeyValuePair<TKey, int>(node.Key, node.Count);
            return true;
        }

        /// <summary>
        /// Enumerates the distinct keys in ascending order, each with its copy count.
        /// </summary>
        internal IEnumerable<KeyValuePair<TKey, int>> Ascending()
        {
            var stack = new Stack<Node>();
            var node = _root;
            while (node != null || stack.Count > 0)
            {
                while (node != null)
                {
                    stack.Push(node);
                    node = node.Left;
                }

                node = stack.Pop();
                yield return new KeyValuePair<TKey, int>(node.Key, node.Count);
                node = node.Right;
            }
        }

        /// <summary>
        /// Enumerates the distinct keys in descending order, each with its copy count.
        /// </summary>
        internal IEnumerable<KeyValuePair<TKey, int>> Descending()
        {
            var stack = new Stack<Node>();
            var node = _root;
            while (node != null || stack.Count > 0)
            {
                while (node != null)
                {
                    stack.Push(node);
                    node = node.Right;
                }

                node = stack.Pop();
                yield return new KeyValuePair<TKey, int>(node.Key, node.Count);
                node = node.Left;
            }
        }

        /// <summary>
        /// Enumerates the distinct keys that fall inside the given bounds, in ascending order.
        /// Subtrees that lie entirely outside the bounds are skipped, so the cost is
        /// O(log n + k) for k reported entries rather than a full traversal.
        /// </summary>
        internal IEnumerable<KeyValuePair<TKey, int>> Range(TKey low, bool lowInclusive, TKey high, bool highInclusive)
        {
            if (_comparer.Compare(low, high) > 0)
            {
                yield break;
            }

            foreach (var entry in RangeCore(_root, low, lowInclusive, high, highInclusive))
            {
                yield return entry;
            }
        }

        /// <summary>
        /// Verifies the red-black invariants, the binary-search ordering and the node count.
        /// </summary>
        /// <param name="error">describes the first violation found, or <c>null</c> when valid.</param>
        /// <returns><c>true</c> when the tree is a valid left-leaning red-black tree.</returns>
        internal bool Validate(out string? error)
        {
            error = null;

            if (Count != CountNodes(_root))
            {
                error = "the recorded distinct count does not match the number of nodes.";
                return false;
            }

            if (_root == null)
            {
                return true;
            }

            if (_root.IsRed)
            {
                error = "the root is red; a red-black tree requires a black root.";
                return false;
            }

            CheckColours(_root, ref error);
            CheckOrdering(ref error);
            return error == null;
        }

        private Node? Find(TKey key)
        {
            var node = _root;
            while (node != null)
            {
                var comparison = _comparer.Compare(key, node.Key);
                if (comparison == 0)
                {
                    return node;
                }

                node = comparison < 0 ? node.Left : node.Right;
            }

            return null;
        }

        private Node Add(Node? node, TKey key, int times, out bool created)
        {
            if (node == null)
            {
                created = true;
                return new Node(key, isRed: true) { Count = times };
            }

            var comparison = _comparer.Compare(key, node.Key);
            if (comparison == 0)
            {
                node.Count += times;
                created = false;
                return node;
            }

            if (comparison < 0)
            {
                node.Left = Add(node.Left, key, times, out created);
            }
            else
            {
                node.Right = Add(node.Right, key, times, out created);
            }

            return Balance(node);
        }

        private Node? Delete(Node? node, TKey key)
        {
            if (node == null)
            {
                return null;
            }

            if (_comparer.Compare(key, node.Key) < 0)
            {
                // The key is in the left subtree and that subtree is known to be non-null,
                // because the caller only descends after a successful lookup.
                if (!IsRed(node.Left) && !IsRed(node.Left!.Left))
                {
                    node = MoveRedLeft(node);
                }

                node.Left = Delete(node.Left, key);
            }
            else
            {
                if (IsRed(node.Left))
                {
                    node = RotateRight(node);
                }

                if (_comparer.Compare(key, node.Key) == 0 && node.Right == null)
                {
                    return null;
                }

                if (!IsRed(node.Right) && !IsRed(node.Right!.Left))
                {
                    node = MoveRedRight(node);
                }

                // The comparisons above are re-taken here: the two moves may have rotated a
                // different node into this position.
                if (_comparer.Compare(key, node.Key) == 0)
                {
                    var successor = MinNode(node.Right!);
                    node.Key = successor.Key;
                    node.Count = successor.Count;
                    node.Right = DeleteMin(node.Right!);
                }
                else
                {
                    node.Right = Delete(node.Right, key);
                }
            }

            return Balance(node);
        }

        private Node? DeleteMin(Node? node)
        {
            if (node == null || node.Left == null)
            {
                return null;
            }

            if (!IsRed(node.Left) && !IsRed(node.Left!.Left))
            {
                node = MoveRedLeft(node);
            }

            node.Left = DeleteMin(node.Left);
            return Balance(node);
        }

        private static Node MinNode(Node node)
        {
            while (node.Left != null)
            {
                node = node.Left;
            }

            return node;
        }

        private static Node Balance(Node node)
        {
            if (IsRed(node.Right) && !IsRed(node.Left))
            {
                node = RotateLeft(node);
            }

            if (IsRed(node.Left) && IsRed(node.Left!.Left))
            {
                node = RotateRight(node);
            }

            if (IsRed(node.Left) && IsRed(node.Right))
            {
                FlipColours(node);
            }

            return node;
        }

        private static Node MoveRedLeft(Node node)
        {
            FlipColours(node);
            if (IsRed(node.Right!.Left))
            {
                node.Right = RotateRight(node.Right!);
                node = RotateLeft(node);
                FlipColours(node);
            }

            return node;
        }

        private static Node MoveRedRight(Node node)
        {
            FlipColours(node);
            if (IsRed(node.Left!.Left))
            {
                node = RotateRight(node);
                FlipColours(node);
            }

            return node;
        }

        private static Node RotateLeft(Node node)
        {
            var pivot = node.Right!;
            node.Right = pivot.Left;
            pivot.Left = node;
            pivot.IsRed = node.IsRed;
            node.IsRed = true;
            return pivot;
        }

        private static Node RotateRight(Node node)
        {
            var pivot = node.Left!;
            node.Left = pivot.Right;
            pivot.Right = node;
            pivot.IsRed = node.IsRed;
            node.IsRed = true;
            return pivot;
        }

        // Both children are required: every call site is guarded by a check that they exist,
        // exactly as in the published left-leaning red-black tree algorithms.
        private static void FlipColours(Node node)
        {
            node.IsRed = !node.IsRed;
            node.Left!.IsRed = !node.Left.IsRed;
            node.Right!.IsRed = !node.Right.IsRed;
        }

        private static bool IsRed(Node? node)
        {
            return node != null && node.IsRed;
        }

        private static int HeightOf(Node? node)
        {
            if (node == null)
            {
                return 0;
            }

            var left = HeightOf(node.Left);
            var right = HeightOf(node.Right);
            return 1 + (left > right ? left : right);
        }

        private static int CountNodes(Node? node)
        {
            if (node == null)
            {
                return 0;
            }

            return 1 + CountNodes(node.Left) + CountNodes(node.Right);
        }

        // Returns the black height of the subtree: the number of black nodes on any path from
        // this node down to a null leaf, counting the null leaf itself as black. A null leaf
        // therefore has height 1.
        private static int CheckColours(Node? node, ref string? error)
        {
            if (node == null)
            {
                return 1;
            }

            if (node.IsRed && (IsRed(node.Left) || IsRed(node.Right)))
            {
                error ??= "a red node has a red child.";
            }

            if (IsRed(node.Right) && !IsRed(node.Left))
            {
                error ??= "a red link leans to the right.";
            }

            var leftHeight = CheckColours(node.Left, ref error);
            var rightHeight = CheckColours(node.Right, ref error);
            if (leftHeight != rightHeight)
            {
                error ??= "the two subtrees of a node have different black heights.";
            }

            return node.IsRed ? leftHeight : leftHeight + 1;
        }

        private void CheckOrdering(ref string? error)
        {
            var previous = default(TKey)!;
            var hasPrevious = false;
            foreach (var entry in Ascending())
            {
                if (hasPrevious && _comparer.Compare(previous, entry.Key) >= 0)
                {
                    error ??= "the in-order sequence is not strictly increasing, so the binary-search ordering is broken.";
                    return;
                }

                previous = entry.Key;
                hasPrevious = true;
            }
        }

        private IEnumerable<KeyValuePair<TKey, int>> RangeCore(Node? node, TKey low, bool lowInclusive, TKey high, bool highInclusive)
        {
            if (node == null)
            {
                yield break;
            }

            var againstLow = _comparer.Compare(node.Key, low);
            var againstHigh = _comparer.Compare(node.Key, high);
            var reachesLow = againstLow > 0 || (lowInclusive && againstLow == 0);
            var reachesHigh = againstHigh < 0 || (highInclusive && againstHigh == 0);

            if (reachesLow)
            {
                foreach (var entry in RangeCore(node.Left, low, lowInclusive, high, highInclusive))
                {
                    yield return entry;
                }
            }

            if (reachesLow && reachesHigh)
            {
                yield return new KeyValuePair<TKey, int>(node.Key, node.Count);
            }

            if (reachesHigh)
            {
                foreach (var entry in RangeCore(node.Right, low, lowInclusive, high, highInclusive))
                {
                    yield return entry;
                }
            }
        }

        private sealed class Node
        {
            internal Node(TKey key, bool isRed)
            {
                Key = key;
                IsRed = isRed;
            }

            internal TKey Key { get; set; }

            internal int Count { get; set; }

            internal bool IsRed { get; set; }

            internal Node? Left { get; set; }

            internal Node? Right { get; set; }
        }
    }
}
