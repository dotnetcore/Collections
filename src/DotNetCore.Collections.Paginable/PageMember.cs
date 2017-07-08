using System;
using System.Linq;
using DotNetCore.Collections.Paginable.Internal;

// ReSharper disable InconsistentNaming
namespace DotNetCore.Collections.Paginable
{
    public struct PageMember<T> : IPageMember<T>
    {
        private T m_memberValue { get; }
        private int m_offset { get; }
        private QueryEntryState<T> m_state { get; }

        internal PageMember(T memberValue, int offset)
        {
            if (offset < 0)
            {
                throw new IndexOutOfRangeException("offset can not be less than zero.");
            }

            m_memberValue = memberValue;
            m_offset = offset;
            m_state = null;
        }

        internal PageMember(QueryEntryState<T> state, int offset)
        {
            if (offset < 0)
            {
                throw new IndexOutOfRangeException("offset can not be less than zero.");
            }

            m_memberValue = default(T);
            m_offset = offset;
            m_state = state;
        }

        public T Value => m_state == null

            ? m_memberValue
            : m_state.SolidMembers().Skip(m_offset).First();

        public int Offset => m_offset;

        public int ItemNumber => m_offset + 1;
    }
}
