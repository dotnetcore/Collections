using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace DotNetCore.Collections.Paginable.OrmQueryTests
{
    /// <summary>One row of the table every provider in this suite is pointed at.</summary>
    public class Sample
    {
        /// <summary>Gets or sets the primary key.</summary>
        public int Id { get; set; }

        /// <summary>Gets or sets the row's name. Ordered on, so it has to be a string.</summary>
        /// <remarks>
        /// Declared without a nullable annotation on purpose: this project compiles in a
        /// nullable-oblivious context, where the annotation is itself the warning (CS8632). EF Core
        /// maps a reference-typed property to a nullable column by convention either way, so the
        /// model is unchanged.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>Gets or sets the amount. Filtered and aggregated on.</summary>
        public int Amount { get; set; }
    }

    /// <summary>The EF Core model. No database is ever contacted; only SQL generation is used.</summary>
    public class SampleContext : DbContext
    {
        /// <summary>Initialises the context.</summary>
        /// <param name="options">Context options.</param>
        public SampleContext(DbContextOptions<SampleContext> options) : base(options) { }

        /// <summary>Gets the row set.</summary>
        public DbSet<Sample> Samples => this.Set<Sample>();

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Sample>().ToTable("Samples");
            builder.Entity<Sample>().HasKey(x => x.Id);
            builder.Entity<Sample>().Property(x => x.Name).HasMaxLength(64);
            builder.Entity<Sample>().Property(x => x.Amount).HasColumnName("Amount");
        }
    }

    /// <summary>Shared data and helpers for the provider suites.</summary>
    internal static class OrmFixture
    {
        /// <summary>
        /// A connection string that names a server that is never contacted. Every provider in this
        /// suite builds SQL without opening a connection, which is what makes the suite runnable in
        /// a build gate that has no database.
        /// </summary>
        internal const string UnreachableSqlServer =
            "Server=.;Database=Nonexistent;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=1";

        internal static readonly Sample[] Rows =
        {
            new Sample { Id = 1, Name = "alpha", Amount = 1 },
            new Sample { Id = 2, Name = "bravo", Amount = 2 },
            new Sample { Id = 3, Name = "charlie", Amount = 3 },
            new Sample { Id = 4, Name = "delta", Amount = 4 },
            new Sample { Id = 5, Name = "echo", Amount = 5 },
        };

        /// <summary>
        /// One query expressed against <see cref="IQueryable{T}"/> only, so that the same delegate
        /// can be run once against the engine's face (and translated), and once natively against a
        /// provider (as the oracle).
        /// </summary>
        internal static readonly Func<IQueryable<Sample>, IQueryable<int>> Pipeline =
            source => source.Where(x => x.Amount > 1)
                .OrderBy(x => x.Name)
                .Skip(1)
                .Take(2)
                .Select(x => x.Id);

        /// <summary>
        /// Rewraps a model's key selector as the <c>Func&lt;T, object&gt;</c> shape a provider's native
        /// ordering API takes. The box has to be in the expression, not in a cast at the call site: an
        /// <c>int</c> key cannot be converted to <c>object</c> by delegate variance.
        /// </summary>
        internal static Expression<Func<Sample, object>> AsObjectKey(LambdaExpression keySelector) =>
            Expression.Lambda<Func<Sample, object>>(
                Expression.Convert(keySelector.Body, typeof(object)),
                keySelector.Parameters);

        /// <summary>
        /// Collapses whitespace so that two renderings of the same query can be compared as text
        /// without depending on how a provider wraps its output.
        /// </summary>
        internal static string Normalise(string sql)
        {
            var parts = sql.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts);
        }
    }
}
