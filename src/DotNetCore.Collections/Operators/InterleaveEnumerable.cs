using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The backing sequence of the <c>Interleave</c> operator of the P2 layer: it walks several
    /// sequences in turn, one element from each per round, until all of them are exhausted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A source that ends early is dropped from the rotation rather than blocking it, so the
    /// operator does not stall on the shortest input the way a naive round-robin would.
    /// </para>
    /// <para>
    /// This type is a class because the number of enumerators is only known at run time. The
    /// operator still returns a value-type wrapper, so a consumer who walks the result with
    /// <see langword="foreach"/> pays for exactly one array of enumerators.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class InterleaveEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T>[] _sources;

        internal InterleaveEnumerable(IEnumerable<T>[] sources)
        {
            _sources = sources;
        }

        /// <summary>Returns an enumerator that rotates over the sources.</summary>
        /// <returns>An enumerator positioned before the first element.</returns>
        public IEnumerator<T> GetEnumerator() => new InterleaveEnumerator<T>(_sources);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Rotates over the source enumerators. Each pull takes one element from the next source that
    /// still has one; a source that answers <see langword="false"/> is disposed and removed from
    /// the rotation at that moment.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class InterleaveEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T>?[] _enumerators;
        private int _remaining;
        private int _cursor;
        private T _current;

        internal InterleaveEnumerator(IEnumerable<T>[] sources)
        {
            _enumerators = new IEnumerator<T>?[sources.Length];
            for (var i = 0; i < sources.Length; i++)
            {
                _enumerators[i] = sources[i].GetEnumerator();
            }

            _remaining = sources.Length;
            _cursor = 0;
            _current = default!;
        }

        /// <summary>Gets the current element.</summary>
        public T Current => _current;

        object IEnumerator.Current => _current!;

        /// <summary>Advances to the next element of the rotation.</summary>
        /// <returns><see langword="true"/> if an element was produced; otherwise
        /// <see langword="false"/>.</returns>
        public bool MoveNext()
        {
            while (_remaining > 0)
            {
                for (var step = 0; step < _enumerators.Length; step++)
                {
                    var index = (_cursor + step) % _enumerators.Length;
                    var enumerator = _enumerators[index];
                    if (enumerator is null)
                    {
                        continue;
                    }

                    if (enumerator.MoveNext())
                    {
                        _current = enumerator.Current;
                        _cursor = (index + 1) % _enumerators.Length;
                        return true;
                    }

                    // This source is finished. Retire it so the rotation never asks it again.
                    enumerator.Dispose();
                    _enumerators[index] = null;
                    _remaining--;
                }
            }

            return false;
        }

        /// <summary>Sets the enumerator to its initial position.</summary>
        /// <exception cref="NotSupportedException">The rotation cannot be restarted, because the
        /// sources it wraps have already been consumed.</exception>
        public void Reset() => throw new NotSupportedException("An interleaved sequence cannot be reset.");

        /// <summary>Releases every source enumerator that is still open.</summary>
        public void Dispose()
        {
            for (var i = 0; i < _enumerators.Length; i++)
            {
                var enumerator = _enumerators[i];
                if (enumerator != null)
                {
                    enumerator.Dispose();
                    _enumerators[i] = null;
                }
            }

            _remaining = 0;
        }
    }
}
