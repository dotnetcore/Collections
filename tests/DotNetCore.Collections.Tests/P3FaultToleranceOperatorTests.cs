using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DotNetCore.Collections;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-14 (first half): the side-effect and fault-tolerance operators of the P3 layer -
    /// <c>Do</c>, <c>Catch</c>, <c>Retry</c> and <c>OnErrorResumeNext</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// None of these operators exists in the BCL, so the semantics are pinned directly. The cases
    /// that matter are the ones a naive implementation gets wrong: <c>Catch</c> must not walk its
    /// fallback when the first sequence simply ended, <c>Retry</c> must report how many duplicate
    /// elements a restarted attempt produces, and <c>OnErrorResumeNext</c> must walk the second
    /// sequence even when the first failed before its first element.
    /// </para>
    /// <para>
    /// Failure is expressed with local iterator functions where the sequence has to be re-creatable
    /// (a restart needs a fresh walk of the same body) and with <see cref="HostileSequence"/> where
    /// the failure has to happen in <c>GetEnumerator</c> rather than in the body - an iterator
    /// cannot fail at that point, because its body has not run yet.
    /// </para>
    /// </remarks>
    public class P3FaultToleranceOperatorTests
    {
        // ----- Do -----

        [Fact]
        public void Do_yields_the_source_unchanged_and_taps_every_element_on_every_arm()
        {
            var numbers = new[] { 4, 1, 9 };
            var taps = new List<int>();

            var fromArray = numbers.ToValueEnumerable().Do(taps.Add).ToArray();
            var fromList = new List<int>(numbers).ToValueEnumerable().Do(taps.Add).ToArray();

            IReadOnlyList<int> readOnlySource = new List<int>(numbers);
            var fromReadOnly = readOnlySource.ToValueEnumerable().Do(taps.Add).ToArray();

            IEnumerable<int> enumerationSource = new Queue<int>(numbers);
            var fromEnumerable = enumerationSource.ToValueEnumerable().Do(taps.Add).ToArray();

            foreach (var actual in new[] { fromArray, fromList, fromReadOnly, fromEnumerable })
            {
                actual.ShouldBe(numbers);
            }

            // Twelve taps in total: three per arm, each in source order.
            taps.ShouldBe(new[] { 4, 1, 9, 4, 1, 9, 4, 1, 9, 4, 1, 9 });
        }

        [Fact]
        public void Do_with_an_index_reports_zero_based_positions()
        {
            var seen = new List<string>();

            var result = new[] { "a", "b", "c" }
                .ToValueEnumerable()
                .Do((element, index) => seen.Add(index + ":" + element))
                .ToArray();

            result.ShouldBe(new[] { "a", "b", "c" });
            seen.ShouldBe(new[] { "0:a", "1:b", "2:c" });
        }

        [Fact]
        public void Do_taps_only_the_elements_that_are_pulled()
        {
            var taps = 0;

            var query = new[] { 1, 2, 3, 4 }
                .ToValueEnumerable()
                .Do(_ => taps++)
                .Take(2);

            // Building the pipeline taps nothing; only pulling does.
            taps.ShouldBe(0);

            var firstTwo = query.ToArray();

            firstTwo.ShouldBe(new[] { 1, 2 });
            taps.ShouldBe(2);
        }

        [Fact]
        public void Do_rejects_a_null_action_on_every_arm()
        {
            Should.Throw<ArgumentNullException>(() => new[] { 1 }.ToValueEnumerable().Do((Action<int>)null!));
            Should.Throw<ArgumentNullException>(() => new List<int> { 1 }.ToValueEnumerable().Do((Action<int>)null!));

            IReadOnlyList<int> readOnlySource = new List<int> { 1 };
            Should.Throw<ArgumentNullException>(() => readOnlySource.ToValueEnumerable().Do((Action<int>)null!));

            IEnumerable<int> enumerationSource = new Queue<int>(new[] { 1 });
            Should.Throw<ArgumentNullException>(() => enumerationSource.ToValueEnumerable().Do((Action<int>)null!));
        }

        // ----- Catch (fallback form) -----

        [Fact]
        public void Catch_continues_with_the_fallback_after_a_failure()
        {
            var result = HostileSequence.YieldingThenFailing(1, 2)
                .ToValueEnumerable()
                .Catch(new[] { 7, 8 })
                .ToArray();

            result.ShouldBe(new[] { 1, 2, 7, 8 });
        }

        [Fact]
        public void Catch_does_not_touch_the_fallback_when_the_source_ends_cleanly()
        {
            var fallback = new ProbeEnumerable();

            var result = new[] { 1, 2 }
                .ToValueEnumerable()
                .Catch(fallback)
                .ToArray();

            result.ShouldBe(new[] { 1, 2 });
            fallback.EnumeratorRequests.ShouldBe(0);
        }

        [Fact]
        public void Catch_recovers_when_the_source_fails_before_the_first_element()
        {
            var source = new HostileSequence(new int[0], failOnStart: true);

            var result = source.ToValueEnumerable().Catch(new[] { 5 }).ToArray();

            result.ShouldBe(new[] { 5 });
            source.Starts.ShouldBe(1);
        }

        [Fact]
        public void Catch_releases_the_enumerator_it_abandoned()
        {
            var source = new HostileSequence(new[] { 1 });

            source.ToValueEnumerable().Catch(new[] { 2 }).ToArray().ShouldBe(new[] { 1, 2 });

            source.Releases.ShouldBe(1);
        }

        // ----- Catch (handler form) -----

        [Fact]
        public void Catch_handler_receives_the_failure_and_chooses_the_continuation()
        {
            InvalidOperationException observed = null;

            var result = HostileSequence.YieldingThenFailing(1, 2)
                .ToValueEnumerable()
                .Catch((InvalidOperationException exception) =>
                {
                    observed = exception;
                    return new[] { 9 };
                })
                .ToArray();

            result.ShouldBe(new[] { 1, 2, 9 });
            observed.ShouldNotBeNull();
            observed.Message.ShouldBe(HostileSequence.FailureMessage);
        }

        [Fact]
        public void Catch_handler_that_returns_null_ends_the_sequence_quietly()
        {
            var result = HostileSequence.YieldingThenFailing(1, 2)
                .ToValueEnumerable()
                .Catch((InvalidOperationException _) => null)
                .ToArray();

            result.ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void Catch_handler_lets_an_exception_of_another_type_through()
        {
            // The source raises InvalidOperationException; this handler only claims to handle
            // ArgumentException, so the failure has to surface unchanged.
            Should.Throw<InvalidOperationException>(() => HostileSequence.YieldingThenFailing(1)
                .ToValueEnumerable()
                .Catch((ArgumentException _) => new[] { 0 })
                .ToArray());
        }

        [Fact]
        public void Catch_handler_is_not_consulted_when_nothing_fails()
        {
            var consulted = 0;

            var result = new[] { 1, 2 }
                .ToValueEnumerable()
                .Catch((InvalidOperationException _) =>
                {
                    consulted++;
                    return new[] { 9 };
                })
                .ToArray();

            result.ShouldBe(new[] { 1, 2 });
            consulted.ShouldBe(0);
        }

        // ----- Retry -----

        [Fact]
        public void Retry_restarts_the_source_and_repeats_the_prefix_it_already_yielded()
        {
            var attempts = 0;

            IEnumerable<int> Transient()
            {
                attempts++;
                yield return 1;
                yield return 2;
                if (attempts < 2)
                {
                    throw new InvalidOperationException("transient");
                }

                yield return 3;
            }

            var result = Transient().ToValueEnumerable().Retry(1).ToArray();

            // The prefix is produced twice: a retry restarts the walk, it does not resume it.
            result.ShouldBe(new[] { 1, 2, 1, 2, 3 });
            attempts.ShouldBe(2);
        }

        [Fact]
        public void Retry_with_zero_retries_propagates_the_first_failure()
        {
            Should.Throw<InvalidOperationException>(() => HostileSequence.YieldingThenFailing(1, 2)
                .ToValueEnumerable()
                .Retry(0)
                .ToArray());
        }

        [Fact]
        public void Retry_gives_up_after_the_budget_is_spent()
        {
            var attempts = 0;

            IEnumerable<int> AlwaysFailing()
            {
                attempts++;
                yield return 1;
                throw new InvalidOperationException("permanent");
            }

            Should.Throw<InvalidOperationException>(() => AlwaysFailing()
                .ToValueEnumerable()
                .Retry(2)
                .ToArray());

            // One initial attempt plus two retries.
            attempts.ShouldBe(3);
        }

        [Fact]
        public void Retry_rejects_a_negative_count()
        {
            Should.Throw<ArgumentOutOfRangeException>(() => new[] { 1 }.ToValueEnumerable().Retry(-1));
        }

        [Fact]
        public void Retry_walks_a_clean_source_exactly_once()
        {
            var source = new ProbeEnumerable();

            var result = source.ToValueEnumerable().Retry(3).ToArray();

            result.ShouldBe(new[] { 1, 2, 3 });
            source.EnumeratorRequests.ShouldBe(1);
        }

        // ----- OnErrorResumeNext -----

        [Fact]
        public void OnErrorResumeNext_concatenates_when_nothing_fails()
        {
            var result = new[] { 1, 2 }.ToValueEnumerable().OnErrorResumeNext(new[] { 3, 4 }).ToArray();

            result.ShouldBe(new[] { 1, 2, 3, 4 });
        }

        [Fact]
        public void OnErrorResumeNext_skips_over_a_failing_first_side()
        {
            var result = HostileSequence.YieldingThenFailing(1, 2)
                .ToValueEnumerable()
                .OnErrorResumeNext(new[] { 7, 8 })
                .ToArray();

            result.ShouldBe(new[] { 1, 2, 7, 8 });
        }

        [Fact]
        public void OnErrorResumeNext_swallows_failures_on_both_sides()
        {
            var result = new HostileSequence(new int[0])
                .ToValueEnumerable()
                .OnErrorResumeNext(new HostileSequence(new[] { 5 }))
                .ToArray();

            result.ShouldBe(new[] { 5 });
        }

        [Fact]
        public void OnErrorResumeNext_always_walks_the_second_side()
        {
            var second = new ProbeEnumerable();

            var result = new[] { 1 }.ToValueEnumerable().OnErrorResumeNext(second).ToArray();

            result.ShouldBe(new[] { 1, 1, 2, 3 });
            second.EnumeratorRequests.ShouldBe(1);
        }
    }

    /// <summary>
    /// A sequence that yields a fixed prefix and then fails, or that fails before it starts. The
    /// mode that fails in <c>GetEnumerator</c> exists because an iterator's body does not run until
    /// the first <c>MoveNext</c>, so a start-up failure cannot be expressed with a local function.
    /// </summary>
    internal sealed class HostileSequence : IEnumerable<int>
    {
        internal const string FailureMessage = "the sequence failed";

        private readonly int[] _prefix;
        private readonly bool _failOnStart;

        internal HostileSequence(int[] prefix, bool failOnStart = false)
        {
            _prefix = prefix;
            _failOnStart = failOnStart;
        }

        internal int Starts { get; private set; }

        internal int Releases { get; private set; }

        internal static HostileSequence YieldingThenFailing(params int[] prefix) => new HostileSequence(prefix);

        public IEnumerator<int> GetEnumerator()
        {
            Starts++;
            if (_failOnStart)
            {
                throw new InvalidOperationException(FailureMessage);
            }

            return new HostileEnumerator(this, _prefix);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void RecordRelease() => Releases++;

        private sealed class HostileEnumerator : IEnumerator<int>
        {
            private readonly HostileSequence _owner;
            private readonly int[] _prefix;
            private int _index = -1;

            internal HostileEnumerator(HostileSequence owner, int[] prefix)
            {
                _owner = owner;
                _prefix = prefix;
            }

            public int Current => _prefix[_index];

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _index++;
                if (_index < _prefix.Length)
                {
                    return true;
                }

                throw new InvalidOperationException(FailureMessage);
            }

            public void Reset() => _index = -1;

            public void Dispose() => _owner.RecordRelease();
        }
    }
}
