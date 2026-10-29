using System;

namespace DotNetCore.Collections.Testing.PropertyTesting
{
    /// <summary>
    /// Describes a stateful property: a machine that produces operations, applies them to the code
    /// under test, and can report whether an invariant still holds.
    /// </summary>
    /// <typeparam name="TOperation">The type describing one operation.</typeparam>
    /// <remarks>
    /// <para>
    /// The runner drives this machine in three phases and all three depend on the same two
    /// contracts:
    /// </para>
    /// <list type="number">
    /// <item><description>
    /// <see cref="Reset"/> must return the code under test to a state equivalent to "no operation
    /// applied yet". The runner calls it before every replay, so a property that forgets to reset
    /// would make shrinking report a sequence that reproduces nothing.
    /// </description></item>
    /// <item><description>
    /// <see cref="CheckInvariant"/> must be side-effect free and must return <c>null</c> (or an
    /// empty string) while the invariant holds, and a human-readable description of the violation
    /// otherwise. The description is what ends up in the failure report, so it should name the
    /// actual vs expected values.
    /// </description></item>
    /// </list>
    /// <para>
    /// Mutable and persistent implementations are both supported without changing this contract:
    /// an adapter for a mutable type applies the operation in place, while an adapter for a
    /// persistent type stores the instance returned by the operation in a field and replaces it
    /// on every <see cref="Apply"/> call. This is what lets one property suite accept both
    /// <c>MultiList&lt;T&gt;</c> today and the persistent types planned for 8.1.0.
    /// </para>
    /// </remarks>
    public abstract class StatefulProperty<TOperation>
    {
        /// <summary>
        /// Gets the name used in failure reports, for example <c>multiset/modell-equivalence</c>.
        /// </summary>
        /// <value>A stable, greppable identifier.</value>
        public abstract string Name { get; }

        /// <summary>
        /// Produces the next operation from the deterministic source.
        /// </summary>
        /// <param name="random">The source to draw from. Never <c>null</c>.</param>
        /// <returns>The operation to apply.</returns>
        /// <remarks>
        /// Only this member may consume randomness. Drawing from <paramref name="random"/> inside
        /// <see cref="Apply"/> would desynchronise replays and break shrinking.
        /// </remarks>
        public abstract TOperation NextOperation(DeterministicRandom random);

        /// <summary>
        /// Applies one operation to the code under test.
        /// </summary>
        /// <param name="operation">The operation returned by <see cref="NextOperation"/>.</param>
        public abstract void Apply(TOperation operation);

        /// <summary>
        /// Checks the invariant after the most recent <see cref="Apply"/> call.
        /// </summary>
        /// <returns>
        /// <c>null</c> or an empty string when the invariant holds; otherwise a description of the
        /// violation that includes the observed and expected values.
        /// </returns>
        public abstract string CheckInvariant();

        /// <summary>
        /// Returns the code under test to its initial state.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Renders one operation for the failure report.
        /// </summary>
        /// <param name="operation">The operation to render.</param>
        /// <returns>A short, replay-oriented description such as <c>Add(5)</c>.</returns>
        public abstract string Describe(TOperation operation);
    }
}
