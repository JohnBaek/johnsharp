using System.Diagnostics.CodeAnalysis;
using Bogus;
using FluentAssertions;
using JohnIsDev.Core.EntityFramework.Implements;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Responses;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JohnIsDev.Core.EntityFramework.Test.EFQueryProvider.implements;

/// <summary>
/// Provides a suite of tests for validating the behavior of the <see cref="QueryExecutor{TDbContext}"/> class.
/// Ensures that operations executed within transactions behave correctly and appropriately handle commits or rollbacks
/// based on the success or failure of the operation.
/// </summary>
/// <remarks>
/// This test class uses an in-memory SQLite database to emulate real database behavior for transactional operations.
/// The <see cref="QueryExecutor{TDbContext}"/> is tested to verify its integration with the database and its ability
/// to correctly handle transactions.
/// </remarks>
/// <seealso cref="QueryExecutor{TDbContext}"/>
/// <seealso cref="IDisposable"/>
/// <seealso cref="IAsyncDisposable"/>
[SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
public class QueryExecutorTests : IDisposable, IAsyncDisposable
{
    private readonly Mock<ILogger<QueryExecutor<TestDbContext>>> _mockLogger;
    private readonly QueryExecutor<TestDbContext> _queryExecutor;
    private readonly TestDbContext _dbContext;
    private readonly Faker<TestUser> _userFaker;
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<TestDbContext> _options;
    private readonly TestDbContextFactory _dbContextFactory;

    /// <summary>
    /// Unit tests for the <see cref="QueryExecutor{TDbContext}"/> class.
    /// Validates core functionality of the query execution mechanism for Entity Framework contexts.
    /// </summary>
    /// <remarks>
    /// Includes a test setup leveraging an in-memory SQLite database to validate the behavior of the query executor.
    /// Provides consistent data seeding via the <see cref="Faker{T}"/> library.
    /// Implements <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> for resource cleanup.
    /// </remarks>
    public QueryExecutorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new TestDbContext(_options);
        _dbContext.Database.EnsureCreated();

        _dbContextFactory = new TestDbContextFactory(_options);

        _mockLogger = new Mock<ILogger<QueryExecutor<TestDbContext>>>();
        _queryExecutor = new QueryExecutor<TestDbContext>(_mockLogger.Object, _dbContext);

        _userFaker = new Faker<TestUser>()
            .RuleFor(u => u.Id, f => f.IndexFaker++)
            .RuleFor(u => u.Name, f => f.Name.FullName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Age, f => f.Random.Int(18, 80))
            .RuleFor(u => u.IsActive, f => f.Random.Bool())
            .RuleFor(u => u.CreatedDate, f => f.Date.Past());
    }

    /// <summary>
    /// Tests that <see cref="QueryExecutor{TDbContext}.ExecuteWithTransactionAutoCommitAsync{TResponse}"/>
    /// commits database changes when the operation completes successfully.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test operation. The test verifies that:
    /// - The execution of the operation doesn't return null.
    /// - The result of the operation is <see cref="EnumResponseResult.Success"/>.
    /// - The changes made in the operation are persisted to the database.
    /// </returns>
    [Fact]
    public async Task ExecuteWithTransactionAsync_ShouldCommitOnSuccess()
    {
        // Arrange
        TestUser? testUser = _userFaker.Generate();

        // Act
        Response response = await _queryExecutor.ExecuteWithTransactionAutoCommitAsync<Response>(async dbContext =>
        {
            await dbContext.Users.AddAsync(testUser);
            await dbContext.SaveChangesAsync();
            return new Response(EnumResponseResult.Success, "", "");
        });

        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be(EnumResponseResult.Success);

        // Verify data is committed
        TestUser? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == testUser.Id);
        user.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that <see cref="QueryExecutor{TDbContext}.ExecuteWithTransactionAutoCommitAsync{TResponse}"/>
    /// rolls back database changes when an exception occurs during the operation.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous test operation. The test verifies that:
    /// - The execution of the operation doesn't return null.
    /// - The result of the operation is <see cref="EnumResponseResult.Error"/>.
    /// - Changes made before the exception are not committed to the database.
    /// </returns>
    [Fact]
    public async Task ExecuteWithTransactionAsync_ShouldRollbackOnException()
    {
        // Arrange
        TestUser? testUser = _userFaker.Generate();
    
        // Act
        Response response = await _queryExecutor.ExecuteWithTransactionAutoCommitAsync<Response>(async dbContext =>
        {
            await dbContext.Users.AddAsync(testUser);
            await dbContext.SaveChangesAsync();
            throw new Exception("Simulated failure");
        });
    
        // Assert
        response.Should().NotBeNull();
        response.Result.Should().Be(EnumResponseResult.Error);
    
        // Verify data is not committed
        TestUser? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == testUser.Id);
        user.Should().BeNull();
    }
    

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    // TestDbContextFactory for IDbContextFactory<TestDbContext>
    private class TestDbContextFactory : IDbContextFactory<TestDbContext>
    {
        private readonly DbContextOptions<TestDbContext> _options;

        public TestDbContextFactory(DbContextOptions<TestDbContext> options)
        {
            _options = options;
        }

        public TestDbContext CreateDbContext()
        {
            return new TestDbContext(_options);
        }

        public ValueTask<TestDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            return new ValueTask<TestDbContext>(new TestDbContext(_options));
        }
    }
}

// TestUser, TestDbContext는 QueryBuilderTests와 동일하게 사용
