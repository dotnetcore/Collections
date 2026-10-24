using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.Collections.Paginable.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Shouldly;
using Xunit;

namespace DotNetCore.Collections.Paginable.OrmQueryTests
{
    /// <summary>
    /// F8-03: the asynchronous face of the nine provider integrations, pinned as executable claims.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The package documents two things about the integrations: which of them accept a
    /// <see cref="CancellationToken"/> on their page fetch, and that the streaming face observes
    /// cancellation between pages. Both are claims about signatures and about where the token
    /// travels, so they are asserted here instead of being left to prose. A provider that gains a
    /// token overload - or loses one - breaks this suite, which forces the documentation to change
    /// in the same commit.
    /// </para>
    /// <para>
    /// Nothing here contacts a database. The matrix walks signatures. The runtime cases use EF
    /// Core's in-memory provider, a real <see cref="IQueryable{T}"/> implementation, so the same
    /// <c>CountAsync</c> / <c>ToListAsync</c> code path runs as against a server provider without
    /// opening a socket; the cancellation case wraps that provider in a recorder so the assertion
    /// is "the token reached the provider", not "something threw".
    /// </para>
    /// </remarks>
    public class AsyncSeamTests
    {
        // The nine integrations, as of 8.0.0. The three booleans are the published claims:
        // an async entry exists, and that entry accepts a cancellation token.
        private const string EntityFrameworkCore = "DotNetCore.Collections.Paginable.EntityFrameworkCore";
        private const string FreeSql = "DotNetCore.Collections.Paginable.FreeSql";
        private const string SqlSugar = "DotNetCore.Collections.Paginable.SqlSugar";
        private const string FreeSqlDbContext = "DotNetCore.Collections.Paginable.FreeSql.DbContext";
        private const string NHibernate = "DotNetCore.Collections.Paginable.NHibernate";
        private const string SqlKata = "DotNetCore.Collections.Paginable.SqlKata";
        private const string Chloe = "DotNetCore.Collections.Paginable.Chloe";
        private const string DosOrm = "DotNetCore.Collections.Paginable.DosOrm";
        private const string EntityFramework = "DotNetCore.Collections.Paginable.EntityFramework";

        /// <summary>
        /// The nine integrations with their published asynchronous shape:
        /// <c>assembly</c>, <c>has an async page entry</c>, <c>that entry takes a token</c>.
        /// </summary>
        public static IEnumerable<object[]> Integrations
        {
            get
            {
                yield return new object[] { EntityFrameworkCore, true, true };
                yield return new object[] { FreeSql, true, true };
                yield return new object[] { SqlSugar, true, true };
                yield return new object[] { FreeSqlDbContext, true, false };
                yield return new object[] { NHibernate, true, false };
                yield return new object[] { SqlKata, true, false };
                yield return new object[] { Chloe, false, false };
                yield return new object[] { DosOrm, false, false };
                yield return new object[] { EntityFramework, false, false };
            }
        }

        [Theory]
        [MemberData(nameof(Integrations))]
        public void The_async_entry_exists_exactly_where_the_documentation_says_it_does(
            string assemblyName, bool hasAsyncEntry, bool takesToken)
        {
            var entries = AsyncPageEntries(assemblyName);

            if (!hasAsyncEntry)
            {
                entries.ShouldBeEmpty(
                    $"{assemblyName} is documented as having no async page entry; it now has " +
                    $"{Describe(entries)}. Update the availability table in README and CHANGELOG.");
                return;
            }

            entries.ShouldNotBeEmpty($"{assemblyName} is documented as having an async page entry.");

            var withToken = entries.Where(HasCancellationToken).ToArray();

            if (takesToken)
            {
                withToken.ShouldNotBeEmpty(
                    $"{assemblyName} is documented as forwarding a CancellationToken to the provider, " +
                    "but no async page entry declares one.");
            }
            else
            {
                withToken.ShouldBeEmpty(
                    $"{assemblyName} is documented as accepting no CancellationToken, but " +
                    $"{Describe(withToken)} now does. The token can no longer be described as " +
                    "missing there - update README, CHANGELOG and the F8-04 note.");
            }
        }

        [Theory]
        [InlineData(EntityFrameworkCore)]
        [InlineData(FreeSql)]
        [InlineData(SqlSugar)]
        public void A_cancellable_integration_never_ships_a_page_fetch_that_cannot_be_cancelled(string assemblyName)
        {
            var fetches = AsyncPageEntries(assemblyName).Where(m => m.Name == "GetPageAsync").ToArray();

            fetches.ShouldNotBeEmpty();

            foreach (var fetch in fetches)
            {
                HasCancellationToken(fetch).ShouldBeTrue(
                    $"{assemblyName}: {Describe(fetch)} fetches a page without a CancellationToken, " +
                    "which makes the 'cancellation reaches the provider' claim false for that overload.");
            }
        }

        [Theory]
        [InlineData(FreeSqlDbContext)]
        [InlineData(NHibernate)]
        [InlineData(SqlKata)]
        public void A_token_less_integration_still_exposes_the_synchronous_page_fetch(string assemblyName)
        {
            // The advice for these three is "loop the synchronous page fetch"; that advice is only
            // valid while the synchronous entry is public. Pin it so the guidance cannot rot.
            var synchronous = StaticMethods(assemblyName).Where(m => m.Name == "GetPage").ToArray();

            synchronous.ShouldNotBeEmpty(
                $"{assemblyName} has no async entry and no synchronous GetPage left either, so the " +
                "documented fallback does not exist.");
        }

        [Fact]
        public async Task The_EF_Core_integration_fetches_a_page_asynchronously()
        {
            using var context = InMemoryContext();
            var query = context.Samples.OrderBy(x => x.Id);

            var page = await query.GetPageAsync(2, 2);

            page.TotalMemberCount.ShouldBe(5);
            page.TotalPageCount.ShouldBe(3);
            page.CurrentPageSize.ShouldBe(2);
            page[0].Value.Id.ShouldBe(3);
            page[1].Value.Id.ShouldBe(4);
        }

        [Fact]
        public async Task The_EF_Core_integration_builds_a_page_set_asynchronously()
        {
            using var context = InMemoryContext();
            var query = context.Samples.OrderBy(x => x.Id);

            // The page size is left to the configured default on purpose: passing it positionally
            // is ambiguous for an IQueryable receiver (pinned by
            // The_page_size_argument_is_typed_differently_on_the_two_candidates below), and this
            // case is about the asynchronous builder itself - its CountAsync round trip sets the
            // member count - not about the page size argument.
            var paginable = await query.ToPaginableAsync(cancellationToken: CancellationToken.None);

            paginable.MemberCount.ShouldBe(5);
            paginable.PageSize.ShouldBe(PaginableSettingsManager.Settings.DefaultPageSize);
            paginable.GetPage(1).TotalMemberCount.ShouldBe(5);
        }

        [Fact]
        public async Task A_cancelled_token_reaches_the_EF_Core_provider()
        {
            // Forwarding is asserted by observing the token at the provider boundary, not by
            // expecting an exception: whether a pre-cancelled token throws depends on the provider,
            // while "did the token get there at all" is the claim the documentation makes.
            using var context = InMemoryContext();

            // Ordered before wrapping: calling OrderBy on the wrapper would itself be ambiguous,
            // because RecordingQuery implements both IQueryable<T> and IAsyncEnumerable<T> and .NET
            // 10 ships an OrderBy for each.
            var ordered = context.Samples.AsQueryable().OrderBy(x => x.Id);
            var recorder = new RecordingQueryProvider(ordered.Provider);
            var recorded = new RecordingQuery<Sample>(recorder, ordered);

            using var source = new CancellationTokenSource();
            await recorded.GetPageAsync(1, 2, source.Token);

            recorder.LastToken.ShouldBe(source.Token);
        }

        [Fact]
        public async Task The_stream_over_an_ORM_backed_page_set_matches_the_synchronous_page_set()
        {
            using var context = InMemoryContext();
            var paginable = await EfBackedPageSet(context, pageSize: 2);

            var synchronous = paginable.Select(page => page.CurrentPageNumber).ToArray();

            var streamed = new List<int>();
            await foreach (var page in paginable.ToPagesAsync())
            {
                streamed.Add(page.CurrentPageNumber);
                page.CurrentPageSize.ShouldBe(
                    page.CurrentPageNumber < 3 ? 2 : 1,
                    "the last of five members at two per page holds one member");
            }

            streamed.ShouldBe(synchronous);
            streamed.ShouldBe(new[] { 1, 2, 3 });
        }

        [Fact]
        public async Task A_cancelled_token_stops_the_ORM_backed_stream_before_the_first_page()
        {
            using var context = InMemoryContext();
            var paginable = await EfBackedPageSet(context, pageSize: 2);

            using var source = new CancellationTokenSource();
            source.Cancel();

            // The stream reports cancellation rather than ending quietly: a consumer that asked to
            // stop must be able to tell "cancelled" from "there were no more pages".
            var streamed = 0;

            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var page in paginable.ToPagesAsync(source.Token))
                {
                    streamed++;
                    page.ShouldNotBeNull();
                }
            });

            streamed.ShouldBe(0, "the first page must not be materialised when the token is already cancelled");
        }

        [Fact]
        public void The_page_size_argument_is_typed_differently_on_the_two_candidates()
        {
            // Pinned because it changes how every EF Core consumer has to write the call. With an
            // IQueryable<T> receiver two ToPaginableAsync candidates are in scope: the package's own
            // IEnumerable<T> overload (page size typed int) and the EF Core integration's
            // IQueryable<T> overload (page size typed int?). Overload resolution has no winner -
            // the adapter has the better receiver, the core overload the better argument - so
            // `query.ToPaginableAsync(50)` does not compile. If the two are ever aligned, this test
            // fails and the documented call form has to change with it.
            var core = PageSizeParameter(typeof(PaginableEnumerable<Sample>).Assembly, typeof(IEnumerable<Sample>));
            var efCore = PageSizeParameter(Assembly.Load(EntityFrameworkCore), typeof(IQueryable<Sample>));

            core.ShouldBe(typeof(int));
            efCore.ShouldBe(typeof(int?));
        }

        // ----- helpers -------------------------------------------------------

        private static Type PageSizeParameter(Assembly assembly, Type receiver)
        {
            var candidates = assembly
                .GetTypes()
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => method.Name == "ToPaginableAsync")
                .Where(method => method.GetParameters().Length == 4)
                .Where(method => method.GetParameters()[0].ParameterType.IsGenericType &&
                                 method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == receiver.GetGenericTypeDefinition())
                .ToArray();

            candidates.Length.ShouldBe(1, $"expected one page-size overload taking {receiver.Name} in {assembly.GetName().Name}");
            return candidates[0].GetParameters()[1].ParameterType;
        }

        /// <summary>
        /// The page set an EF Core integration produces for a given page size: the adapter's
        /// asynchronous builder with its <c>CountAsync</c> round trip replaced by an explicit count,
        /// which is the same object it returns but without the ambiguous call site.
        /// </summary>
        private static async Task<PaginableQueryable<Sample>> EfBackedPageSet(SampleContext context, int pageSize)
        {
            var query = context.Samples.OrderBy(x => x.Id);
            var memberCount = await query.CountAsync();

            return new PaginableQueryable<Sample>(
                query, pageSize, PaginableCalc.GetRealPageCount(memberCount, pageSize), memberCount);
        }

        private static SampleContext InMemoryContext()
        {
            var options = new DbContextOptionsBuilder<SampleContext>()
                .UseInMemoryDatabase("f803-async-" + Guid.NewGuid().ToString("N"))
                .Options;

            var context = new SampleContext(options);
            context.Samples.AddRange(OrmFixture.Rows);
            context.SaveChanges();
            return context;
        }

        private static MethodInfo[] StaticMethods(string assemblyName)
        {
            Type[] types;
            try
            {
                types = Assembly.Load(assemblyName).GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // A provider assembly may fail to load a type this suite does not use; the rest of
                // the surface is still inspectable, so keep the types that did load.
                types = ex.Types.Where(type => type != null).ToArray()!;
            }

            return types
                .Where(type => type.IsPublic && type.IsAbstract && type.IsSealed)
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .ToArray();
        }

        private static MethodInfo[] AsyncPageEntries(string assemblyName) =>
            StaticMethods(assemblyName)
                .Where(method => method.Name == "GetPageAsync" || method.Name == "ToPaginableAsync")
                .ToArray();

        private static bool HasCancellationToken(MethodInfo method) =>
            method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken));

        private static string Describe(IEnumerable<MethodInfo> methods) =>
            string.Join(", ", methods.Select(Describe));

        private static string Describe(MethodInfo method) =>
            method.DeclaringType!.Name + "." + method.Name + "(" +
            string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name)) + ")";

        /// <summary>
        /// An <see cref="IQueryProvider"/> that remembers the token handed to it and then delegates,
        /// so "the token reached the provider" can be asserted directly.
        /// </summary>
        private sealed class RecordingQueryProvider : IAsyncQueryProvider
        {
            private readonly IQueryProvider _inner;

            internal RecordingQueryProvider(IQueryProvider inner) => _inner = inner;

            internal CancellationToken LastToken { get; private set; }

            public IQueryable CreateQuery(Expression expression) => _inner.CreateQuery(expression);

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
                new RecordingQuery<TElement>(this, _inner.CreateQuery<TElement>(expression));

            public object Execute(Expression expression) => _inner.Execute(expression);

            public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

            public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            {
                LastToken = cancellationToken;
                return ((IAsyncQueryProvider)_inner).ExecuteAsync<TResult>(expression, cancellationToken);
            }
        }

        /// <summary>
        /// A query that keeps <see cref="RecordingQueryProvider"/> in the provider slot while every
        /// other member is served by the real provider's own query.
        /// </summary>
        /// <remarks>
        /// <see cref="IAsyncEnumerable{T}"/> is implemented by delegation rather than by omission:
        /// from EF Core 8 onwards <c>ToListAsync</c> reaches the provider through
        /// <c>AsAsyncEnumerable</c>, which only accepts a source that already implements
        /// <see cref="IAsyncEnumerable{T}"/> — a plain <see cref="IQueryable{T}"/> wrapper makes EF
        /// throw instead of enumerating.
        /// </remarks>
        private sealed class RecordingQuery<T> : IOrderedQueryable<T>, IAsyncEnumerable<T>
        {
            private readonly IQueryProvider _provider;
            private readonly IQueryable<T> _inner;

            internal RecordingQuery(IQueryProvider provider, IQueryable<T> inner)
            {
                _provider = provider;
                _inner = inner;
            }

            public Type ElementType => typeof(T);

            public Expression Expression => _inner.Expression;

            public IQueryProvider Provider => _provider;

            public IEnumerator<T> GetEnumerator() => _inner.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
                _inner.AsAsyncEnumerable().GetAsyncEnumerator(cancellationToken);
        }
    }
}
