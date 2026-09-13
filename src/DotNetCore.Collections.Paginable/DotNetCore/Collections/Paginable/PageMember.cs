using System;
using System.Linq;
using DotNetCore.Collections.Paginable.Abstractions;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Page member
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct PageMember<T> : IPageMember<T>
    {
        private readonly T _memberValue;
        private readonly int _offset;
        private readonly int _startIndex;
        // null for the value-backed variant, set for the query-state-backed one; Value
        // branches on it, so the two shapes share one struct.
        private readonly IQueryEntryState<T>? _state;

        internal PageMember(T memberValue, int offset, ref int startIndex)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset can not be less than zero.");
            _startIndex = startIndex;
            _memberValue = memberValue;
            _offset = offset;
            _state = null;
        }

        internal PageMember(IQueryEntryState<T> state, int offset, ref int startIndex)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset can not be less than zero.");
            _startIndex = startIndex;
            // This variant answers Value from the state; _memberValue is never read.
            _memberValue = default!;
            _offset = offset;
            _state = state;
        }

        /// <inheritdoc />
        public T Value => _state is null
            ? _memberValue
            : _state.AllValues.ElementAt(_offset);

        /// <inheritdoc />
        public int Offset => _offset;

        /// <inheritdoc />
        public int ItemNumber => _startIndex + _offset + 1;
    }
}