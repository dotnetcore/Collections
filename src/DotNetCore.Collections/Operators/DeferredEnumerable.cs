using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// <c>Defer(factory)</c>: builds the sequence when it is enumerated instead of when it is
    /// described.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The operator exists so that a pipeline can be handed around before the data it will read is
    /// known - the classic case being a query over a session or connection that is opened later.
    /// The factory runs once per enumeration, so enumerating the result twice observes the world
    /// twice; that is the difference between deferring a sequence and caching one.
    /// </para>
    /// <para>
    /// A factory that answers <see langword="null"/> is reported as
    /// <see cref="InvalidOperationException"/> at enumeration time rather than swallowed, because a
    /// silent empty sequence is indistinguishable from "this query returned nothing" - the exact
    /// confusion the operator is meant to prevent.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class DeferredEnumerable<T> : IEnumerable<T>
    {
        private readonly Func<IEnumerable<T>> _factory;

        internal DeferredEnumerable(Func<IEnumerable<T>> factory) => _factory = factory;

        public IEnumerator<T> GetEnumerator()
        {
            var sequence = _factory();
            if (sequence is null)
            {
                throw new InvalidOperationException("The deferred sequence factory returned null.");
            }

            return sequence.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
