using System.Collections.Generic;

namespace DotNetCore.Collections.Internal
{
    /// <summary>
    /// The one-sided hash lookup the outer joins are built on: it folds one of the two sequences
    /// into groups keyed by the join key and then answers "which elements sit under this key?" in
    /// constant time, which is what turns a nested-loop join into a hash join.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The type exists because of a nullability detail rather than a performance one.
    /// <see cref="Dictionary{TKey,TValue}"/> declares <c>where TKey : notnull</c> on the modern
    /// targets, and the join operators must not: the BCL's own lookup is unconstrained, so
    /// constraining ours would reject a nullable key the platform accepts. The buckets are
    /// therefore keyed by the <see cref="int"/> hash and the keys themselves live in a plain
    /// <see cref="List{T}"/>, which carries no such constraint.
    /// </para>
    /// <para>
    /// A null key is never stored. That is not a shortcut: <c>Lookup.CreateForJoin</c>, which is
    /// what <c>System.Linq.Enumerable.LeftJoin</c> / <c>RightJoin</c> build, drops an element whose
    /// key is null as well, so a null-keyed element on the lookup side can never match anything.
    /// The structure itself would cope - <see cref="HashOf"/> maps a null key to bucket zero and
    /// <see cref="List{T}"/> happily holds one - so the rule is enforced by the callers that need
    /// it and skipped by the ones that do not. <c>FullGroupJoin</c> is the latter: it groups the way
    /// <c>GroupBy</c> does, where a null key forms a group of its own.
    /// </para>
    /// <para>
    /// Elements keep the order they were added in, so a group hands them back in source order -
    /// the order the BCL's lookup hands them back in.
    /// </para>
    /// <para>
    /// Since 7.3.0 the type is compiled on every target rather than only below .NET 10: the full
    /// joins of the P2 layer need it on all of them, because no TFM in the matrix ships one.
    /// </para>
    /// </remarks>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TValue">The type of the elements being grouped.</typeparam>
    internal sealed class JoinLookup<TKey, TValue>
    {
        private readonly IEqualityComparer<TKey> _comparer;
        private readonly Dictionary<int, List<int>> _buckets;
        private readonly List<TKey> _keys;
        private readonly List<List<TValue>> _groups;

        internal JoinLookup(IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
            _buckets = new Dictionary<int, List<int>>();
            _keys = new List<TKey>();
            _groups = new List<List<TValue>>();
        }

        /// <summary>Adds <paramref name="value"/> to the group of <paramref name="key"/>, creating
        /// the group when the key is new.</summary>
        /// <param name="key">The join key, or <see langword="null"/> to open a group for the null
        /// key.</param>
        /// <param name="value">The element to group.</param>
        internal void Add(TKey key, TValue value)
        {
            var hash = HashOf(key);

            if (_buckets.TryGetValue(hash, out var bucket))
            {
                for (var i = 0; i < bucket.Count; i++)
                {
                    var index = bucket[i];
                    if (_comparer.Equals(_keys[index], key))
                    {
                        _groups[index].Add(value);
                        return;
                    }
                }
            }
            else
            {
                bucket = new List<int>();
                _buckets.Add(hash, bucket);
            }

            _keys.Add(key);
            _groups.Add(new List<TValue> { value });
            bucket.Add(_keys.Count - 1);
        }

        /// <summary>Looks up the group of <paramref name="key"/>.</summary>
        /// <param name="key">The join key, or <see langword="null"/> to look up the null-key
        /// group.</param>
        /// <param name="group">The elements under the key, in the order they were added, when the
        /// return value is <see langword="true"/>; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> when the key was found.</returns>
        internal bool TryGetGroup(TKey key, out List<TValue>? group)
        {
            var hash = HashOf(key);

            if (_buckets.TryGetValue(hash, out var bucket))
            {
                for (var i = 0; i < bucket.Count; i++)
                {
                    var index = bucket[i];
                    if (_comparer.Equals(_keys[index], key))
                    {
                        group = _groups[index];
                        return true;
                    }
                }
            }

            group = null;
            return false;
        }

        /// <summary>
        /// Maps a key to a non-negative hash. A null key is mapped to zero rather than handed to
        /// the comparer, because <see cref="IEqualityComparer{T}.GetHashCode(T)"/> is annotated
        /// <c>[DisallowNull]</c> and some implementations throw when they are passed one. This is
        /// the guard the BCL's own lookup carries as well.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The bucket hash of <paramref name="key"/>.</returns>
        private int HashOf(TKey key) => key is null ? 0 : _comparer.GetHashCode(key) & 0x7FFFFFFF;
    }
}
