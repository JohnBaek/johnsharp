using FluentAssertions;
using JohnIsDev.Core.EntityFramework.Implements;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Common.Query;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace JohnIsDev.Core.EntityFramework.Test.EFQueryProvider.implements;

/// <summary>
/// Differential tests that pin the current QueryBuilder behaviour against a Gridify-based implementation.
/// </summary>
/// <remarks>
/// These are characterization tests: they do not assert what is correct, they assert that both
/// implementations agree. The current QueryBuilder is the specification here, because it is what a year of
/// production traffic has been running against. Any disagreement these tests surface is a migration risk
/// that has to be decided on deliberately, not a bug in either side.
/// </remarks>
public class QueryBuilderGridifyParityTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _dbContext;
    private readonly QueryBuilder<TestDbContext> _queryBuilder;
    private readonly ITestOutputHelper _output;

    /// <summary>
    /// Sets up an in-memory database and seeds it with rows chosen to expose the differences that matter.
    /// </summary>
    /// <param name="output">xUnit sink used to print the filter string and both SQL statements.</param>
    public QueryBuilderGridifyParityTests(ITestOutputHelper output)
    {
        _output = output;
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options);
        _dbContext.Database.EnsureCreated();
        _queryBuilder = new QueryBuilder<TestDbContext>(
            new Mock<ILogger<QueryBuilder<TestDbContext>>>().Object, _dbContext);

        Seed();
    }

    /// <summary>
    /// Seeds rows whose values deliberately contain every character Gridify treats as an operator.
    /// A parity test can only expose a difference that the seed data actually exercises, so the data is
    /// half the test.
    /// </summary>
    private void Seed()
    {
        _dbContext.Users.AddRange(
            new TestUser { Id = 1, Name = "Kim",     Email = "kim@a.com",    Age = 30, IsActive = true,  CreatedDate = new DateTime(2026, 01, 10) },
            new TestUser { Id = 2, Name = "Kim,Lee", Email = "kimlee@a.com", Age = 31, IsActive = false, CreatedDate = new DateTime(2026, 02, 10) },
            new TestUser { Id = 3, Name = "(주)한국", Email = "kr@a.com",     Age = 32, IsActive = true,  CreatedDate = new DateTime(2026, 03, 10) },
            new TestUser { Id = 4, Name = "A|B",     Email = "ab@a.com",     Age = 33, IsActive = false, CreatedDate = new DateTime(2026, 04, 10) },
            new TestUser { Id = 5, Name = "Lee",     Email = "lee@a.com",    Age = 30, IsActive = true,  CreatedDate = new DateTime(2026, 05, 10) });

        _dbContext.SaveChanges();
    }

    /// <summary>
    /// The case matrix. Only primitives go in here so xUnit can serialise each row into its own test case;
    /// the RequestQuery is assembled inside the test body.
    /// </summary>
    public static TheoryData<string, EnumQuerySearchType, string> SearchCases => new()
    {
        { "Name",     EnumQuerySearchType.Like,           "Kim"        },  // baseline
        { "Name",     EnumQuerySearchType.Like,           "Kim,Lee"    },  // ',' is Gridify's AND separator
        { "Name",     EnumQuerySearchType.Like,           "(주)한국"    },  // '(' ')' are Gridify's grouping
        { "Name",     EnumQuerySearchType.Like,           "A|B"        },  // '|' is Gridify's OR separator
        { "Name",     EnumQuerySearchType.Like,           "Kim;Lee"    },  // ';' is the legacy multi-keyword OR
        { "Name",     EnumQuerySearchType.Equals,         "Kim"        },
        { "Age",      EnumQuerySearchType.NumericOrEnums, "30"         },
        { "IsActive", EnumQuerySearchType.Boolean,        "true"       },
        { "IsActive", EnumQuerySearchType.Boolean,        "0"          },  // legacy reads "0" as TRUE
        { "IsActive", EnumQuerySearchType.Boolean,        "1"          }   // legacy reads "1" as FALSE
    };

    /// <summary>
    /// Runs one search instruction through both implementations and compares what comes back.
    /// </summary>
    /// <param name="field">The field to search on.</param>
    /// <param name="searchType">The declared search type, as <c>[QueryMetaConvert]</c> would set it.</param>
    /// <param name="keyword">The keyword a user would have typed.</param>
    /// <returns>A task representing the asynchronous operation of the test.</returns>
    [Theory]
    [MemberData(nameof(SearchCases))]
    public async Task Filtering_ShouldMatchLegacyImplementation(
        string field,
        EnumQuerySearchType searchType,
        string keyword)
    {
        // Arrange
        RequestQuery request = new RequestQuery
        {
            SearchMetas = [ new RequestQuerySearchMeta { Field = field, SearchType = searchType } ] ,
            SearchFields = [ field ] ,
            SearchKeywords = [ keyword ]
        };

        // Act - both sides are reduced to one normalised descriptor, so that "one of them threw" shows up as
        //       a readable difference instead of blowing the test up before anything is reported
        string legacy = await DescribeAsync(() =>
            _queryBuilder.BuildQuery<TestUser>(request, _dbContext.Users.AsNoTracking()));

        string gridify = await DescribeAsync(() =>
            GridifyAdapter.ApplyFilter(_dbContext.Users.AsNoTracking(), request));

        // Diagnostics - the SQL text differs even when the semantics agree, because Gridify parameterises
        //               its values. So it is printed for debugging, never asserted on.
        _output.WriteLine($"filter  : {GridifyAdapter.BuildFilter(request)}");
        _output.WriteLine($"legacy  : {legacy}");
        _output.WriteLine($"gridify : {gridify}");
        _output.WriteLine($"legacy SQL  : {Sql(() => _queryBuilder.BuildQuery<TestUser>(request, _dbContext.Users.AsNoTracking()))}");
        _output.WriteLine($"gridify SQL : {Sql(() => GridifyAdapter.ApplyFilter(_dbContext.Users.AsNoTracking(), request))}");

        // Assert
        gridify.Should().Be(legacy);
    }

    /// <summary>
    /// Records the one difference that is deliberately left in place: a keyword that cannot be parsed into
    /// the field's type.
    /// </summary>
    /// <remarks>
    /// Neither side is right here. The current implementation lets Expression.Equal throw on the type
    /// mismatch, swallows it, and drops the whole WHERE clause - so an unparsable keyword silently widens the
    /// result to every row. Gridify refuses the value instead. Making the adapter reproduce the current
    /// behaviour would mean porting a bug, so this test pins both behaviours as facts rather than asserting
    /// they agree. If either side ever changes, this is where it shows up.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation of the test.</returns>
    [Fact]
    public async Task Filtering_WithUnparsableNumericKeyword_IsAKnownDivergence()
    {
        // Arrange
        RequestQuery request = new RequestQuery
        {
            SearchMetas = [ new RequestQuerySearchMeta { Field = "Age", SearchType = EnumQuerySearchType.NumericOrEnums } ] ,
            SearchFields = [ "Age" ] ,
            SearchKeywords = [ "notanumber" ]
        };

        // Act
        string legacy = await DescribeAsync(() =>
            _queryBuilder.BuildQuery<TestUser>(request, _dbContext.Users.AsNoTracking()));

        string gridify = await DescribeAsync(() =>
            GridifyAdapter.ApplyFilter(_dbContext.Users.AsNoTracking(), request));

        _output.WriteLine($"legacy  : {legacy}");
        _output.WriteLine($"gridify : {gridify}");

        // Assert
        legacy.Should().Be("[1,2,3,4,5]",
            "the current implementation swallows the cast failure and drops the WHERE clause, returning every row");
        gridify.Should().Be("<ArgumentException>",
            "Gridify rejects the value rather than silently widening the result set");
    }

    /// <summary>
    /// Reduces a query to a comparable descriptor: the matching ids, or the name of whatever it threw.
    /// </summary>
    private static async Task<string> DescribeAsync(Func<IQueryable<TestUser>?> build)
    {
        try
        {
            IQueryable<TestUser>? queryable = build();

            if (queryable == null)
                return "<null queryable>";

            List<int> ids = await queryable.Select(u => u.Id).OrderBy(id => id).ToListAsync();
            return $"[{string.Join(",", ids)}]";
        }
        catch (Exception e)
        {
            return $"<{e.GetType().Name}>";
        }
    }

    /// <summary>
    /// Renders the SQL a query would run, or the failure that stopped it from being built.
    /// </summary>
    private static string Sql(Func<IQueryable<TestUser>?> build)
    {
        try
        {
            return build()?.ToQueryString().Replace(Environment.NewLine, " ") ?? "<null queryable>";
        }
        catch (Exception e)
        {
            return $"<{e.GetType().Name}: {e.Message}>";
        }
    }

    /// <summary>
    /// Releases the database context and the underlying in-memory connection.
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
