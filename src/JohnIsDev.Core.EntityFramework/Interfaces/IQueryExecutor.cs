using JohnIsDev.Core.Models.Common.Query;
using JohnIsDev.Core.Models.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace JohnIsDev.Core.EntityFramework.Interfaces;

/// <summary>
/// Provides an interface to execute operations within a transactional context.
/// Ensures that the specified operation is executed with transactional safety mechanisms.
/// </summary>
public interface IQueryExecutor<TDbContext> where TDbContext : DbContext
{
     
    /// <summary>
    /// Executes an operation within a transactional scope and optionally commits the transaction automatically.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response expected from the operation.</typeparam>
    /// <param name="operation">The function representing the operation to be executed within the transaction.</param>
    /// <param name="autoCommit">Indicates whether the transaction should be automatically committed upon successful execution. Defaults to true.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> ExecuteWithTransactionAutoCommitAsync<TResponse>(
        Func<TDbContext, Task<TResponse>> operation, bool autoCommit = true)
        where TResponse : Response, new();


    /// <summary>
    /// Executes an operation within a transactional scope and optionally commits the transaction automatically.
    /// </summary>
    /// <param name="operation">The function representing the operation to be executed within the transaction.</param>
    /// <param name="autoCommit">Indicates whether the transaction should be automatically committed upon successful execution. Defaults to true.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteWithTransactionAutoCommitAsync(
        Func<TDbContext, Task> operation, bool autoCommit = true);


    /// <summary>
    /// Executes an operation within a transactional scope and optionally commits the transaction automatically,
    /// allowing access to the database context and transaction object.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response expected from the operation.</typeparam>
    /// <param name="operation">The function representing the operation to be executed within the transaction.
    /// The function takes a database context and a transaction object as parameters and returns a task with the response of type <typeparamref name="TResponse"/>.</param>
    /// <param name="autoCommit">Indicates whether the transaction should be automatically committed upon successful execution. Defaults to true.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response of type <typeparamref name="TResponse"/>.</returns>
    Task<TResponse> ExecuteWithTransactionAutoCommitAsync<TResponse>(
        Func<TDbContext, IDbContextTransaction, Task<TResponse>> operation,
        bool autoCommit = true)
        where TResponse : Response, new();
    

    /// <summary>
    /// Executes a query operation with the provided queryable source and request query parameters,
    /// and returns a paginated list of items wrapped in a response.
    /// </summary>
    /// <param name="queryable">The queryable source of data to be executed.</param>
    /// <param name="requestQuery">The request query containing pagination and filtering options.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ResponseList{TDbContext}"/>
    /// with the result of the query.</returns>
    Task<ResponseList<TEntity>> ExecuteAutoPaginateAsync<TEntity>(IQueryable<TEntity> queryable, RequestQuery requestQuery) where TEntity : class;


    /// <summary>
    /// Converts the provided queryable into a paginated response list based on the request query parameters.
    /// </summary>
    /// <typeparam name="T">The type of the data contained in the response list.</typeparam>
    /// <param name="queryable">An IQueryable representing the data source to be converted into a response list.</param>
    /// <param name="requestQuery">The request query containing pagination and other filtering criteria.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a paginated response list of type <see cref="ResponseList{T}" />.</returns>
    Task<ResponseList<T>> ToResponseListAsync<T>(IQueryable<T> queryable, RequestQuery requestQuery)
        where T : class;

    /// <summary>
    /// Converts the provided queryable source into a paginated response list with elements automatically mapped to the target type.
    /// </summary>
    /// <typeparam name="TQueryable">The type of the elements in the original queryable source.</typeparam>
    /// <typeparam name="TConvert">The type of the elements in the converted response list.</typeparam>
    /// <param name="queryable">The IQueryable source to be converted and paginated.</param>
    /// <param name="requestQuery">The request query containing pagination and other query parameters.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a paginated response list of type <typeparamref name="TConvert"/>.</returns>
    Task<ResponseList<TConvert>> ToResponseListAutoMappingAsync<TQueryable, TConvert>(
        IQueryable<TQueryable> queryable, RequestQuery requestQuery)
        where TConvert : class
        where TQueryable : class;

    /// <summary>
    /// Maps a queryable source to a list of a different type and converts the result into a ResponseList.
    /// </summary>
    /// <typeparam name="TQueryable">The type of the elements in the queryable source.</typeparam>
    /// <typeparam name="TConvert">The type of the elements after mapping.</typeparam>
    /// <param name="queryable">The queryable source to be mapped and converted.</param>
    /// <param name="requestQuery">The request query containing pagination and filtering parameters.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a <see cref="ResponseList{TConvert}"/> with the converted items.</returns>
    Task<ResponseList<TConvert>> ToResponseListAutoMappingAsync<TQueryable, TConvert>(
        IQueryable<TQueryable> queryable)
        where TConvert : class
        where TQueryable : class;
}