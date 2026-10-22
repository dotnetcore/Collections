using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// <c>Repeat(source, count)</c>: yields the whole source <c>count</c> times, one round after
    /// another.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The BCL's <see cref="System.Linq.Enumerable.Repeat{TResult}(TResult,int)"/> repeats one
    /// element; this repeats a sequence, which is the shape a test fixture or a synthetic
    /// throughput stream actually needs.
    /// </para>
    /// <para>
    /// Each round asks the source for a fresh enumerator, because an enumerator is single-use. A
    /// source that can only be walked once therefore yields one meaningful round and empty rounds
    /// after it - the operator does not and cannot detect that, and buffering the first round to
    /// replay it would change the operator from streaming to materialising.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class RepeatEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly int _count;

        internal RepeatEnumerable(IEnumerable<T> source, int count)
        {
            _source = source;
            _count = count;
        }

        public IEnumerator<T> GetEnumerator() => new RepeatEnumerator<T>(_source, _count);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Wraps one source enumerator at a time and replaces it when it runs out, until the requested
    /// number of rounds has been completed.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class RepeatEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly int _count;
        private IEnumerator<T>? _enumerator;
        private int _completedRounds;
        private T _current;

        internal RepeatEnumerator(IEnumerable<T> source, int count)
        {
            _source = source;
            _count = count;
            _enumerator = null;
            _completedRounds = 0;
            _current = default!;
        }

        public T Current => _current;

        object IEnumerator.Current => _current!;

        public bool MoveNext()
        {
            while (true)
            {
                if (_enumerator is null)
                {
                    if (_completedRounds >= _count)
                    {
                        return false;
                    }

                    _enumerator = _source.GetEnumerator();
                }

                if (_enumerator.MoveNext())
                {
                    _current = _enumerator.Current;
                    return true;
                }

                // One round is over. The release is an ordinary one: this operator is not a
                // recovery operator, so a source that fails to release itself is reported rather
                // than quietly dropped.
                _enumerator.Dispose();
                _enumerator = null;
                _completedRounds++;
            }
        }

        public void Reset() => throw new NotSupportedException("A repeated sequence cannot be reset.");

        public void Dispose()
        {
            if (_enumerator != null)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }
        }
    }
}
