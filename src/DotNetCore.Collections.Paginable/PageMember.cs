using System;
using System.Linq;
using DotNetCore.Collections.Paginable.Abstractions;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable {
    /// <summary>
    /// Page member
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct PageMember<T> : IPageMember<T> {
        private readonly T _memberValue;
        private readonly int _offset;
        private readonly int _startIndex;
        private readonly IQueryEntryState<T> _state;

        internal PageMember(T memberValue, int offset, ref int startIndex) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset can not be less than zero.");
            }

            _startIndex = startIndex;
            _memberValue = memberValue;
            _offset = offset;
            _state = null;
        }

        internal PageMember(IQueryEntryState<T> state, int offset, ref int startIndex) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset can not be less than zero.");
            }

            _startIndex = startIndex;
            _memberValue = default(T);
            _offset = offset;
            _state = state;
        }

        /// <summary>
        /// Value of current member
        /// </summary>
        public T Value => _state == null
            ? _memberValue
            : _state.AllValue.ElementAt(_offset);

        /// <summary>
        /// Offset of current member
        /// </summary>
        public int Offset => _offset;

        /// <summary>
        /// Item number of current member
        /// </summary>
        public int ItemNumber => _startIndex + _offset + 1;
    }
}