using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DotNetCore.Collections;
using DotNetCore.Collections.Internal;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Tests
{
    /// <summary>
    /// F7-02: pins the <see cref="List{T}"/> field-layout assumption that the list arm of the
    /// dispatch depends on. The layout is what lets the enumerator index the backing array rather
    /// than go through the list indexer; the same assumption is re-verified at runtime on every
    /// .NET Framework generation by the framework probe.
    /// </summary>
    public class ListLayoutTests
    {
        [Fact]
        public void Backing_array_is_reached_through_the_field_layout()
        {
            var list = new List<int>(32) { 1, 2, 3 };

            var viaLayout = ListLayoutAccessor.GetItems(list);

            viaLayout.ShouldBeSameAs(ReflectedItems(list));
        }

        [Fact]
        public void The_layout_view_is_the_live_storage_not_a_copy()
        {
            var list = new List<int>(4) { 1 };
            var items = ListLayoutAccessor.GetItems(list);

            list.Add(2);

            ListLayoutAccessor.GetItems(list).ShouldBeSameAs(items);
            items.Take(list.Count).ShouldBe(new[] { 1, 2 });
        }

        [Fact]
        public void The_array_reached_through_the_layout_is_the_capacity_not_the_count()
        {
            var list = new List<int>(64) { 1, 2 };

            ListLayoutAccessor.GetItems(list).Length.ShouldBeGreaterThanOrEqualTo(64);
            ListLayoutAccessor.GetItems(list).Length.ShouldNotBe(list.Count);
        }

        [Fact]
        public void Layout_mirror_holds_for_reference_and_custom_element_types()
        {
            var strings = new List<string>(8) { "a", null };
            var points = new List<Point>(8) { new Point(1, 2) };

            ListLayoutAccessor.GetItems(strings).ShouldBeSameAs(ReflectedItems(strings));
            ListLayoutAccessor.GetItems(points).ShouldBeSameAs(ReflectedItems(points));
        }

        [Fact]
        public void The_sanctioned_span_view_describes_the_same_elements_as_the_layout()
        {
            var list = new List<int>(32) { 4, 5, 6 };

            var span = ListLayoutAccessor.AsReadOnlySpan(list);

            // On net5.0+ the span comes from CollectionsMarshal.AsSpan rather than from the
            // layout; the two must agree on the live elements.
            span.ToArray().ShouldBe(new[] { 4, 5, 6 });
            ListLayoutAccessor.GetItems(list).Take(list.Count).ShouldBe(span.ToArray());
        }

        [Fact]
        public void The_sanctioned_span_view_tracks_the_live_count_after_a_shrink()
        {
            var list = new List<int>(32) { 4, 5, 6, 7 };
            list.RemoveAt(3);

            ListLayoutAccessor.AsReadOnlySpan(list).ToArray().ShouldBe(new[] { 4, 5, 6 });
        }

        private static T[] ReflectedItems<T>(List<T> list)
        {
            var field = typeof(List<T>).GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
            field.ShouldNotBeNull();
            return (T[])field.GetValue(list);
        }

        private readonly struct Point
        {
            internal Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            internal int X { get; }

            internal int Y { get; }
        }
    }
}
