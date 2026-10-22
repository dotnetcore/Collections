using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The recovery half of the P3 layer: the internal machinery behind <c>Catch</c>,
    /// <c>Retry</c> and <c>OnErrorResumeNext</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These operators cannot stay on the value-type enumerator path the rest of the engine uses.
    /// Catching a failure means holding an enumerator whose walk may throw, switching to a second
    /// enumerator when it does, and re-creating the first one on a retry - all of which is
    /// reference state by nature. The wrappers are therefore classes, and the four public arms are
    /// convenience shims over one boxed source, the same trade-off <c>Interleave</c> documents.
    /// </para>
    /// <para>
    /// Every enumerator below recovers from failures of <c>GetEnumerator</c> and <c>MoveNext</c>
    /// only. A failure raised by <c>Current</c> while reading an element propagates: at that point
    /// the element itself is unusable, and pretending otherwise would hand the consumer a
    /// <see langword="default"/> value it never asked for.
    /// </para>
    /// </remarks>
    internal static class FaultRecovery
    {
        /// <summary>Disposes an enumerator that is being abandoned, swallowing whatever the
        /// release itself throws.</summary>
        /// <remarks>
        /// A recovery operator's contract is "the failure of one sequence does not stop the walk".
        /// A release that throws would break that contract from the outside, on a sequence that has
        /// already been abandoned, so the exception is dropped. This is the one place in the engine
        /// where a swallowed exception is the design rather than an oversight.
        /// </remarks>
        internal static void Release<T>(IEnumerator<T>? enumerator)
        {
            if (enumerator is null)
            {
                return;
            }

            try
            {
                enumerator.Dispose();
            }
            catch
            {
                // See the remarks: releasing an abandoned sequence must not fault the pipeline.
            }
        }
    }

    /// <summary>
    /// <c>Catch(first, second)</c>: yields <c>first</c> until it fails, then yields
    /// <c>second</c> in full. A source that ends without failing is not followed by the fallback -
    /// the operator is about failures, not about concatenation.
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class CatchEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly IEnumerable<T> _fallback;

        internal CatchEnumerable(IEnumerable<T> source, IEnumerable<T> fallback)
        {
            _source = source;
            _fallback = fallback;
        }

        public IEnumerator<T> GetEnumerator() => new CatchEnumerator<T>(_source, _fallback);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class CatchEnumerator<T> : IEnumerator<T>
    {
        private const int ModeSource = 0;
        private const int ModeFallback = 1;
        private const int ModeDone = 2;

        private readonly IEnumerable<T> _source;
        private readonly IEnumerable<T> _fallback;
        private IEnumerator<T>? _enumerator;
        private int _mode;
        private T _current;

        internal CatchEnumerator(IEnumerable<T> source, IEnumerable<T> fallback)
        {
            _source = source;
            _fallback = fallback;
            _enumerator = null;
            _mode = ModeSource;
            _current = default!;
        }

        public T Current => _current;

        object IEnumerator.Current => _current!;

        public bool MoveNext()
        {
            while (true)
            {
                switch (_mode)
                {
                    case ModeSource:
                        if (_enumerator is null)
                        {
                            try
                            {
                                _enumerator = _source.GetEnumerator();
                            }
                            catch
                            {
                                _enumerator = null;
                                _mode = ModeFallback;
                                continue;
                            }
                        }

                        bool moved;
                        try
                        {
                            moved = _enumerator.MoveNext();
                        }
                        catch
                        {
                            FaultRecovery.Release(_enumerator);
                            _enumerator = null;
                            _mode = ModeFallback;
                            continue;
                        }

                        if (moved)
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        // A clean end is not a failure: the fallback stays unused.
                        FaultRecovery.Release(_enumerator);
                        _enumerator = null;
                        _mode = ModeDone;
                        return false;

                    case ModeFallback:
                        if (_enumerator is null)
                        {
                            _enumerator = _fallback.GetEnumerator();
                        }

                        if (_enumerator.MoveNext())
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        _mode = ModeDone;
                        return false;

                    default:
                        return false;
                }
            }
        }

        public void Reset() => throw new NotSupportedException("A recovered sequence cannot be reset.");

        public void Dispose()
        {
            FaultRecovery.Release(_enumerator);
            _enumerator = null;
            _mode = ModeDone;
        }
    }

    /// <summary>
    /// The handler form of <c>Catch</c>: the caller decides what to continue with, given the
    /// exception. A handler that answers <see langword="null"/> ends the sequence quietly, which is
    /// how a consumer says "swallow this one".
    /// </summary>
    /// <typeparam name="T">The type of the elements.</typeparam>
    /// <typeparam name="TException">The exception type the caller is prepared to handle.</typeparam>
    internal sealed class CatchWithHandlerEnumerable<T, TException> : IEnumerable<T>
        where TException : Exception
    {
        private readonly IEnumerable<T> _source;
        private readonly Func<TException, IEnumerable<T>?> _handler;

        internal CatchWithHandlerEnumerable(IEnumerable<T> source, Func<TException, IEnumerable<T>?> handler)
        {
            _source = source;
            _handler = handler;
        }

        public IEnumerator<T> GetEnumerator() => new CatchWithHandlerEnumerator<T, TException>(_source, _handler);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class CatchWithHandlerEnumerator<T, TException> : IEnumerator<T>
        where TException : Exception
    {
        private const int ModeSource = 0;
        private const int ModeHandler = 1;
        private const int ModeDone = 2;

        private readonly IEnumerable<T> _source;
        private readonly Func<TException, IEnumerable<T>?> _handler;
        private IEnumerator<T>? _enumerator;
        private int _mode;
        private T _current;

        internal CatchWithHandlerEnumerator(IEnumerable<T> source, Func<TException, IEnumerable<T>?> handler)
        {
            _source = source;
            _handler = handler;
            _enumerator = null;
            _mode = ModeSource;
            _current = default!;
        }

        public T Current => _current;

        object IEnumerator.Current => _current!;

        public bool MoveNext()
        {
            while (true)
            {
                switch (_mode)
                {
                    case ModeSource:
                        if (_enumerator is null)
                        {
                            try
                            {
                                _enumerator = _source.GetEnumerator();
                            }
                            catch (Exception exception) when (exception is TException handled)
                            {
                                _enumerator = null;
                                if (!EnterHandler(handled))
                                {
                                    return false;
                                }

                                continue;
                            }
                        }

                        bool moved;
                        try
                        {
                            moved = _enumerator.MoveNext();
                        }
                        catch (Exception exception) when (exception is TException handled)
                        {
                            FaultRecovery.Release(_enumerator);
                            _enumerator = null;
                            if (!EnterHandler(handled))
                            {
                                return false;
                            }

                            continue;
                        }

                        if (moved)
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        FaultRecovery.Release(_enumerator);
                        _enumerator = null;
                        _mode = ModeDone;
                        return false;

                    case ModeHandler:
                        // EnterHandler sets the enumerator before the mode moves, so the field is
                        // non-null here whenever the mode is; the annotation is what tells the
                        // nullable analysis what the state machine already guarantees.
                        if (_enumerator!.MoveNext())
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        _mode = ModeDone;
                        return false;

                    default:
                        return false;
                }
            }
        }

        // Returns false when the handler declined to continue, which ends the sequence. Whatever the
        // handler's own sequence does - including throwing - is its business and propagates.
        private bool EnterHandler(TException exception)
        {
            var next = _handler(exception);
            if (next is null)
            {
                _mode = ModeDone;
                return false;
            }

            _enumerator = next.GetEnumerator();
            _mode = ModeHandler;
            return true;
        }

        public void Reset() => throw new NotSupportedException("A recovered sequence cannot be reset.");

        public void Dispose()
        {
            FaultRecovery.Release(_enumerator);
            _enumerator = null;
            _mode = ModeDone;
        }
    }

    /// <summary>
    /// <c>Retry(source, count)</c>: walks the source, and when it fails re-walks it from the
    /// beginning, up to <c>count</c> further times.
    /// </summary>
    /// <remarks>
    /// A retry restarts the whole sequence, so elements that were already handed over before the
    /// failure are produced a second time. That is inherent to retrying a sequence rather than a
    /// request (there is no way to resume an arbitrary enumerator), and it is why the operator is
    /// documented as a fault-tolerance tool for idempotent walks rather than a general one.
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class RetryEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly int _retries;

        internal RetryEnumerable(IEnumerable<T> source, int retries)
        {
            _source = source;
            _retries = retries;
        }

        public IEnumerator<T> GetEnumerator() => new RetryEnumerator<T>(_source, _retries);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class RetryEnumerator<T> : IEnumerator<T>
    {
        private readonly IEnumerable<T> _source;
        private readonly int _retries;
        private IEnumerator<T>? _enumerator;
        private int _retriesUsed;
        private bool _finished;
        private T _current;

        internal RetryEnumerator(IEnumerable<T> source, int retries)
        {
            _source = source;
            _retries = retries;
            _enumerator = null;
            _retriesUsed = 0;
            _finished = false;
            _current = default!;
        }

        public T Current => _current;

        object IEnumerator.Current => _current!;

        public bool MoveNext()
        {
            while (!_finished)
            {
                if (_enumerator is null)
                {
                    try
                    {
                        _enumerator = _source.GetEnumerator();
                    }
                    catch
                    {
                        if (!BeginRetry())
                        {
                            throw;
                        }

                        continue;
                    }
                }

                bool moved;
                try
                {
                    moved = _enumerator.MoveNext();
                }
                catch
                {
                    FaultRecovery.Release(_enumerator);
                    _enumerator = null;
                    if (!BeginRetry())
                    {
                        throw;
                    }

                    continue;
                }

                if (moved)
                {
                    _current = _enumerator.Current;
                    return true;
                }

                // The source ran out on its own: the walk is over and no retry is due.
                FaultRecovery.Release(_enumerator);
                _enumerator = null;
                _finished = true;
                return false;
            }

            return false;
        }

        private bool BeginRetry()
        {
            if (_retriesUsed >= _retries)
            {
                return false;
            }

            _retriesUsed++;
            return true;
        }

        public void Reset() => throw new NotSupportedException("A retried sequence cannot be reset.");

        public void Dispose()
        {
            FaultRecovery.Release(_enumerator);
            _enumerator = null;
            _finished = true;
        }
    }

    /// <summary>
    /// <c>OnErrorResumeNext(first, second)</c>: concatenates the two sequences while ignoring
    /// failures on either side.
    /// </summary>
    /// <remarks>
    /// The difference from <c>Catch</c> is what happens to a clean end: this operator always walks
    /// the second sequence, so it doubles as a plain concatenation when nothing goes wrong. The
    /// first sequence stops at its first failure; the second is still walked to its own end or
    /// failure, and then the result ends.
    /// </remarks>
    /// <typeparam name="T">The type of the elements.</typeparam>
    internal sealed class OnErrorResumeNextEnumerable<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T> _first;
        private readonly IEnumerable<T> _second;

        internal OnErrorResumeNextEnumerable(IEnumerable<T> first, IEnumerable<T> second)
        {
            _first = first;
            _second = second;
        }

        public IEnumerator<T> GetEnumerator() => new OnErrorResumeNextEnumerator<T>(_first, _second);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal sealed class OnErrorResumeNextEnumerator<T> : IEnumerator<T>
    {
        private const int ModeFirst = 0;
        private const int ModeSecond = 1;
        private const int ModeDone = 2;

        private readonly IEnumerable<T> _first;
        private readonly IEnumerable<T> _second;
        private IEnumerator<T>? _enumerator;
        private int _mode;
        private T _current;

        internal OnErrorResumeNextEnumerator(IEnumerable<T> first, IEnumerable<T> second)
        {
            _first = first;
            _second = second;
            _enumerator = null;
            _mode = ModeFirst;
            _current = default!;
        }

        public T Current => _current;

        object IEnumerator.Current => _current!;

        public bool MoveNext()
        {
            while (true)
            {
                switch (_mode)
                {
                    case ModeFirst:
                    {
                        if (_enumerator is null)
                        {
                            try
                            {
                                _enumerator = _first.GetEnumerator();
                            }
                            catch
                            {
                                _mode = ModeSecond;
                                continue;
                            }
                        }

                        bool moved;
                        try
                        {
                            moved = _enumerator.MoveNext();
                        }
                        catch
                        {
                            moved = false;
                        }

                        if (moved)
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        FaultRecovery.Release(_enumerator);
                        _enumerator = null;
                        _mode = ModeSecond;
                        continue;
                    }

                    case ModeSecond:
                    {
                        if (_enumerator is null)
                        {
                            try
                            {
                                _enumerator = _second.GetEnumerator();
                            }
                            catch
                            {
                                _mode = ModeDone;
                                return false;
                            }
                        }

                        bool moved;
                        try
                        {
                            moved = _enumerator.MoveNext();
                        }
                        catch
                        {
                            moved = false;
                        }

                        if (moved)
                        {
                            _current = _enumerator.Current;
                            return true;
                        }

                        FaultRecovery.Release(_enumerator);
                        _enumerator = null;
                        _mode = ModeDone;
                        return false;
                    }

                    default:
                        return false;
                }
            }
        }

        public void Reset() => throw new NotSupportedException("A recovered sequence cannot be reset.");

        public void Dispose()
        {
            FaultRecovery.Release(_enumerator);
            _enumerator = null;
            _mode = ModeDone;
        }
    }
}
