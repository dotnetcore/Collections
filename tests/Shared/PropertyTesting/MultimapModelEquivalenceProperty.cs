using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// The core property for a multimap: after every operation the map must agree with an
    /// independent reference model on key count, total value count, per-key value counts,
    /// containment, and the values the removal members return.
    /// </summary>
    /// <typeparam name="TMap">The concrete multimap type.</typeparam>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// <para>
    /// Two contract points get their own assertion because a contents-only comparison would miss
    /// them:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// The inherited <c>Count</c> must report the number of <em>keys</em>, not the number of
    /// values. The two only differ once a key holds more than one value, which random operation
    /// sequences reach quickly.
    /// </description></item>
    /// <item><description>
    /// A key must disappear as soon as its last value is removed, so <c>KeyCount</c> and
    /// <c>TotalValueCount</c> can never disagree about an empty key.
    /// </description></item>
    /// </list>
    /// <para>
    /// <c>ValuesOf</c> is checked for absent keys as well: the contract promises an empty
    /// collection there, not an exception.
    /// </para>
    /// </remarks>
    public sealed class MultimapModelEquivalenceProperty<TMap, TKey, TValue> : StatefulProperty<MultimapOperation<TKey, TValue>>
    {
        private readonly IMultimapUnderTest<TMap, TKey, TValue> _adapter;
        private readonly IReadOnlyList<TKey> _keyDomain;
        private readonly IReadOnlyList<TValue> _valueDomain;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly bool _allowDuplicateValues;
        private readonly string _label;

        private TMap _map;
        private MultimapModel<TKey, TValue> _model;
        private string _contractViolation;

        /// <summary>
        /// Initializes a new property.
        /// </summary>
        /// <param name="adapter">The adapter that talks to the multimap under test.</param>
        /// <param name="label">A short label used in the property name.</param>
        /// <param name="keyDomain">The finite set of keys the generator may draw from.</param>
        /// <param name="valueDomain">The finite set of values the generator may draw from.</param>
        /// <param name="keyComparer">The key comparer handed to both sides.</param>
        /// <param name="allowDuplicateValues">
        /// The duplicate-value mode the map under test was configured with; the model has to agree
        /// with it or every run would report a defect.
        /// </param>
        public MultimapModelEquivalenceProperty(
            IMultimapUnderTest<TMap, TKey, TValue> adapter,
            string label,
            IReadOnlyList<TKey> keyDomain,
            IReadOnlyList<TValue> valueDomain,
            IEqualityComparer<TKey> keyComparer,
            bool allowDuplicateValues)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            if (keyDomain == null || keyDomain.Count == 0)
            {
                throw new ArgumentException("The key domain must contain at least one key.", nameof(keyDomain));
            }

            if (valueDomain == null || valueDomain.Count == 0)
            {
                throw new ArgumentException("The value domain must contain at least one value.", nameof(valueDomain));
            }

            _adapter = adapter;
            _label = label;
            _keyDomain = keyDomain;
            _valueDomain = valueDomain;
            _keyComparer = keyComparer;
            _allowDuplicateValues = allowDuplicateValues;

            Reset();
        }

        /// <inheritdoc />
        public override string Name => "multimap/model-equivalence[" + _label + "]";

        /// <inheritdoc />
        public override MultimapOperation<TKey, TValue> NextOperation(DeterministicRandom random)
        {
            var draw = random.NextInt(0, 100);
            var operation = new MultimapOperation<TKey, TValue>();

            if (draw < 62)
            {
                operation.Kind = MultimapOperationKind.Add;
                operation.Key = random.NextItem(_keyDomain);
                operation.Value = random.NextItem(_valueDomain);
            }
            else if (draw < 88)
            {
                operation.Kind = MultimapOperationKind.RemoveValue;
                operation.Key = random.NextItem(_keyDomain);
                operation.Value = random.NextItem(_valueDomain);
            }
            else if (draw < 97)
            {
                operation.Kind = MultimapOperationKind.RemoveKey;
                operation.Key = random.NextItem(_keyDomain);
            }
            else
            {
                operation.Kind = MultimapOperationKind.Clear;
            }

            return operation;
        }

        /// <inheritdoc />
        public override void Apply(MultimapOperation<TKey, TValue> operation)
        {
            switch (operation.Kind)
            {
                case MultimapOperationKind.Add:
                    _map = _adapter.Add(_map, operation.Key, operation.Value);
                    _model.Add(operation.Key, operation.Value);
                    break;

                case MultimapOperationKind.RemoveValue:
                {
                    var expected = _model.Remove(operation.Key, operation.Value);
                    bool removed;
                    _map = _adapter.RemoveValue(_map, operation.Key, operation.Value, out removed);
                    if (removed != expected)
                    {
                        RecordViolation(
                            "Remove(" + Format(operation.Key) + ", " + Format(operation.Value) + ") returned " + removed +
                            " but the model reports " + expected + ".");
                    }

                    break;
                }

                case MultimapOperationKind.RemoveKey:
                {
                    var expected = _model.Remove(operation.Key);
                    bool removed;
                    _map = _adapter.RemoveKey(_map, operation.Key, out removed);
                    if (removed != expected)
                    {
                        RecordViolation(
                            "Remove(" + Format(operation.Key) + ") returned " + removed + " but the model reports " + expected + ".");
                    }

                    break;
                }

                case MultimapOperationKind.Clear:
                    _map = _adapter.Clear(_map);
                    _model.Clear();
                    break;

                default:
                    throw new InvalidOperationException("Unknown multimap operation: " + operation.Kind + ".");
            }
        }

        /// <inheritdoc />
        public override string CheckInvariant()
        {
            if (_contractViolation != null)
            {
                return _contractViolation;
            }

            var expectedKeys = _model.KeyCount;
            var actualKeys = _adapter.KeyCount(_map);
            if (actualKeys != expectedKeys)
            {
                return "KeyCount is " + actualKeys + " but the model holds " + expectedKeys + " keys.";
            }

            var expectedTotal = _model.TotalValueCount;
            var actualTotal = _adapter.TotalValueCount(_map);
            if (actualTotal != expectedTotal)
            {
                return "TotalValueCount is " + actualTotal + " but the model holds " + expectedTotal + " values.";
            }

            var reported = _adapter.ReportedCount(_map);
            if (reported != expectedKeys)
            {
                return "The inherited Count reports " + reported + " but KeyCount is " + expectedKeys +
                       "; the multimap contract pins Count to the number of keys.";
            }

            for (var keyIndex = 0; keyIndex < _keyDomain.Count; keyIndex++)
            {
                var key = _keyDomain[keyIndex];
                var expected = _model.ValueCount(key);
                var actual = _adapter.ValueCount(_map, key);

                if (actual != expected)
                {
                    return "ValueCount(" + Format(key) + ") is " + actual + " but the model holds " + expected + ".";
                }

                var values = _adapter.ValuesOf(_map, key);
                if (values == null)
                {
                    return "ValuesOf(" + Format(key) + ") returned null; the contract promises an empty collection for an absent key.";
                }

                if (values.Count != expected)
                {
                    return "ValuesOf(" + Format(key) + ") yields " + values.Count + " values but the model holds " + expected + ".";
                }

                for (var valueIndex = 0; valueIndex < _valueDomain.Count; valueIndex++)
                {
                    var value = _valueDomain[valueIndex];
                    var expectedContains = _model.Contains(key, value);
                    var actualContains = _adapter.Contains(_map, key, value);

                    if (actualContains != expectedContains)
                    {
                        return "Contains(" + Format(key) + ", " + Format(value) + ") returned " + actualContains +
                               " but the model reports " + expectedContains + ".";
                    }
                }
            }

            for (var valueIndex = 0; valueIndex < _valueDomain.Count; valueIndex++)
            {
                var value = _valueDomain[valueIndex];
                var expectedContains = _model.ContainsValue(value);
                var actualContains = _adapter.ContainsValue(_map, value);

                if (actualContains != expectedContains)
                {
                    return "ContainsValue(" + Format(value) + ") returned " + actualContains +
                           " but the model reports " + expectedContains + ".";
                }
            }

            var flattened = 0;
            foreach (var unused in _adapter.FlattenedValues(_map))
            {
                flattened++;
            }

            if (flattened != expectedTotal)
            {
                return "The flattened Values sequence yields " + flattened + " values but TotalValueCount is " +
                       expectedTotal + ".";
            }

            var enumeratedKeys = 0;
            foreach (var unused in _adapter.Keys(_map))
            {
                enumeratedKeys++;
            }

            if (enumeratedKeys != expectedKeys)
            {
                return "Keys yielded " + enumeratedKeys + " keys but KeyCount is " + expectedKeys + ".";
            }

            return null;
        }

        /// <inheritdoc />
        public override void Reset()
        {
            _map = _adapter.Create(_keyComparer);
            _model = new MultimapModel<TKey, TValue>(_keyComparer, null, _allowDuplicateValues);
            _contractViolation = null;
        }

        /// <inheritdoc />
        public override string Describe(MultimapOperation<TKey, TValue> operation)
        {
            switch (operation.Kind)
            {
                case MultimapOperationKind.Add:
                    return "Add(" + Format(operation.Key) + ", " + Format(operation.Value) + ")";
                case MultimapOperationKind.RemoveValue:
                    return "Remove(" + Format(operation.Key) + ", " + Format(operation.Value) + ")";
                case MultimapOperationKind.RemoveKey:
                    return "Remove(" + Format(operation.Key) + ")";
                case MultimapOperationKind.Clear:
                    return "Clear()";
                default:
                    return operation.Kind.ToString();
            }
        }

        private static string Format(TValue value)
        {
            return ReferenceEquals(value, null) ? "null" : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string Format(TKey key)
        {
            return ReferenceEquals(key, null) ? "null" : Convert.ToString(key, CultureInfo.InvariantCulture);
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
