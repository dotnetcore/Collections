using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// The core property for a bag: after every operation the bag must agree with an independent
    /// reference model on copy counts, distinct counts, the inherited <c>Count</c>, containment and
    /// the values the removal members return.
    /// </summary>
    /// <typeparam name="TBag">The concrete bag type.</typeparam>
    /// <typeparam name="T">The element type.</typeparam>
    /// <remarks>
    /// <para>
    /// The model is <see cref="MultisetModel{T}"/>: a plain dictionary of copy counts that shares no
    /// code with the collection under test. Agreement is checked element by element over a finite
    /// domain rather than by comparing enumerations, because both sides enumerate in an unspecified
    /// order and comparing sequences would report false failures on correct implementations.
    /// </para>
    /// <para>
    /// Two invariants reach beyond "same contents as the model", because getting them wrong is a
    /// documented hazard of this family:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// The inherited <c>Count</c> must report the number of <em>copies</em>. An implementation that
    /// reports the number of distinct elements would still pass a contents-only comparison.
    /// </description></item>
    /// <item><description>
    /// The return value of the removal members must be the number of copies <em>remaining</em>,
    /// which is zero both for an absent element and for the removal that empties it.
    /// </description></item>
    /// </list>
    /// <para>
    /// The suite talks to the collection exclusively through <see cref="IBagUnderTest{TBag,T}"/>, so
    /// the same property covers the mutable collections today and the persistent ones planned for
    /// 8.1.0 without modification.
    /// </para>
    /// </remarks>
    public sealed class BagModelEquivalenceProperty<TBag, T> : StatefulProperty<BagOperation<T>>
    {
        private readonly IBagUnderTest<TBag, T> _adapter;
        private readonly IReadOnlyList<T> _domain;
        private readonly IEqualityComparer<T> _comparer;
        private readonly string _label;

        private TBag _bag;
        private MultisetModel<T> _model;
        private string _contractViolation;

        /// <summary>
        /// Initializes a new property.
        /// </summary>
        /// <param name="adapter">The adapter that talks to the collection under test.</param>
        /// <param name="label">A short label used in the property name and failure report.</param>
        /// <param name="domain">
        /// The finite set of elements the generator may draw from. Every element the bag can ever
        /// contain must come from this list, which is what lets the invariant check every element
        /// instead of guessing which ones exist.
        /// </param>
        /// <param name="comparer">The element comparer handed to both sides.</param>
        public BagModelEquivalenceProperty(IBagUnderTest<TBag, T> adapter, string label, IReadOnlyList<T> domain, IEqualityComparer<T> comparer)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            if (domain == null || domain.Count == 0)
            {
                throw new ArgumentException("The domain must contain at least one element.", nameof(domain));
            }

            _adapter = adapter;
            _label = label;
            _domain = domain;
            _comparer = comparer;

            Reset();
        }

        /// <inheritdoc />
        public override string Name => "bag/model-equivalence[" + _label + "]";

        /// <inheritdoc />
        public override BagOperation<T> NextOperation(DeterministicRandom random)
        {
            var draw = random.NextInt(0, 100);
            var operation = new BagOperation<T>();

            if (draw < 40)
            {
                operation.Kind = BagOperationKind.Add;
                operation.Item = random.NextItem(_domain);
            }
            else if (draw < 60)
            {
                operation.Kind = BagOperationKind.AddCopies;
                operation.Item = random.NextItem(_domain);
                operation.Times = random.NextInt(1, 5);
            }
            else if (draw < 78)
            {
                operation.Kind = BagOperationKind.RemoveOne;
                operation.Item = random.NextItem(_domain);
            }
            else if (draw < 92)
            {
                operation.Kind = BagOperationKind.RemoveCopies;
                operation.Item = random.NextItem(_domain);
                operation.Times = random.NextInt(1, 5);
            }
            else if (draw < 98)
            {
                operation.Kind = BagOperationKind.RemoveAllCopies;
                operation.Item = random.NextItem(_domain);
            }
            else
            {
                operation.Kind = BagOperationKind.Clear;
            }

            return operation;
        }

        /// <inheritdoc />
        public override void Apply(BagOperation<T> operation)
        {
            switch (operation.Kind)
            {
                case BagOperationKind.Add:
                    _bag = _adapter.Add(_bag, operation.Item);
                    _model.Add(operation.Item);
                    break;

                case BagOperationKind.AddCopies:
                    _bag = _adapter.AddCopies(_bag, operation.Item, operation.Times);
                    _model.Add(operation.Item, operation.Times);
                    break;

                case BagOperationKind.RemoveOne:
                {
                    var expected = _model.Remove(operation.Item);
                    int remaining;
                    _bag = _adapter.RemoveOne(_bag, operation.Item, out remaining);
                    RecordReturnValue("Remove(item)", operation, expected, remaining);
                    break;
                }

                case BagOperationKind.RemoveCopies:
                {
                    var expected = _model.Remove(operation.Item, operation.Times);
                    int remaining;
                    _bag = _adapter.RemoveCopies(_bag, operation.Item, operation.Times, out remaining);
                    RecordReturnValue("Remove(item, times)", operation, expected, remaining);
                    break;
                }

                case BagOperationKind.RemoveAllCopies:
                {
                    var expected = _model.RemoveAllCopies(operation.Item);
                    bool removedAnything;
                    _bag = _adapter.RemoveAllCopies(_bag, operation.Item, out removedAnything);
                    if (removedAnything != expected)
                    {
                        RecordViolation(
                            "RemoveAllCopies(" + Format(operation.Item) + ") returned " + removedAnything +
                            " but the model removed " + expected + ".");
                    }

                    break;
                }

                case BagOperationKind.Clear:
                    _bag = _adapter.Clear(_bag);
                    _model.Clear();
                    break;

                default:
                    throw new InvalidOperationException("Unknown bag operation: " + operation.Kind + ".");
            }
        }

        /// <inheritdoc />
        public override string CheckInvariant()
        {
            if (_contractViolation != null)
            {
                return _contractViolation;
            }

            var expectedTotal = _model.TotalCount;
            var actualTotal = _adapter.TotalCount(_bag);
            if (actualTotal != expectedTotal)
            {
                return "TotalCount is " + actualTotal + " but the model holds " + expectedTotal + " copies.";
            }

            var actualDistinct = _adapter.DistinctCount(_bag);
            if (actualDistinct != _model.DistinctCount)
            {
                return "DistinctCount is " + actualDistinct + " but the model holds " + _model.DistinctCount + " distinct elements.";
            }

            var reported = _adapter.ReportedCount(_bag);
            if (reported != expectedTotal)
            {
                return "The inherited Count reports " + reported + " but TotalCount is " + expectedTotal +
                       "; the multiset contract pins Count to the number of copies.";
            }

            var summedCopies = 0;
            var countedClasses = new HashSet<T>(_comparer ?? EqualityComparer<T>.Default);

            for (var index = 0; index < _domain.Count; index++)
            {
                var item = _domain[index];
                var expected = _model.CountOf(item);
                var actual = _adapter.CountOf(_bag, item);

                if (actual != expected)
                {
                    return "CountOf(" + Format(item) + ") is " + actual + " but the model holds " + expected + ".";
                }

                var contains = _adapter.Contains(_bag, item);
                if (contains != (expected > 0))
                {
                    return "Contains(" + Format(item) + ") returned " + contains + " while the copy count is " + expected + ".";
                }

                // The domain may hold several values that the comparer folds into one equivalence
                // class (for example "a" and "A" under a case-insensitive comparer). Each of them
                // must answer the same query as above, but only one of them may contribute to the
                // sum, otherwise the total is counted once per spelling.
                if (countedClasses.Add(item))
                {
                    summedCopies += expected;
                }
            }

            if (summedCopies != expectedTotal)
            {
                return "The bag holds " + expectedTotal + " copies but the domain accounts for " + summedCopies +
                       "; an element outside the domain was introduced.";
            }

            var enumeratedDistinct = 0;
            foreach (var unused in _adapter.DistinctItems(_bag))
            {
                enumeratedDistinct++;
            }

            if (enumeratedDistinct != _model.DistinctCount)
            {
                return "DistinctItems() yielded " + enumeratedDistinct + " elements but the model holds " +
                       _model.DistinctCount + " distinct elements.";
            }

            return null;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            _bag = _adapter.Create(_comparer);
            _model = new MultisetModel<T>(_comparer);
            _contractViolation = null;
        }

        /// <inheritdoc />
        public override string Describe(BagOperation<T> operation)
        {
            switch (operation.Kind)
            {
                case BagOperationKind.Add:
                    return "Add(" + Format(operation.Item) + ")";
                case BagOperationKind.AddCopies:
                    return "Add(" + Format(operation.Item) + ", " + operation.Times + ")";
                case BagOperationKind.RemoveOne:
                    return "Remove(" + Format(operation.Item) + ")";
                case BagOperationKind.RemoveCopies:
                    return "Remove(" + Format(operation.Item) + ", " + operation.Times + ")";
                case BagOperationKind.RemoveAllCopies:
                    return "RemoveAllCopies(" + Format(operation.Item) + ")";
                case BagOperationKind.Clear:
                    return "Clear()";
                default:
                    return operation.Kind.ToString();
            }
        }

        private static string Format(T item)
        {
            return ReferenceEquals(item, null) ? "null" : Convert.ToString(item, CultureInfo.InvariantCulture);
        }

        private void RecordReturnValue(string member, BagOperation<T> operation, int expected, int actual)
        {
            if (expected == actual)
            {
                return;
            }

            RecordViolation(
                member + " on " + Format(operation.Item) + (operation.Times > 0 ? " x" + operation.Times : string.Empty) +
                " returned " + actual + " remaining copies but the model reports " + expected + ".");
        }

        private void RecordViolation(string description)
        {
            if (_contractViolation == null)
            {
                _contractViolation = description;
            }
        }
    }
}
