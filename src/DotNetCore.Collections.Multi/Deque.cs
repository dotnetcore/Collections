using System;
using System.Collections;
using System.Collections.Generic;

namespace DotNetCore.Collections.Multi
{
    /// <summary>
    /// Represents a double-ended queue (deque): a sequence that can be added to and removed from
    /// <em>both</em> ends in O(1) amortized time, and read at any position in O(1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The .NET base class library ships no deque. <see cref="Queue{T}"/> and <see cref="Stack{T}"/>
    /// are single-ended, and <see cref="LinkedList{T}"/> — the only in-box type with two ends —
    /// stores one heap node per element, so every add allocates, every element carries two
    /// references of overhead and no position can be read without walking the list. This type
    /// closes that gap with a single array used as a ring buffer: the head and the tail are two
    /// moving indices into one allocation, so pushing and popping at either end is an index move
    /// plus one array slot, and growth is the only thing that ever allocates.
    /// </para>
    /// <para>
    /// <b>Positions address the sequence from head to tail.</b> Index <c>0</c> is the first
    /// element (<see cref="GetFirst"/>) and index <see cref="Count"/> - 1 is the last
    /// (<see cref="GetLast"/>), so <see cref="this[int]"/> is a plain O(1) ring read and
    /// <see cref="IndexOf(T)"/> scans from the head. That is a different contract from
    /// <see cref="OrderedMultiList{T}"/>, where a position addresses the <em>expanded</em> sorted
    /// multiset and a comparer decides which element sits where: nothing here reorders anything,
    /// and an element stays where it was put until the caller moves it.
    /// </para>
    /// <para>
    /// <b>Cost, by member.</b> Adding and removing at either end, reading either end and reading
    /// any position are O(1) — O(1) amortized for the four adds and removes, because the ring
    /// occasionally has to grow. <see cref="Contains(T)"/> / <see cref="IndexOf(T)"/> /
    /// <see cref="Remove(T)"/> are O(n): a deque trades random <em>search</em> away for O(1) access
    /// at the two ends, and no amount of ring bookkeeping changes that.
    /// <see cref="Insert(int, T)"/> and <see cref="RemoveAt(int)"/> slide the shorter side, so they
    /// cost O(min(index, <see cref="Count"/> - 1 - index)) and are O(1) amortized at either end.
    /// </para>
    /// <para>
    /// <b>Capacity follows <see cref="List{T}"/>:</b> the buffer doubles when it fills, starting
    /// from 4, and never shrinks by itself — a deque that has drained to empty keeps its buffer so
    /// the next fill does not pay for it again. <see cref="TrimExcess"/> reclaims the slack, and
    /// <see cref="Capacity"/> can be set directly. Growth is the one operation that copies: it
    /// moves the ring into a fresh array with the head at slot zero, which is why the adds are
    /// amortized rather than worst-case O(1).
    /// </para>
    /// <para>
    /// <b>Null elements are supported</b> and are stored like any other element: there is no
    /// comparer and no hash table behind this type, so nothing inspects an element on the way in
    /// and a <c>null</c> can occupy a slot including the first and the last one. The three members
    /// that do compare — <see cref="Contains(T)"/>, <see cref="IndexOf(T)"/> and
    /// <see cref="Remove(T)"/> — go through <see cref="EqualityComparer{T}.Default"/>, which treats
    /// <c>null</c> as equal to <c>null</c> and unequal to everything else; that is the same
    /// convention <see cref="MultiList{T}"/> follows, where a <c>null</c> is an ordinary element
    /// with a copy count of its own.
    /// </para>
    /// <para>
    /// <see cref="IList{T}"/> and <see cref="IReadOnlyList{T}"/> are both implemented, so a deque
    /// can be handed to anything that takes a list. The two interfaces ask for the same members
    /// here and there is no conflict to resolve: every <see cref="IList{T}"/> member is meaningful
    /// on a sequence whose order the caller controls, including the indexer setter and
    /// <see cref="Insert(int, T)"/> — unlike the sorted family, where a comparer owns the positions
    /// and those two have to throw.
    /// </para>
    /// <para>
    /// Structural equality is <b>not</b> overridden: two deques with the same elements in the same
    /// order are still distinct objects under <see cref="object.Equals(object)"/>, matching every
    /// other type of this package except <see cref="MultiList{T}"/>. Use
    /// <see cref="ToArray"/> and compare the sequences.
    /// </para>
    /// <para>
    /// This class is not thread-safe. Wrap it with external synchronization for concurrent use.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var line = new Deque&lt;string&gt; { "b", "c" };
    ///
    /// line.AddFirst("a");                      // a, b, c
    /// line.AddLast("d");                       // a, b, c, d
    /// line.RemoveFirst();                      // "a" -&gt; b, c, d
    /// line.RemoveLast();                       // "d" -&gt; b, c
    ///
    /// line[0];                                 // "b" (O(1))
    /// line.GetFirst();                         // "b"
    /// </code>
    /// </example>
    public class Deque<T> : IEnumerable<T>, ICollection<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, IList<T>
    {
        /// <summary>The buffer length a deque starts from when no capacity is requested.</summary>
        private const int DefaultCapacity = 4;

        /// <summary>The largest length a single array may have on any supported runtime.</summary>
        private const int MaxCapacity = 0x7FFFFFC7;

        // Array.Empty<T>() is .NET 4.6+, so the shared empty buffer is declared here instead: this
        // package still targets net451 and net461.
        private static readonly T[] EmptyBuffer = new T[0];

        private T[] _buffer;
        private int _head;
        private int _count;
        private int _version;

        /// <summary>
        /// Initializes an empty <see cref="Deque{T}"/>.
        /// </summary>
        public Deque() : this(0)
        {
        }

        /// <summary>
        /// Initializes an empty <see cref="Deque{T}"/> whose buffer can hold
        /// <paramref name="capacity"/> elements before it has to grow.
        /// </summary>
        /// <param name="capacity">the number of elements the buffer is sized for up front.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
        /// <example>
        /// <code>
        /// var window = new Deque&lt;int&gt;(1024);   // a sliding window that never grows
        /// </code>
        /// </example>
        public Deque(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity can not be less than zero.");
            }

            _buffer = capacity == 0 ? EmptyBuffer : new T[capacity];
            _head = 0;
            _count = 0;
            _version = 0;
        }

        /// <summary>
        /// Initializes a <see cref="Deque{T}"/> holding the elements of the specified collection,
        /// in the order the collection enumerates them (the first element becomes the head).
        /// </summary>
        /// <param name="collection">the elements to copy in.</param>
        /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
        public Deque(IEnumerable<T> collection) : this(0)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            // Sizing up front keeps a known-length source to one allocation. Everything else pays
            // the doubling path, which is still amortized O(1) per element.
            if (collection is ICollection<T> sized && sized.Count > 0)
            {
                SetCapacity(sized.Count);
            }

            foreach (var item in collection)
            {
                AddLast(item);
            }
        }

        /// <summary>
        /// Gets or sets the number of elements the buffer can hold without growing. Setting a
        /// capacity below <see cref="Count"/> is rejected; setting one above it copies the ring
        /// into a fresh buffer with the head at slot zero.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">the value is negative or below <see cref="Count"/>.</exception>
        /// <example>
        /// <code>
        /// var window = new Deque&lt;int&gt;(1024);
        /// window.Capacity = 4096;                  // one copy, then 4096 adds without growth
        /// </code>
        /// </example>
        public int Capacity
        {
            get => _buffer.Length;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Capacity can not be less than zero.");
                }

                if (value < _count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        "The capacity can not be set below the number of elements the deque holds.");
                }

                SetCapacity(value);
            }
        }

        /// <summary>
        /// Gets the number of elements the deque holds.
        /// </summary>
        public int Count => _count;

        /// <summary>Gets a value indicating whether the deque holds no elements.</summary>
        public bool IsEmpty => _count == 0;

        /// <summary>Gets a value indicating whether the deque is read-only. Always <c>false</c>.</summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets or sets the element at the specified position, counted from the head. O(1).
        /// </summary>
        /// <param name="index">the zero-based position, from zero to <see cref="Count"/> - 1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or
        /// greater than or equal to <see cref="Count"/>.</exception>
        /// <example>
        /// <code>
        /// // line holds "b", "c"
        /// line[0];                                 // "b"
        /// line[1] = "C";                           // overwrites in place, no shifting
        /// </code>
        /// </example>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index), "The index is outside the bounds of the deque.");
                }

                return GetElement(index);
            }
            set
            {
                if (index < 0 || index >= _count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(index), "The index is outside the bounds of the deque.");
                }

                _buffer[ToBufferIndex(index)] = value;
                _version++;
            }
        }

        // ------------------------------------------------------------------
        // The two ends
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds the element at the front of the deque, making it the new first element. O(1)
        /// amortized.
        /// </summary>
        /// <example>
        /// <code>
        /// line.AddFirst("a");
        /// </code>
        /// </example>
        public void AddFirst(T item)
        {
            EnsureCapacityForOneMore();
            _head = _head == 0 ? _buffer.Length - 1 : _head - 1;
            _buffer[_head] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Adds the element at the back of the deque, making it the new last element. O(1)
        /// amortized. <see cref="Add(T)"/> is this member under its <see cref="ICollection{T}"/>
        /// name.
        /// </summary>
        /// <example>
        /// <code>
        /// line.AddLast("d");
        /// </code>
        /// </example>
        public void AddLast(T item)
        {
            EnsureCapacityForOneMore();
            _buffer[ToBufferIndex(_count)] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Removes and returns the first element. O(1).
        /// </summary>
        /// <returns>the element that was at the front.</returns>
        /// <exception cref="InvalidOperationException">the deque is empty.</exception>
        /// <example>
        /// <code>
        /// string head = line.RemoveFirst();
        /// </code>
        /// </example>
        public T RemoveFirst()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The deque is empty, so it has no first element to remove.");
            }

            var item = _buffer[_head];
            _buffer[_head] = default!;
            _head = _head + 1 == _buffer.Length ? 0 : _head + 1;
            _count--;
            _version++;
            return item;
        }

        /// <summary>
        /// Removes and returns the last element. O(1).
        /// </summary>
        /// <returns>the element that was at the back.</returns>
        /// <exception cref="InvalidOperationException">the deque is empty.</exception>
        /// <example>
        /// <code>
        /// string tail = line.RemoveLast();
        /// </code>
        /// </example>
        public T RemoveLast()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The deque is empty, so it has no last element to remove.");
            }

            var index = ToBufferIndex(_count - 1);
            var item = _buffer[index];
            _buffer[index] = default!;
            _count--;
            _version++;
            return item;
        }

        /// <summary>
        /// Removes and returns the first element, or returns <c>false</c> without throwing when the
        /// deque is empty. O(1). This is the member to use when an empty deque is a normal outcome
        /// rather than a caller mistake.
        /// </summary>
        /// <param name="item">the removed first element, or <c>default</c> when the deque was empty.</param>
        /// <returns><c>true</c> when an element was removed.</returns>
        /// <example>
        /// <code>
        /// if (line.TryRemoveFirst(out var next)) { /* consume next */ }
        /// </code>
        /// </example>
        public bool TryRemoveFirst(out T item)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = RemoveFirst();
            return true;
        }

        /// <summary>
        /// Removes and returns the last element, or returns <c>false</c> without throwing when the
        /// deque is empty. O(1).
        /// </summary>
        /// <param name="item">the removed last element, or <c>default</c> when the deque was empty.</param>
        /// <returns><c>true</c> when an element was removed.</returns>
        /// <example>
        /// <code>
        /// if (line.TryRemoveLast(out var last)) { /* consume last */ }
        /// </code>
        /// </example>
        public bool TryRemoveLast(out T item)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = RemoveLast();
            return true;
        }

        /// <summary>
        /// Gets the first element without removing it. O(1).
        /// </summary>
        /// <returns>the element at the front.</returns>
        /// <exception cref="InvalidOperationException">the deque is empty.</exception>
        /// <example>
        /// <code>
        /// string head = line.GetFirst();
        /// </code>
        /// </example>
        public T GetFirst()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The deque is empty, so it has no first element.");
            }

            return _buffer[_head];
        }

        /// <summary>
        /// Gets the last element without removing it. O(1).
        /// </summary>
        /// <returns>the element at the back.</returns>
        /// <exception cref="InvalidOperationException">the deque is empty.</exception>
        /// <example>
        /// <code>
        /// string tail = line.GetLast();
        /// </code>
        /// </example>
        public T GetLast()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("The deque is empty, so it has no last element.");
            }

            return _buffer[ToBufferIndex(_count - 1)];
        }

        /// <summary>
        /// Gets the first element without removing it, or returns <c>false</c> without throwing
        /// when the deque is empty. O(1).
        /// </summary>
        /// <param name="item">the element at the front, or <c>default</c> when the deque was empty.</param>
        /// <returns><c>true</c> when the deque holds at least one element.</returns>
        public bool TryGetFirst(out T item)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _buffer[_head];
            return true;
        }

        /// <summary>
        /// Gets the last element without removing it, or returns <c>false</c> without throwing when
        /// the deque is empty. O(1).
        /// </summary>
        /// <param name="item">the element at the back, or <c>default</c> when the deque was empty.</param>
        /// <returns><c>true</c> when the deque holds at least one element.</returns>
        public bool TryGetLast(out T item)
        {
            if (_count == 0)
            {
                item = default!;
                return false;
            }

            item = _buffer[ToBufferIndex(_count - 1)];
            return true;
        }

        // ------------------------------------------------------------------
        // The list face - IList<T> / IReadOnlyList<T>
        // ------------------------------------------------------------------

        /// <summary>
        /// Adds the element at the back of the deque. O(1) amortized. This is
        /// <see cref="AddLast(T)"/> under the <see cref="ICollection{T}"/> name, so the deque
        /// behaves like <see cref="List{T}"/> (append at the back) and like <see cref="Queue{T}"/>
        /// (enqueue at the back) in generic code, and in a collection initializer.
        /// </summary>
        /// <example>
        /// <code>
        /// var line = new Deque&lt;string&gt; { "b", "c" };   // collection initializer
        /// line.Add("d");                               // b, c, d
        /// </code>
        /// </example>
        public void Add(T item)
        {
            AddLast(item);
        }

        /// <summary>
        /// Gets the position of the first element equal to <paramref name="item"/>, counting from
        /// the head, or <c>-1</c> when the deque holds none. O(n).
        /// </summary>
        /// <param name="item">the element to locate.</param>
        /// <example>
        /// <code>
        /// // line holds "b", "c", "b"
        /// line.IndexOf("b");                           // 0
        /// line.IndexOf("z");                           // -1
        /// </code>
        /// </example>
        public int IndexOf(T item)
        {
            var comparer = EqualityComparer<T>.Default;
            var length = _buffer.Length;
            for (var index = 0; index < _count; index++)
            {
                var position = _head + index;
                if (position >= length)
                {
                    position -= length;
                }

                if (comparer.Equals(_buffer[position], item))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// Inserts the element at the specified position, shifting the elements from there on
        /// towards the nearer end to make room. O(min(index, <see cref="Count"/> - index)), and O(1)
        /// amortized at either end.
        /// </summary>
        /// <param name="index">the position the element takes, from zero to <see cref="Count"/> inclusive.</param>
        /// <param name="item">the element to insert.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or
        /// greater than <see cref="Count"/>.</exception>
        /// <example>
        /// <code>
        /// // line holds "b", "c"
        /// line.Insert(1, "X");                         // b, X, c
        /// </code>
        /// </example>
        public void Insert(int index, T item)
        {
            if (index < 0 || index > _count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), "The index is outside the bounds of the deque.");
            }

            if (index == 0)
            {
                AddFirst(item);
                return;
            }

            if (index == _count)
            {
                AddLast(item);
                return;
            }

            EnsureCapacityForOneMore();

            var length = _buffer.Length;
            if (index <= _count / 2)
            {
                // Cheaper to move the head back and slide the front elements into the freed slot:
                // that touches `index` elements instead of `Count - index`.
                var newHead = _head == 0 ? length - 1 : _head - 1;
                for (var slot = 0; slot < index; slot++)
                {
                    var target = newHead + slot;
                    if (target >= length)
                    {
                        target -= length;
                    }

                    var source = target + 1;
                    if (source >= length)
                    {
                        source -= length;
                    }

                    _buffer[target] = _buffer[source];
                }

                var hole = newHead + index;
                if (hole >= length)
                {
                    hole -= length;
                }

                _buffer[hole] = item;
                _head = newHead;
            }
            else
            {
                // Slide the back elements up by one and fill the hole they leave behind.
                for (var slot = _count - 1; slot >= index; slot--)
                {
                    var source = _head + slot;
                    if (source >= length)
                    {
                        source -= length;
                    }

                    var target = source + 1;
                    if (target >= length)
                    {
                        target -= length;
                    }

                    _buffer[target] = _buffer[source];
                }

                var hole = _head + index;
                if (hole >= length)
                {
                    hole -= length;
                }

                _buffer[hole] = item;
            }

            _count++;
            _version++;
        }

        /// <summary>
        /// Removes the element at the specified position, sliding the nearer side over the hole.
        /// O(min(index, <see cref="Count"/> - 1 - index)), and O(1) at either end.
        /// </summary>
        /// <param name="index">the position to remove, from zero to <see cref="Count"/> - 1.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or
        /// greater than or equal to <see cref="Count"/>.</exception>
        /// <example>
        /// <code>
        /// // line holds "b", "X", "c"
        /// line.RemoveAt(1);                            // b, c
        /// </code>
        /// </example>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), "The index is outside the bounds of the deque.");
            }

            var length = _buffer.Length;
            if (index <= _count / 2)
            {
                // Slide the front elements up by one, then retire the vacated head slot: that
                // touches `index` elements instead of `Count - 1 - index`.
                for (var slot = index - 1; slot >= 0; slot--)
                {
                    var source = _head + slot;
                    if (source >= length)
                    {
                        source -= length;
                    }

                    var target = source + 1;
                    if (target >= length)
                    {
                        target -= length;
                    }

                    _buffer[target] = _buffer[source];
                }

                _buffer[_head] = default!;
                _head = _head + 1 == length ? 0 : _head + 1;
            }
            else
            {
                for (var slot = index; slot < _count - 1; slot++)
                {
                    var target = _head + slot;
                    if (target >= length)
                    {
                        target -= length;
                    }

                    var source = target + 1;
                    if (source >= length)
                    {
                        source -= length;
                    }

                    _buffer[target] = _buffer[source];
                }

                _buffer[ToBufferIndex(_count - 1)] = default!;
            }

            _count--;
            _version++;
        }

        /// <summary>
        /// Removes the first element equal to <paramref name="item"/> and returns <c>true</c>, or
        /// returns <c>false</c> when the deque holds none. O(n): the search is linear, and the
        /// removal slides the nearer side over the hole.
        /// </summary>
        /// <param name="item">the element to remove one copy of.</param>
        /// <returns><c>true</c> when an element was removed.</returns>
        /// <example>
        /// <code>
        /// // line holds "b", "c", "b"
        /// line.Remove("b");                            // true -&gt; c, b
        /// </code>
        /// </example>
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Determines whether the deque holds an element equal to <paramref name="item"/>. O(n).
        /// </summary>
        /// <param name="item">the element to look for.</param>
        /// <example>
        /// <code>
        /// bool present = line.Contains("c");
        /// </code>
        /// </example>
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }

        /// <summary>
        /// Copies the elements to the target array, in order from head to tail.
        /// </summary>
        /// <param name="array">the destination array.</param>
        /// <param name="arrayIndex">the position in <paramref name="array"/> the first element is written to.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is negative.</exception>
        /// <exception cref="ArgumentException">the elements do not fit between
        /// <paramref name="arrayIndex"/> and the end of <paramref name="array"/>.</exception>
        /// <example>
        /// <code>
        /// line.CopyTo(buffer, 0);
        /// </code>
        /// </example>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Array index can not be less than zero.");
            }

            if (arrayIndex + _count > array.Length)
            {
                throw new ArgumentException(
                    "The number of elements is greater than the available space from arrayIndex to the end of the target array.");
            }

            CopyElementsTo(array, arrayIndex);
        }

        // ------------------------------------------------------------------
        // Bulk operations
        // ------------------------------------------------------------------

        /// <summary>
        /// Removes every element and releases the references the buffer holds, so a deque of
        /// reference types does not keep its former contents alive. The buffer itself is kept, so
        /// refilling the deque does not pay for a new one — use <see cref="TrimExcess"/> as well
        /// when the memory matters more than the next fill.
        /// </summary>
        /// <example>
        /// <code>
        /// line.Clear();
        /// </code>
        /// </example>
        public void Clear()
        {
            if (_count == 0)
            {
                return;
            }

            var length = _buffer.Length;
            var tail = _head + _count;
            if (tail <= length)
            {
                Array.Clear(_buffer, _head, _count);
            }
            else
            {
                Array.Clear(_buffer, _head, length - _head);
                Array.Clear(_buffer, 0, tail - length);
            }

            _head = 0;
            _count = 0;
            _version++;
        }

        /// <summary>
        /// Shrinks the buffer to <see cref="Count"/> when more than a tenth of it is unused,
        /// releasing the slack a drained deque would otherwise keep. Does nothing when the buffer
        /// is already close to full, so calling it on every iteration of a loop costs one
        /// comparison rather than a copy per call.
        /// </summary>
        /// <example>
        /// <code>
        /// line.TrimExcess();                           // after draining a large window
        /// </code>
        /// </example>
        public void TrimExcess()
        {
            var threshold = (int)(_buffer.Length * 0.9);
            if (_count < threshold)
            {
                Capacity = _count;
            }
        }

        /// <summary>
        /// Copies the elements into a new array, in order from head to tail.
        /// </summary>
        /// <returns>an array holding a snapshot of the deque.</returns>
        /// <example>
        /// <code>
        /// string[] snapshot = line.ToArray();
        /// </code>
        /// </example>
        public T[] ToArray()
        {
            if (_count == 0)
            {
                return EmptyBuffer;
            }

            var result = new T[_count];
            CopyElementsTo(result, 0);
            return result;
        }

        /// <summary>
        /// Returns a live read-only view of the deque: enumeration, <see cref="Count"/> and
        /// positional reads reflect subsequent changes to the owning deque, and mutating members
        /// are not exposed.
        /// </summary>
        /// <remarks>
        /// The declared return type is <see cref="IReadOnlyList{T}"/> — wider than the
        /// <see cref="IReadOnlyCollection{T}"/> the rest of this package returns from
        /// <c>AsReadOnly()</c>. Those types keep the narrower declaration only because widening it
        /// now would be a binary-breaking change for existing consumers; this type is new, has no
        /// consumers to break, and its reason to be read through a view is exactly the O(1)
        /// positional access the narrower type would hide. It is still assignable to
        /// <see cref="IReadOnlyCollection{T}"/> wherever that is what a caller wants.
        /// </remarks>
        /// <example>
        /// <code>
        /// IReadOnlyList&lt;string&gt; view = line.AsReadOnly();
        /// view[0];                                     // O(1), live
        /// </code>
        /// </example>
        public IReadOnlyList<T> AsReadOnly()
        {
            return new ReadOnlyView(this);
        }

        /// <summary>
        /// Creates a copy of the deque holding the same elements in the same order. The copy is
        /// independent: adding to either one does not affect the other.
        /// </summary>
        /// <returns>a new deque holding a snapshot of this one.</returns>
        /// <example>
        /// <code>
        /// var snapshot = line.Clone();
        /// </code>
        /// </example>
        public Deque<T> Clone()
        {
            return new Deque<T>(this);
        }

        /// <summary>
        /// Returns the elements, comma separated, in order from head to tail.
        /// </summary>
        /// <example>
        /// <code>
        /// string text = line.ToString();
        /// </code>
        /// </example>
        public override string ToString()
        {
            return string.Join(",", this);
        }

        /// <summary>
        /// Enumerates the elements in order from head to tail. The returned enumerator is a
        /// <c>struct</c>, so a <c>foreach</c> over the deque allocates nothing.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var item in line) { }
        /// </code>
        /// </example>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // ------------------------------------------------------------------
        // Ring bookkeeping
        // ------------------------------------------------------------------

        /// <summary>
        /// Maps a position counted from the head onto a buffer slot. The head and the position are
        /// both below the buffer length, so their sum is below twice it and one subtraction wraps
        /// it — cheaper than a modulo on the hot path, and correct for every position the deque
        /// can address.
        /// </summary>
        private int ToBufferIndex(int index)
        {
            var position = _head + index;
            return position >= _buffer.Length ? position - _buffer.Length : position;
        }

        private T GetElement(int index)
        {
            return _buffer[ToBufferIndex(index)];
        }

        /// <summary>
        /// Grows the buffer by one doubling step when it is full. This is the only thing an add
        /// ever pays beyond one slot write, which is what makes the four end operations O(1)
        /// amortized rather than O(1) worst case.
        /// </summary>
        private void EnsureCapacityForOneMore()
        {
            if (_count < _buffer.Length)
            {
                return;
            }

            int target;
            if (_buffer.Length == 0)
            {
                target = DefaultCapacity;
            }
            else if ((uint)_buffer.Length > MaxCapacity / 2)
            {
                target = MaxCapacity;
            }
            else
            {
                target = _buffer.Length * 2;
            }

            if (target <= _buffer.Length)
            {
                throw new OutOfMemoryException("The deque has reached the largest capacity a single array can hold.");
            }

            SetCapacity(target);
        }

        /// <summary>
        /// Moves the ring into a buffer of the requested length, with the head at slot zero.
        /// </summary>
        private void SetCapacity(int value)
        {
            if (value == _buffer.Length)
            {
                return;
            }

            var newBuffer = value == 0 ? EmptyBuffer : new T[value];
            if (_count > 0)
            {
                CopyElementsTo(newBuffer, 0);
            }

            _buffer = newBuffer;
            _head = 0;
            _version++;
        }

        /// <summary>
        /// Copies the elements into <paramref name="array"/> in two runs at most, because the ring
        /// wraps at most once.
        /// </summary>
        private void CopyElementsTo(T[] array, int arrayIndex)
        {
            if (_count == 0)
            {
                return;
            }

            var length = _buffer.Length;
            var tail = _head + _count;
            if (tail <= length)
            {
                Array.Copy(_buffer, _head, array, arrayIndex, _count);
                return;
            }

            var firstRun = length - _head;
            Array.Copy(_buffer, _head, array, arrayIndex, firstRun);
            Array.Copy(_buffer, 0, array, arrayIndex + firstRun, _count - firstRun);
        }

        private sealed class ReadOnlyView : IReadOnlyList<T>
        {
            private readonly Deque<T> _owner;

            public ReadOnlyView(Deque<T> owner)
            {
                _owner = owner;
            }

            public int Count => _owner._count;

            public T this[int index] => _owner[index];

            public IEnumerator<T> GetEnumerator()
            {
                return _owner.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _owner.GetEnumerator();
            }
        }

        /// <summary>
        /// Enumerates a <see cref="Deque{T}"/> from head to tail. A <c>struct</c> like
        /// <see cref="List{T}.Enumerator"/>, so a <c>foreach</c> over the typed deque boxes
        /// nothing; it is also handed out through <see cref="IEnumerable{T}"/>, where it boxes as
        /// any enumerator does.
        /// </summary>
        public struct Enumerator : IEnumerator<T>
        {
            private readonly Deque<T> _deque;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(Deque<T> deque)
            {
                _deque = deque;
                _version = deque._version;
                _index = 0;
                _current = default!;
            }

            /// <summary>Gets the element at the current position.</summary>
            public T Current => _current;

            object? IEnumerator.Current => _current;

            /// <summary>
            /// Advances to the next element.
            /// </summary>
            /// <returns><c>false</c> when every element has been visited.</returns>
            /// <exception cref="InvalidOperationException">the deque was modified while it was
            /// being enumerated.</exception>
            public bool MoveNext()
            {
                if (_version != _deque._version)
                {
                    throw new InvalidOperationException(
                        "The deque was modified while it was being enumerated.");
                }

                if (_index >= _deque._count)
                {
                    _current = default!;
                    return false;
                }

                _current = _deque.GetElement(_index);
                _index++;
                return true;
            }

            /// <summary>Returns the enumerator to the head of the deque.</summary>
            /// <exception cref="InvalidOperationException">the deque was modified while it was
            /// being enumerated.</exception>
            public void Reset()
            {
                if (_version != _deque._version)
                {
                    throw new InvalidOperationException(
                        "The deque was modified while it was being enumerated.");
                }

                _index = 0;
                _current = default!;
            }

            /// <summary>Does nothing: the enumerator holds no resource to release.</summary>
            public void Dispose()
            {
            }
        }
    }
}
