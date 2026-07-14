using JohnIsDev.Core.EntityFramework.Interfaces;
using JohnIsDev.Core.Extensions;
using JohnIsDev.Core.Features.Helpers;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Common.Query;
using JohnIsDev.Core.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace JohnIsDev.Core.EntityFramework.Implements;

/// <summary>
/// Provides an interface to execute operations within a transactional context.
/// Ensures that the specified operation is executed with transactional safety mechanisms.
/// </summary>
public class QueryExecutor<TDbContext>(
      ILogger<QueryExecutor<TDbContext>> logger
    ,TDbContext dbContext) : IQueryExecutor<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Executes the given operation within a database transaction asynchronously. Automatically commits the transaction if no exceptions occur.
    /// </summary>
    /// <typeparam name="TResponse">The response type inheriting from <see cref="Response"/> that the operation will return.</typeparam>
    /// <param name="operation">A function representing the database operation to execute. It receives the database context and returns a task of type <typeparamref name="TResponse"/>.</param>
    /// <param name="autoCommit">A boolean flag indicating whether the transaction should automatically commit. The default value is true.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response of type <typeparamref name="TResponse"/>.</returns>
    public async Task<TResponse> ExecuteWithTransactionAutoCommitAsync<TResponse>(
        Func<TDbContext, Task<TResponse>> operation, bool autoCommit = true)
        where TResponse : Response, new()
    {
        // Begin transactions
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            TResponse result = await operation(dbContext);
            
            if(autoCommit)
                await transaction.CommitAsync();
            
            return result;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, e.Message);
            return new TResponse
            {
                Result = EnumResponseResult.Error,
                Code = "TRANSACTION_ERROR",
                Message = "Error has been occurred. while executing transaction."
            };
        }
    }

    /// <summary>
    /// Executes the provided database operation within a transaction asynchronously. Automatically commits the transaction if no exceptions occur and autoCommit is set to true.
    /// </summary>
    /// <param name="operation">A function representing the database operation to execute. The function takes the database context of type <typeparamref name="TDbContext"/> and returns a task.</param>
    /// <param name="autoCommit">A boolean flag indicating whether the transaction should commit automatically. The default value is true.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task ExecuteWithTransactionAutoCommitAsync(Func<TDbContext, Task> operation, bool autoCommit = true)
    {
        // Begin transactions
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            if(autoCommit)
                await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, e.Message);
        }
    }

    /// <summary>
    /// Executes the provided operation within a database transaction asynchronously. If no exceptions occur, automatically commits the transaction based on the specified auto-commit flag.
    /// </summary>
    /// <typeparam name="TResponse">The response type inheriting from <see cref="Response"/> that the operation will return.</typeparam>
    /// <param name="operation">A function that performs the database operation. It accepts the database context and transaction as parameters and returns a task of type <typeparamref name="TResponse"/>.</param>
    /// <param name="autoCommit">A boolean flag indicating whether the transaction should automatically commit upon successful execution. The default value is true.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response of type <typeparamref name="TResponse"/>.</returns>
    public async Task<TResponse> ExecuteWithTransactionAutoCommitAsync<TResponse>(
        Func<TDbContext, IDbContextTransaction, Task<TResponse>> operation, bool autoCommit = true)
        where TResponse : Response, new()
    {
        // Begin transactions
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            TResponse result = await operation(dbContext, transaction);
            
            if(autoCommit)
                await transaction.CommitAsync();
            
            return result;
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            logger.LogError(e, e.Message);
            return new TResponse
            {
                Result = EnumResponseResult.Error,
                Code = "TRANSACTION_ERROR",
                Message = "Error has been occurred. while executing transaction."
            };
        }
    }

    /// <summary>
    /// Executes the specified query asynchronously and returns a paginated response containing the results.
    /// </summary>
    /// <param name="queryable">The queryable object representing the database query to execute.</param>
    /// <param name="requestQuery">An object containing pagination parameters such as skip and page count.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ResponseList{TDbContext}"/> with the query results and pagination details.</returns>
    public async Task<ResponseList<TEntity>> ExecuteAutoPaginateAsync<TEntity>(
        IQueryable<TEntity> queryable,
        RequestQuery requestQuery) where TEntity : class
    {
        try
        {
            // Select a total count
            int totalCount = await queryable.AsNoTracking().CountAsync();
            
            // Select a paged list
            List<TEntity> items = await queryable.AsNoTracking()
                .Skip(requestQuery.Skip)
                .Take(requestQuery.PageCount)
                .ToListAsync();
            
            return new ResponseList<TEntity>(EnumResponseResult.Success, "", "", items)
            {
                TotalCount = totalCount ,
                Skip = requestQuery.Skip ,
                PageCount = requestQuery.PageCount 
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new ResponseList<TEntity>(EnumResponseResult.Error, "COMMON_DATABASE_ERROR","", []);
        }
    }


    /// <summary>
    /// Converts a queryable source into a paginated response list based on the specified request query.
    /// </summary>
    /// <typeparam name="T">The type of elements in the queryable source.</typeparam>
    /// <param name="queryable">The queryable source to apply the request query filters and pagination.</param>
    /// <param name="requestQuery">The request query containing pagination and filtering information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ResponseList{T}"/>
    /// populated with paginated and filtered data, along with additional pagination metadata.</returns>
    public async Task<ResponseList<T>> ToResponseListAsync<T>(IQueryable<T> queryable, RequestQuery requestQuery)
        where T : class
    {
        try
        {
            requestQuery = requestQuery.PrepareRanges(EntityMapper.ToEntry<T>());
            
            // Select a total count
            int totalCount = await queryable.AsNoTracking().CountAsync();
            
            // Select a paged list
            List<T> items = await queryable.AsNoTracking()
                .Skip(requestQuery.Skip)
                .Take(requestQuery.PageCount)
                .ToListAsync();
            
            return new ResponseList<T>(EnumResponseResult.Success, "", "", items)
            {
                TotalCount = totalCount ,
                Skip = requestQuery.Skip ,
                PageCount = requestQuery.PageCount 
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new ResponseList<T>(EnumResponseResult.Error, "COMMON_DATABASE_ERROR","", []);
        }
    }
    
    
    /// <summary>
    /// Converts a queryable data set to a response list with automatic mapping from the source type to the target type.
    /// </summary>
    /// <typeparam name="TQueryable">The type of the source queryable data.</typeparam>
    /// <typeparam name="TConvert">The type to which the source data will be mapped.</typeparam>
    /// <param name="queryable">The source queryable data to be converted.</param>
    /// <param name="requestQuery">The request query object containing filters, sorting, and pagination criteria.</param>
    /// <returns>A response list of type TConvert containing the converted data or an error status if the operation fails.</returns>
    public async Task<ResponseList<TConvert>> ToResponseListAutoMappingAsync<TQueryable, TConvert>(
        IQueryable<TQueryable> queryable, RequestQuery requestQuery)
        where TConvert : class
        where TQueryable : class
    {
        try
        {
            requestQuery = requestQuery.PrepareRanges(EntityMapper.ToEntry<TConvert>());
            
            // Get a data ResponseList<TQueryable>
            ResponseList<TQueryable> result = await ToResponseListAsync(queryable, requestQuery);
            
            // Convert TQueryable to TConvert List Collection
            List<TConvert> convertList = new List<TConvert>(result.Items.Select(resultItem => resultItem.FromCopyValueUniversal<TConvert>()));
            
            // Convert ResponseList<TConvert>
            return new ResponseList<TConvert>
            {
                Result = result.Result,
                Code = result.Code,
                Message = result.Message,
                TotalCount = result.TotalCount,
                Skip = result.Skip,
                PageCount = result.PageCount,
                Items = convertList
            };
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return new ResponseList<TConvert>(EnumResponseResult.Error, "COMMON_DATABASE_ERROR","", []);
        }
    }

    /// <summary>
    /// Converts the given queryable collection into a response list with automatic mapping, using a new default request query.
    /// </summary>
    /// <typeparam name="TQueryable">The type of the elements in the input queryable collection.</typeparam>
    /// <typeparam name="TConvert">The type of the elements in the converted response list.</typeparam>
    /// <param name="queryable">The queryable collection to be converted into a response list.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ResponseList{TConvert}"/> with the converted elements.</returns>
    public async Task<ResponseList<TConvert>>
        ToResponseListAutoMappingAsync<TQueryable, TConvert>(IQueryable<TQueryable> queryable)
        where TQueryable : class where TConvert : class
        => await ToResponseListAutoMappingAsync<TQueryable, TConvert>(queryable, new RequestQuery(skip: 0, pageCount: int.MaxValue));
}