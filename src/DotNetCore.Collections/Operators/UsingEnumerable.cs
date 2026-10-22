using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// <c>Using(resourceFactory, sequenceFactory)</c>: binds the lifetime of a resource to the
    /// lifetime of the enumeration of a sequence built from it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The resource is created when the enumeration starts, not when the operator call is made, and
    /// it is released when the enumeration ends - whether that end is a full walk, an early
    /// <c>break</c>, or a failure. This is the sequence-level counterpart of
    /// <see langword="using"/>, for the case where the sequence outlives the statement that
    /// created it.
    /// </para>
    /// <para>
    /// If building the sequence or taking its enumerator throws, the resource is released before
    /// the exception continues, so no failure path can leak it. The release of the resource happens
    /// in a <see langword="finally"/>, which means a failing source enumerator cannot stop it.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TResource">The type of the resource being scoped.</typeparam>
    internal sealed class UsingEnumerable<T, TResource> : IEnumerable<T>
        where TResource : IDisposable
    {
        private readonly Func<TResource> _resourceFactory;
        private readonly Func<TResource, IEnumerable<T>> _sequenceFactory;

        internal UsingEnumerable(Func<TResource> resourceFactory, Func<TResource, IEnumerable<T>> sequenceFactory)
        {
            _resourceFactory = resourceFactory;
            _sequenceFactory = sequenceFactory;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var resource = _resourceFactory();
            if (resource is null)
            {
                throw new InvalidOperationException("The resource factory returned null.");
            }

            IEnumerator<T>? enumerator = null;
            try
            {
                var sequence = _sequenceFactory(resource);
                if (sequence is null)
                {
                    throw new InvalidOperationException("The sequence factory returned null.");
                }

                enumerator = sequence.GetEnumerator();
            }
            catch
            {
                resource.Dispose();
                throw;
            }

            return new UsingEnumerator<T, TResource>(enumerator, resource);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Releases the source enumerator and then the resource, and does it once.
    /// </summary>
    /// <remarks>
    /// The resource release sits in a <see langword="finally"/> so that a source whose
    /// <see cref="IDisposable.Dispose"/> throws still gets the resource released; the source's
    /// exception is the one that escapes, which keeps the diagnosis pointing at the failing
    /// sequence rather than at its finaliser.
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TResource">The type of the resource being scoped.</typeparam>
    internal sealed class UsingEnumerator<T, TResource> : IEnumerator<T>
        where TResource : IDisposable
    {
        private readonly IEnumerator<T> _enumerator;
        private readonly TResource _resource;
        private bool _disposed;

        internal UsingEnumerator(IEnumerator<T> enumerator, TResource resource)
        {
            _enumerator = enumerator;
            _resource = resource;
            _disposed = false;
        }

        public T Current => _enumerator.Current;

        object IEnumerator.Current => _enumerator.Current!;

        public bool MoveNext() => _enumerator.MoveNext();

        public void Reset() => _enumerator.Reset();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _enumerator.Dispose();
            }
            finally
            {
                _resource.Dispose();
            }
        }
    }
}
