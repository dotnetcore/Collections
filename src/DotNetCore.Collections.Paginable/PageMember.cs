using System;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

namespace DotNetCore.Collections.Paginable
{
    /// <summary>
    /// Page member
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct PageMember<T> : IPageMember<T>
    {
        private T m_memberValue { get; }
        private int m_offset { get; }
        private int m_startIndex { get; }
        private QueryEntryState<T> m_state { get; }

        internal PageMember(T memberValue, int offset, int startIndex)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset can not be less than zero.");
            }

            m_startIndex = startIndex;
            m_memberValue = memberValue;
            m_offset = offset;
            m_state = null;
        }

        internal PageMember(QueryEntryState<T> state, int offset, int startIndex)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "offset can not be less than zero.");
            }

            m_startIndex = startIndex;
            m_memberValue = default(T);
            m_offset = offset;
            m_state = state;
        }

        /// <summary>
        /// Value of current member
        /// </summary>
        public T Value => m_state == null

            ? m_memberValue
            : m_state.AllValue.ElementAt(m_offset);

        /// <summary>
        /// Offset of current member
        /// </summary>
        public int Offset => m_offset;

        /// <summary>
        /// Item number of current member
        /// </summary>
        public int ItemNumber => m_startIndex + m_offset + 1;
    }
}
