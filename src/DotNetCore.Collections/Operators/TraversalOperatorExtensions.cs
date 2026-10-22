using System;
using System.Collections.Generic;

namespace DotNetCore.Collections
{
    /// <summary>
    /// The tree and graph walks of the P2 layer: <c>TraverseBreadthFirst</c> and
    /// <c>TraverseDepthFirst</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These two are the only operators of the engine that are not extension methods, and the reason
    /// is a rule rather than an oversight. Their natural receiver is the root node, whose type is a
    /// bare type parameter; an extension method on <c>TSource</c> would extend every type in the
    /// program, so <c>anything.TraverseBreadthFirst(...)</c> would appear in IntelliSense everywhere
    /// and the overload would be one careless <c>using</c> away from colliding with whatever else
    /// answers to that name. The namespace-governance contract in
    /// <c>NamespaceGovernanceTests</c> pins the engine's extension methods to engine-owned receivers
    /// for exactly that reason, so these are static methods on the same class.
    /// </para>
    /// <para>
    /// Both walks are lazy: children are asked for only when the walk reaches their parent, so an
    /// infinite tree is a legal input as long as the caller stops pulling. Neither detects cycles -
    /// a graph with one loops forever, which is the caller's contract to avoid or to break with a
    /// <c>Take</c>.
    /// </para>
    /// </remarks>
    public static partial class CollectionExtensions
    {
        /// <summary>Walks the tree from <paramref name="root"/> breadth first, level by level.</summary>
        /// <typeparam name="TSource">The type of the nodes.</typeparam>
        /// <param name="root">The node to start from. It is the first element of the walk.</param>
        /// <param name="childSelector">The children of a node. It must not return
        /// <see langword="null"/>; returning an empty sequence is how a leaf is expressed.</param>
        /// <returns>The nodes in breadth-first order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> or
        /// <paramref name="childSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TSource> TraverseBreadthFirst<TSource>(
            TSource root, Func<TSource, IEnumerable<TSource>> childSelector)
            => TraverseBreadthFirst(root, childSelector, node => node);

        /// <summary>Walks the tree from <paramref name="root"/> breadth first, projecting each
        /// node.</summary>
        /// <typeparam name="TSource">The type of the nodes.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="root">The node to start from. It is the first element of the walk.</param>
        /// <param name="childSelector">The children of a node. It must not return
        /// <see langword="null"/>; returning an empty sequence is how a leaf is expressed.</param>
        /// <param name="resultSelector">The projection of a node.</param>
        /// <returns>The projected nodes in breadth-first order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="root"/>,
        /// <paramref name="childSelector"/> or <paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> TraverseBreadthFirst<TSource, TResult>(
            TSource root, Func<TSource, IEnumerable<TSource>> childSelector,
            Func<TSource, TResult> resultSelector)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            if (childSelector is null) throw new ArgumentNullException(nameof(childSelector));
            if (resultSelector is null) throw new ArgumentNullException(nameof(resultSelector));

            return new EnumerableValueEnumerable<TResult>(
                BreadthFirstCore(root, childSelector, resultSelector));
        }

        /// <summary>Walks the tree from <paramref name="root"/> depth first, visiting a node before
        /// its children.</summary>
        /// <typeparam name="TSource">The type of the nodes.</typeparam>
        /// <param name="root">The node to start from. It is the first element of the walk.</param>
        /// <param name="childSelector">The children of a node. It must not return
        /// <see langword="null"/>; returning an empty sequence is how a leaf is expressed.</param>
        /// <returns>The nodes in depth-first pre-order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="root"/> or
        /// <paramref name="childSelector"/> is <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TSource> TraverseDepthFirst<TSource>(
            TSource root, Func<TSource, IEnumerable<TSource>> childSelector)
            => TraverseDepthFirst(root, childSelector, node => node);

        /// <summary>Walks the tree from <paramref name="root"/> depth first, projecting each node as
        /// it is visited.</summary>
        /// <typeparam name="TSource">The type of the nodes.</typeparam>
        /// <typeparam name="TResult">The type of the projected results.</typeparam>
        /// <param name="root">The node to start from. It is the first element of the walk.</param>
        /// <param name="childSelector">The children of a node. It must not return
        /// <see langword="null"/>; returning an empty sequence is how a leaf is expressed.</param>
        /// <param name="resultSelector">The projection of a node.</param>
        /// <returns>The projected nodes in depth-first pre-order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="root"/>,
        /// <paramref name="childSelector"/> or <paramref name="resultSelector"/> is
        /// <see langword="null"/>.</exception>
        public static EnumerableValueEnumerable<TResult> TraverseDepthFirst<TSource, TResult>(
            TSource root, Func<TSource, IEnumerable<TSource>> childSelector,
            Func<TSource, TResult> resultSelector)
        {
            if (root is null) throw new ArgumentNullException(nameof(root));
            if (childSelector is null) throw new ArgumentNullException(nameof(childSelector));
            if (resultSelector is null) throw new ArgumentNullException(nameof(resultSelector));

            return new EnumerableValueEnumerable<TResult>(
                DepthFirstCore(root, childSelector, resultSelector));
        }

        private static IEnumerable<TResult> BreadthFirstCore<TSource, TResult>(
            TSource root, Func<TSource, IEnumerable<TSource>> childSelector,
            Func<TSource, TResult> resultSelector)
        {
            var queue = new Queue<TSource>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                yield return resultSelector(node);

                foreach (var child in childSelector(node))
                {
                    queue.Enqueue(child);
                }
            }
        }

        private static IEnumerable<TResult> DepthFirstCore<TSource, TResult>(
            TSource root, Func<TSource, IEnumerable<TSource>> childSelector,
            Func<TSource, TResult> resultSelector)
        {
            var stack = new Stack<TSource>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return resultSelector(node);

                // A stack hands back what was pushed last, so the children are buffered and pushed in
                // reverse to keep the walk reading left to right. That buffer is the one allocation
                // per node the depth-first walk cannot avoid without keeping a stack of enumerators
                // instead of a stack of nodes.
                var children = new List<TSource>(childSelector(node));
                for (var i = children.Count - 1; i >= 0; i--)
                {
                    stack.Push(children[i]);
                }
            }
        }
    }
}
