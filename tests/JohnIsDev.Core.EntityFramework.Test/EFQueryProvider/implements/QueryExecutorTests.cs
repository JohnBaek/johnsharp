using System.Diagnostics.CodeAnalysis;
using Bogus;
using CommonModels;
using FluentAssertions;
using JohnIsDev.Core.EntityFramework.Implements;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Common.Query;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

#pragma warning disable CS0169 // Field is never used
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace JohnIsDev.Core.EntityFramework.Test.EFQueryProvider.implements;

/// <summary>
/// Unit test class for the <see cref="QueryBuilder"/>.
/// Contains tests that verify the functionality and correctness
/// of different methods within the QueryBuilder implementation.
/// </summary>
[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
public class QueryBuilderTests : IDisposable, IAsyncDisposable
{
    private readonly Mock<ILogger<QueryBuilder<TestDbContext>>> _mockLogger;
    private readonly QueryBuilder<TestDbContext> _queryBuilder;
    private readonly TestDbContext _dbContext;
    private readonly Faker<TestUser> _userFaker;
    private readonly SqliteConnection _connection;
    
    /// <summary>
    /// Setup for the tests.
    /// </summary>
    public QueryBuilderTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _dbContext = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options);
        _dbContext.Database.EnsureCreated();
        _mockLogger = new Mock<ILogger<QueryBuilder<TestDbContext>>>();
        _queryBuilder = new QueryBuilder<TestDbContext>(_mockLogger.Object, _dbContext);
        
        // Set Fakers 
        _userFaker = new Faker<TestUser>()
            .RuleFor(u => u.Id, f => f.IndexFaker++)
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Age, f => f.Random.Int(18, 80))
            .RuleFor(u => u.IsActive, f => f.Random.Bool())
            .RuleFor(u => u.CreatedDate, f => f.Date.Past());
    }

    /// <summary>
    /// Tests whether the QueryBuilder successfully builds an <see cref="IQueryable{T}"/>
    /// when provided with a valid <see cref="RequestQuery"/>.
    /// Ensures the returned queryable object is not null.
    /// </summary>
    [Fact]
    public void BuildQuery_WithValidRequestQuery_ShouldReturnIQueryable()
    {
        // Arrange
        List<TestUser> testUsers = _userFaker.Generate(100);
        _dbContext.Users.AddRange(testUsers);
        _dbContext.SaveChanges();

        // Set requestQuery to search by Name
        RequestQuery requestQuery = new RequestQuery
        {
            SearchFields = [ "Name" ] ,
            SearchKeywords = [ testUsers.First().Name ] ,
            SearchMetas = [ new ()
            {
                Field = "Name" ,
                SearchType = EnumQuerySearchType.Like
            }]
        };
        
        // Act
        IQueryable<TestUser>? queryable = _queryBuilder.BuildQuery<TestUser>(requestQuery);
        
        // Assert
        queryable.Should().NotBeNull();
        queryable.Should().NotBeEmpty();
        queryable.Should().BeAssignableTo<IQueryable<TestUser>>();
    }

    /// <summary>
    /// Tests that the query builder applies the correct sorting based on the provided sort order.
    /// </summary>
    /// <param name="sortOrder">The sorting order to be applied, either "Asc" for ascending or "Desc" for descending.</param>
    [Theory]
    [InlineData("Asc")]
    [InlineData("Desc")]
    public async Task BuildQuery_WithSortOrder_ShouldApplyCorrectSorting(string sortOrder)
    {
        // Arrange
        List<TestUser> testUsers = _userFaker.Generate(100);
        await _dbContext.Users.AddRangeAsync(testUsers);
        await _dbContext.SaveChangesAsync();
        
        // Set requestQuery to sort order by Age
        RequestQuery requestQuery = new RequestQuery
        {
            SortFields = [ "Age" ] ,
            SortOrders = [ sortOrder ] ,
            PageCount = 100
        };
        
        
        // Act
        IQueryable<TestUser>? queryable = _queryBuilder.BuildQuery<TestUser>(requestQuery);
        
        // Assert
        queryable.Should().NotBeNull();
        queryable.Should().NotBeEmpty();

        if (queryable == null)
            return;
        
        List<TestUser> result = await queryable.ToListAsync();
        result.Should().NotBeEmpty();
        
        if(sortOrder == "Asc")
            result.Should().BeInAscendingOrder(u => u.Age);
        
        if(sortOrder == "Desc")
            result.Should().BeInDescendingOrder(u => u.Age);
        
        result.Count.Should().Be(100);
    }


    // /// <summary>
    // /// Tests the functionality of the QueryBuilder to ensure that
    // /// it returns a correctly paged result set based on the provided paging parameters.
    // /// </summary>
    // /// <param name="skip">The number of records to skip from the start of the result set.</param>
    // /// <param name="pageCount">The number of records to include in a single page.</param>
    // [Theory]
    // [InlineData(0, 10)]
    // [InlineData(10, 20)]
    // [InlineData(30, 40)]
    // [InlineData(90, 110)]
    // public async Task BuildQuery_WithPaging_ShouldReturnPagedResults(int skip, int pageCount)
    // {
    //     // Arrange
    //     List<TestUser> testUsers = _userFaker.Generate(100);
    //     await _dbContext.Users.AddRangeAsync(testUsers);
    //     await _dbContext.SaveChangesAsync();
    //
    //     RequestQuery request = new RequestQuery
    //     {
    //         Skip = skip,
    //         PageCount = pageCount,
    //         SortFields = ["Id"],
    //         SortOrders = ["Asc"]
    //     };
    //     
    //     // Act
    //     IQueryable<TestUser>? queryable = _queryBuilder.BuildQuery<TestUser>(request);
    //     
    //     // Assert
    //     queryable.Should().NotBeNull();
    //     queryable.Should().NotBeEmpty();
    //     
    //     if (queryable == null)
    //         return;
    //     
    //     // Assert that the result set is correctly paged with page count
    //     List<TestUser> result = await queryable.ToListAsync();
    //     result.Count.Should().BeLessOrEqualTo(pageCount);
    //     
    //     List<int> expectedIds = testUsers.OrderBy(i => i.Id).Skip(skip).Take(pageCount).Select(i => i.Id).ToList();
    //     result.Select(i => i.Id).Should().BeEquivalentTo(expectedIds , options => options.WithStrictOrdering());
    // }

    /// <summary>
    /// Verifies that the query builder applies the correct "WHERE" condition
    /// based on search metadata provided within the request query.
    /// </summary>
    /// <param name="field">The name of the field to apply the search on.</param>
    /// <param name="searchType">The search condition type to apply (e.g., Equals, Like).</param>
    /// <param name="value">The value to be used for the field's search condition.</param>
    /// <returns>A task representing the asynchronous operation of the test.</returns>
    [Theory]
    [InlineData("Name", EnumQuerySearchType.Like, "JohnDoe")]
    [InlineData("Name", EnumQuerySearchType.Equals, "JohnDoe")]
    [InlineData("Age", EnumQuerySearchType.Equals, 25)]
    [InlineData("IsActive", EnumQuerySearchType.Equals, false)]
    public async Task BuildQuery_WithSearchMetas_ShouldApplyWhereCondition(
        string field, 
        EnumQuerySearchType searchType,
        object value)
    {
        // Arrange
        List<TestUser> testUsers =
        [
            new() { Id = 1, Name = "JohnDoe", Age = 30, IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 5, Name = "JohnDoe2", Age = 30, IsActive = true, CreatedDate = DateTime.Now },
            new() { Id = 2, Name = "JaneDoe", Age = 25, IsActive = false, CreatedDate = DateTime.Now },
            new() { Id = 3, Name = "Unknown", Age = 30, IsActive = true, CreatedDate = DateTime.Now }
        ];
        await _dbContext.Users.AddRangeAsync(testUsers);
        await _dbContext.SaveChangesAsync();
        
        RequestQuerySearchMeta meta = new RequestQuerySearchMeta
        {
            Field = field,
            SearchType = searchType
        };
        
        RequestQuery requestQuery = new RequestQuery
        {
            SearchMetas = [meta],
            SearchFields = [field],
            SearchKeywords = [ value.ToString() ?? "" ]
        };
        
        // Act
        IQueryable<TestUser>? queryable = _queryBuilder.BuildQuery<TestUser>(requestQuery);

        // Assert
        queryable.Should().NotBeNull();
        queryable.Should().NotBeEmpty();
        
        if (queryable == null)
            return;
        
        List<TestUser> result = await queryable.ToListAsync();

        if (value is string && value.ToString() == "JohnDoe" && searchType == EnumQuerySearchType.Equals)
        {
            result.Count.Should().Be(1);
            result[0].Name.Should().Be("JohnDoe");
        }
        
        if (value is string && value.ToString() == "JohnDoe" && searchType == EnumQuerySearchType.Like)
        {
            result.Count.Should().Be(2);
            result.Should().OnlyContain(u => u.Name.Contains("JohnDoe"));
        }
        
        if (value is int && field == "Age" && searchType == EnumQuerySearchType.Equals)
        {
            result.Count.Should().Be(1);
            result[0].Age.Should().Be(25);
        }
        
        if (value is bool && field == "IsActive" && searchType == EnumQuerySearchType.Equals)
        {
            result.Count.Should().Be(1);
            result[0].IsActive.Should().BeFalse();
        }
    }

    /// <summary>
    /// Verifies that a search condition built for one entity type is never reused for a different entity type
    /// that happens to declare a property with the same name and the same search type.
    /// </summary>
    /// <remarks>
    /// A condition expression holds a PropertyInfo bound to the entity it was built for. Any cache or reuse keyed
    /// on field name, search type and keyword alone - without the entity type - hands the second entity an
    /// expression pointing at the first entity's property. Rebinding that throws, the exception is swallowed by
    /// the query builder, the WHERE clause is dropped entirely and every row comes back. This test guards against
    /// such an optimisation being reintroduced.
    /// </remarks>
    /// <returns>A task representing the asynchronous operation of the test.</returns>
    [Fact]
    public async Task BuildQuery_WithSameFieldNameOnDifferentEntities_ShouldNotReuseCachedCondition()
    {
        // Arrange - both entities declare a "Name" string property, and exactly one row of each matches
        const string keyword = "CrossEntityCacheProbe";

        await _dbContext.Users.AddRangeAsync(
            new TestUser { Id = 1, Name = keyword, Age = 30, IsActive = true, CreatedDate = DateTime.Now },
            new TestUser { Id = 2, Name = "Unrelated", Age = 31, IsActive = true, CreatedDate = DateTime.Now });

        await _dbContext.Products.AddRangeAsync(
            new TestProduct { Id = 1, Name = keyword, Price = 1000 },
            new TestProduct { Id = 2, Name = "Unrelated", Price = 2000 },
            new TestProduct { Id = 3, Name = "AlsoUnrelated", Price = 3000 });

        await _dbContext.SaveChangesAsync();

        RequestQuery BuildRequest() => new()
        {
            SearchMetas = [ new RequestQuerySearchMeta { Field = "Name", SearchType = EnumQuerySearchType.Like } ],
            SearchFields = [ "Name" ],
            SearchKeywords = [ keyword ]
        };

        // Act - query TestUser first, then query TestProduct with the identical field name,
        //       search type and keyword
        IQueryable<TestUser>? users = _queryBuilder.BuildQuery<TestUser>(BuildRequest());
        IQueryable<TestProduct>? products = _queryBuilder.BuildQuery<TestProduct>(BuildRequest());

        // Assert
        users.Should().NotBeNull();
        products.Should().NotBeNull();

        List<TestUser> userResult = await users!.ToListAsync();
        userResult.Should().ContainSingle().Which.Name.Should().Be(keyword);

        List<TestProduct> productResult = await products!.ToListAsync();
        productResult.Should()
            .ContainSingle("the WHERE clause must be built from TestProduct.Name, not reused from TestUser.Name")
            .Which.Name.Should().Be(keyword);
    }

    /// <summary>
    /// Disposes the resources used by the test class. This method releases internal resources such as the database context
    /// and database connection, ensuring proper cleanup after the execution of the tests.
    /// </summary>
    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    /// <summary>
    /// Asynchronously disposes of resources used by the test class.
    /// Cleans up managed and unmanaged resources to ensure proper release
    /// of database and connection objects.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous dispose operation.
    /// </returns>
    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

/// <summary>
/// Represents a testing-specific implementation of the <see cref="DbContext"/> class.
/// Provides a framework for creating and managing in-memory or mock databases to enable
/// entity-based data operations during unit tests. Implements the <see cref="ICommonDbContext"/> interface
/// to provide access to custom database sets.
/// </summary>
public class TestDbContext : DbContext
{
    /// <summary>
    /// Represents a custom implementation of the <see cref="DbContext"/> class
    /// designed for testing purposes. Implements the <see cref="ICommonDbContext"/> interface.
    /// </summary>
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the DbSet representing the collection of users in the database.
    /// This property facilitates queries, additions, updates, and deletions
    /// of user data within the database context for testing purposes.
    /// </summary>
    public DbSet<TestUser> Users { get; set; }

    /// <summary>
    /// Gets or sets the DbSet representing the collection of products in the database.
    /// Exists so tests can verify that conditions built for one entity are not reused for another
    /// entity declaring a property of the same name.
    /// </summary>
    public DbSet<TestProduct> Products { get; set; }
}

/// <summary>
/// Represents a user entity with properties for identification, personal details,
/// and status within an application for test
/// </summary>
public class TestUser
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

/// <summary>
/// Represents a product entity for test. Declares a <c>Name</c> string property on purpose, so that it collides
/// with <see cref="TestUser.Name"/> in the query builder's expression cache key.
/// </summary>
public class TestProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Price { get; set; }
}
