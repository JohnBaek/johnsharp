using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using JohnIsDev.Core.EntityFramework.Interfaces;
using JohnIsDev.Core.Features.Extensions;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Common.Query;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JohnIsDev.Core.EntityFramework.Implements;

/// <summary>
/// The QueryBuilder class provides functionality for dynamically building LINQ queries
/// based on user-defined filtering, sorting, and pagination criteria.
/// </summary>
/// <typeparam name="TDbContext">
/// The type of the database context, which must be a subclass of DbContext.
/// </typeparam>
public class QueryBuilder<TDbContext>(
    ILogger<QueryBuilder<TDbContext>> logger,
    TDbContext dbContext
) : IQueryBuilder<TDbContext> where TDbContext : DbContext
{
    /// <summary>
    /// A thread-safe cache for storing and reusing compiled expressions.
    /// This static dictionary is keyed by a string representing the unique identifier of an expression,
    /// allowing previously compiled expressions to be retrieved without recompilation for performance optimization.
    /// </summary>
    private static readonly ConcurrentDictionary<string, Expression> ExpressionCache = new();

    /// <summary>
    /// A thread-safe, static cache storing metadata information about entity properties.
    /// This dictionary is keyed by the entity type and stores an array of PropertyInfo objects,
    /// enabling efficient reuse and reduction of reflection overhead during runtime.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>
    /// Builds a query based on the provided request query.
    /// </summary>
    /// <typeparam name="T">The type of entity for the query.</typeparam>
    /// <param name="requestQuery">The request query object containing filters, sorting, and pagination criteria.</param>
    /// <returns>An IQueryable of type T that represents the built query, or null if not applicable.</returns>
    public IQueryable<T>? BuildQuery<T>(RequestQuery requestQuery) where T : class
        => BuildQuery(requestQuery , dbContext.Set<T>().AsNoTracking());


    /// <summary>
    /// Builds a query based on the provided request query and an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of entity for the query.</typeparam>
    /// <param name="requestQuery">An object containing filters, sorting, and pagination criteria.</param>
    /// <param name="queryable">The IQueryable instance to apply the query conditions to.</param>
    /// <returns>An IQueryable of type T with the applied query conditions, or null if an error occurs.</returns>
    // JohnIsDev.Core.EntityFramework.EFQueryProvider.Implements.QueryBuilder
    public IQueryable<T>? BuildQuery<T>(RequestQuery requestQuery, IQueryable<T> queryable) where T : class
{
    try
    {
        // Add Where Condition 
        Expression<Func<T, bool>>? whereCondition = CreateSearchConditions<T>(requestQuery);
        if (whereCondition != null)
            queryable = queryable.Where(whereCondition);
        
        IEnumerable<QuerySortOrder> sortOrders = ConvertToQuerySortList(requestQuery);
        bool isFirstSort = true;
        
        PropertyInfo[] cachedProperties = GetCachedProperties<T>();

        foreach (QuerySortOrder sortOrder in sortOrders)
        {
            PropertyInfo? propertyInfo = cachedProperties
                .FirstOrDefault(p => p.Name.Equals(sortOrder.Field, StringComparison.OrdinalIgnoreCase));

            if (propertyInfo == null)
                continue;

            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
            MemberExpression propertyAccess = Expression.Property(parameter, propertyInfo);
            LambdaExpression orderByExpression = Expression.Lambda(propertyAccess, parameter);

            string methodName;
            if (isFirstSort)
            {
                methodName = sortOrder.Order == EnumQuerySortOrder.Asc ? "OrderBy" : "OrderByDescending";
                isFirstSort = false;
            }
            else
            {
                methodName = sortOrder.Order == EnumQuerySortOrder.Asc ? "ThenBy" : "ThenByDescending";
            }

            MethodCallExpression resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), propertyInfo.PropertyType], 
                queryable.Expression,
                Expression.Quote(orderByExpression)
            );

            queryable = queryable.Provider.CreateQuery<T>(resultExpression);
        }
        return queryable;
    }
    catch (Exception e)
    {
        logger.LogError(e, e.Message);
        return null;
    }
}


    /// <summary>
    /// Converts the filter and search criteria from a RequestQuery into a list of QuerySearch objects.
    /// </summary>
    /// <typeparam name="T">The type of entity to which the query applies.</typeparam>
    /// <param name="requestQuery">The request query object containing search fields, keywords, and other filtering parameters.</param>
    /// <returns>A list of QuerySearch objects representing the transformed search criteria, or an empty list if no criteria are found.</returns>
    /// <exception cref="ArgumentException">Thrown when the number of search fields does not match the number of search keywords in the request query.</exception>
    public IEnumerable<QuerySearch> ConvertToQuerySearchList<T>(RequestQuery requestQuery) where T : class
        => ConvertToQuerySearchListInternal(requestQuery);

    // /// <summary>
    // /// Converts a queryable source into a paginated response list based on the specified request query.
    // /// </summary>
    // /// <typeparam name="T">The type of elements in the queryable source.</typeparam>
    // /// <param name="queryable">The queryable source to apply the request query filters and pagination.</param>
    // /// <param name="requestQuery">The request query containing pagination and filtering information.</param>
    // /// <returns>A task that represents the asynchronous operation. The task result contains a <see cref="ResponseList{T}"/>
    // /// populated with paginated and filtered data, along with additional pagination metadata.</returns>
    // public async Task<ResponseList<T>> ToResponseListAsync<T>(IQueryable<T> queryable, RequestQuery requestQuery)
    //     where T : class
    // {
    //     try
    //     {
    //         // Create a where statement by meta-information
    //         IQueryable<T>? buildQuery = BuildQuery(requestQuery, queryable);
    //         if (buildQuery == null)
    //             return new ResponseList<T>(EnumResponseResult.Success, "","", []);
    //         
    //         // Select a total count
    //         int totalCount = await buildQuery.AsNoTracking().CountAsync();
    //         
    //         // Select a paged list
    //         List<T> items = await buildQuery.AsNoTracking()
    //             .Skip(requestQuery.Skip)
    //             .Take(requestQuery.PageCount)
    //             .ToListAsync();
    //         
    //         return new ResponseList<T>(EnumResponseResult.Success, "", "", items)
    //         {
    //             TotalCount = totalCount ,
    //             Skip = requestQuery.Skip ,
    //             PageCount = requestQuery.PageCount 
    //         };
    //     }
    //     catch (Exception e)
    //     {
    //         logger.LogError(e, e.Message);
    //         return new ResponseList<T>(EnumResponseResult.Error, "COMMON_DATABASE_ERROR","", []);
    //     }
    // }
    //
    // /// <summary>
    // /// Converts a queryable data set to a response list with automatic mapping from the source type to the target type.
    // /// </summary>
    // /// <typeparam name="TQueryable">The type of the source queryable data.</typeparam>
    // /// <typeparam name="TConvert">The type to which the source data will be mapped.</typeparam>
    // /// <param name="queryable">The source queryable data to be converted.</param>
    // /// <param name="requestQuery">The request query object containing filters, sorting, and pagination criteria.</param>
    // /// <returns>A response list of type TConvert containing the converted data or an error status if the operation fails.</returns>
    // public async Task<ResponseList<TConvert>> ToResponseListAutoMappingAsync<TQueryable, TConvert>(
    //     IQueryable<TQueryable> queryable, RequestQuery requestQuery)
    //     where TConvert : class
    //     where TQueryable : class
    // {
    //     try
    //     {
    //         // Get a data ResponseList<TQueryable>
    //         ResponseList<TQueryable> result = await ToResponseListAsync(queryable, requestQuery);
    //         
    //         // Convert TQueryable to TConvert List Collection
    //         List<TConvert> convertList = new List<TConvert>(result.Items.Select(resultItem => resultItem.FromCopyValue<TConvert>()));
    //         
    //         return new ResponseList<TConvert>(EnumResponseResult.Success, "", "", convertList);
    //     }
    //     catch (Exception e)
    //     {
    //         logger.LogError(e, e.Message);
    //         return new ResponseList<TConvert>(EnumResponseResult.Error, "COMMON_DATABASE_ERROR","", []);
    //     }
    // }
    
    /// <summary>
    /// An ExpressionVisitor to replace a parameter in an expression with another one.
    /// This is crucial for combining two lambda expressions that were created with different parameter instances.
    /// </summary>
    private class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _parameter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterReplacer"/> class.
        /// </summary>
        /// <param name="parameter">The parameter to replace with.</param>
        internal ParameterReplacer(ParameterExpression parameter)
        {
            _parameter = parameter;
        }

        /// <summary>
        /// Visits the <see cref="ParameterExpression"/>.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified; otherwise, returns the original expression.</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            // Replace every parameter found with the one we passed in.
            return base.VisitParameter(_parameter);
        }
    }
    
    /// <summary>
    /// Create a where statement by meta-information
    /// </summary>
    private Expression<Func<T, bool>>? CreateSearchConditions<T>(RequestQuery requestQuery)
    {
        try
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "x");
            Expression? combinedExpression = null;
            IEnumerable<QuerySearch> convertedQuerySearches = ConvertToQuerySearchListInternal(requestQuery);
        
            foreach (QuerySearch querySearch in convertedQuerySearches)
            {
                RequestQuerySearchMeta? meta = requestQuery.SearchMetas
                    .Find(i => i.Field.Equals(querySearch.Field, StringComparison.CurrentCultureIgnoreCase));

                if (meta == null)
                    continue;
                
                var tempParameter = Expression.Parameter(typeof(T), "p");
                var memberExpression = Expression.Property(tempParameter, meta.Field);
            
                // MemberExpression memberExpression = Expression.Property(parameterExpression, meta.Field);
                List<string> searchKeywords = querySearch.Keyword?.Split(';').ToList() ?? [];
            
                Expression? singleCondition = CreateConditionExpression(meta, querySearch, memberExpression, searchKeywords);

                if (singleCondition == null)
                    continue;
                
                
                if (combinedExpression == null)
                {
                    combinedExpression = new ParameterReplacer(parameterExpression).Visit(singleCondition);
                }
                else
                {
                    var rewrittenCondition = new ParameterReplacer(parameterExpression).Visit(singleCondition);
                    combinedExpression = Expression.AndAlso(combinedExpression, rewrittenCondition);
                }
                
                //
                // combinedExpression = combinedExpression == null
                //     ? singleCondition
                //     : Expression.AndAlso(combinedExpression, singleCondition);
            }

            return combinedExpression != null
                ? Expression.Lambda<Func<T, bool>>(combinedExpression, parameterExpression)
                : null;
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
            return null;
        }
    }

    

    /// <summary>
    /// Create a where statement by meta-information
    /// </summary>
    /// <param name="requestQuery"></param>
    // private List<Expression<Func<T, bool>>> CreateSearchConditions<T>(RequestQuery requestQuery)
    // {
    //     List<Expression<Func<T, bool>>> conditions = [];
    //     try
    //     {
    //         // Convert Client request 
    //         IEnumerable<QuerySearch> convertedQuerySearches = ConvertToQuerySearchListInternal(requestQuery);
    //         
    //         // Process all
    //         foreach (QuerySearch querySearch in convertedQuerySearches)
    //         {
    //             // Find Target Meta 
    //             RequestQuerySearchMeta? meta = requestQuery.SearchMetas
    //                 .Find(i => 
    //                     i.Field.Equals(querySearch.Field, StringComparison.CurrentCultureIgnoreCase));
    //
    //             // Does not have a meta 
    //             if(meta == null)
    //                 continue;
    //             
    //             // Alias
    //             ParameterExpression parameterExpression = Expression.Parameter(typeof(T), "x");
    //             
    //             // MemberExpression
    //             MemberExpression memberExpression = Expression.Property(parameterExpression, meta.Field);
    //             
    //             // To Create a Keyword list by split ; 
    //             List<string> searchKeywords = querySearch.Keyword?.Split(';').ToList() ?? [];
    //             
    //             // Process By Meta types
    //             Expression? conditionExpression = CreateConditionExpression(meta: meta , query: querySearch , memberExpression: memberExpression ,keywords: searchKeywords);
    //             
    //             // If it cannot make
    //             if(conditionExpression == null)
    //                 continue;
    //             
    //             // Add Conditions
    //             conditions.Add(Expression.Lambda<Func<T, bool>>(conditionExpression, parameterExpression));
    //         }
    //     }
    //     catch (Exception e)
    //     {
    //         logger.LogError(e, e.Message);
    //     }
    //     return conditions;
    // }
    
    /// <summary>
    /// ConvertToQuerySearchList
    /// </summary>
    /// <param name="requestQuery">요청 정보</param>
    /// <returns></returns>
    private IEnumerable<QuerySearch> ConvertToQuerySearchListInternal(RequestQuery requestQuery)
    {
        List<QuerySearch> result = [];
        
        try
        {
            if (requestQuery is { SearchKeywords: not null, SearchFields: not null }
                && requestQuery.SearchFields.Count != requestQuery.SearchKeywords.Count)
            {
                throw new ArgumentException("Unmatched sort fields");
            }

            for (int i = 0; i < requestQuery.SearchFields?.Count; i++)
            {
                if(requestQuery.SearchKeywords == null)
                    continue;
                
                QuerySearch add = new QuerySearch
                {
                    Field = requestQuery.SearchFields[i] ,
                    Keyword = requestQuery.SearchKeywords[i]
                };
                result.Add(add);
            }
            
            for (int i = 0; i < requestQuery.GreaterThenFields.Count; i++)
            {
                // ReSharper disable once UnusedVariable
                if (double.TryParse(requestQuery.GreaterThenValues?[i], out double parsed))
                {
                    QuerySearch add = new QuerySearch
                    {
                        Field = requestQuery.GreaterThenFields[i] ,
                        Keyword = requestQuery.GreaterThenValues[i] ,
                        NumericType = EnumQuerySearchType.GreaterThen ,
                    };
                    result.Add(add);
                }
            }
            
            for (int i = 0; i < requestQuery.LessThenFields.Count; i++)
            {
                if(requestQuery.LessThenValues == null)
                    continue;

                // ReSharper disable once UnusedVariable
                if (double.TryParse(requestQuery.LessThenValues?[i], out double parsed))
                {
                    QuerySearch add = new QuerySearch
                    {
                        Field = requestQuery.LessThenFields[i] ,
                        Keyword = requestQuery.LessThenValues[i] ,
                        NumericType = EnumQuerySearchType.LessThen ,
                    };
                    result.Add(add);
                }
            }
            
            for (int i = 0; i < requestQuery.RangeDateFields.Count; i++)
            {
                // Validate
                if (requestQuery.StartDateValues[i].IsEmpty())
                    throw new ArgumentException("StartDateValues Count does not matched");
                if (requestQuery.EndDateValues[i].IsEmpty())
                    throw new ArgumentException("EndDateValues Count does not matched");
                
                // Try Parse
                if(DateTime.TryParse(requestQuery.StartDateValues[i].Trim(), out DateTime startDate) == false)
                    throw new ArgumentException($"{requestQuery.StartDateValues[i]} is Not Valid DateTime Format");
                if(DateTime.TryParse(requestQuery.EndDateValues[i].Trim(), out DateTime endDate) == false)
                    throw new ArgumentException($"{requestQuery.EndDateValues[i]} is Not Valid DateTime Format");
                
                // Add Query Search
                QuerySearch add = new QuerySearch
                {
                    Keyword = "",
                    Field = requestQuery.RangeDateFields[i] ,
                    StartDate = startDate ,
                    EndDate = endDate.AddDays(1) ,
                };
                result.Add(add);
            }
        }
        catch (Exception e)
        {
            e.LogError(logger);
        }
        return result;
    }
    
    /// <summary>
    /// Create a condition Expressions
    /// </summary>
    /// <param name="meta">meta</param>
    /// <param name="query">query</param>
    /// <param name="memberExpression">memberExpression</param>
    /// <param name="keywords">keywords</param>
    /// <returns></returns>
    private Expression? CreateConditionExpression(RequestQuerySearchMeta meta, QuerySearch query, MemberExpression memberExpression, List<string> keywords)
    {
        Expression? result = null;
        try
        {
            // Check has cache before Create an expression
            string cacheKey = "";
            if (meta.SearchType == EnumQuerySearchType.RangeDate)
            {
                cacheKey = $"{meta.Field}_{meta.SearchType}_{query.StartDate:yyyy-MM-dd}_{query.EndDate:yyyy-MM-dd} ";
            }
            else
            {
                cacheKey = $"{meta.Field}_{meta.SearchType}_{string.Join(",", keywords)}";
            }
            
            if(ExpressionCache.TryGetValue(cacheKey, out Expression? expression))
                return expression;
            
            // Process all Keywords
            foreach (string keyword in keywords)
            {
                ConstantExpression constant = GetParseConstant(keyword, memberExpression.Type);
                
                // By types
                switch (meta.SearchType)
                {
                    // Contains
                    case EnumQuerySearchType.Equals:
                    case EnumQuerySearchType.NumericOrEnums:
                        Expression equalExpression = Expression.Equal(memberExpression, constant);
                        result = result == null
                            ? equalExpression
                            : Expression.Or(result, equalExpression);
                        break;
                    
                    case EnumQuerySearchType.Boolean:
                        if (keywords == null || !keywords.Any())
                            continue;

                        bool boolValue;
                        if (keyword == "true" || keyword == "0")
                        {
                            boolValue = true;
                        }
                        else if (keyword == "false" || keyword == "1")
                        {
                            boolValue = false;
                        }
                        else
                        {
                            continue; 
                        }

                        constant = Expression.Constant(boolValue, memberExpression.Type);
                        equalExpression = Expression.Equal(memberExpression, constant);
                        result = result == null
                            ? equalExpression
                            : Expression.OrElse(result, equalExpression);
                        break;
                    
                    // Like
                    case EnumQuerySearchType.Like:
                        MethodInfo? likeMethod = typeof(DbFunctionsExtensions).GetMethod("Like", [typeof(DbFunctions), typeof(string), typeof(string)
                        ]);
                        if(likeMethod == null)
                            continue;
                        
                        Expression likeExpression = Expression.Call(null, likeMethod, Expression.Constant(EF.Functions), memberExpression,
                            Expression.Constant($"%{keyword}%"));
    
                        // 이전 결과와 OR 연산으로 결합
                        result = result == null
                            ? likeExpression
                            : Expression.OrElse(result, likeExpression);
                        break;
                    
                    // Range Date
                    case EnumQuerySearchType.RangeDate:
                        // Ensure StartDate and EndDate are DateTime
                        DateTime startDateValue = DateTime.Parse(query.StartDate.ToString("yyyy-MM-dd 00:00:00"));
                        DateTime endDateValue = DateTime.Parse(query.EndDate.ToString("yyyy-MM-dd 00:00:00"));

                        // Convert memberExpression to DateTime
                        var convertedMember = Expression.Convert(memberExpression, typeof(DateTime));

                        // Create expressions using DateTime
                        BinaryExpression startDate = Expression.GreaterThanOrEqual(convertedMember, Expression.Constant(startDateValue));
                        BinaryExpression endDate = Expression.LessThan(convertedMember, Expression.Constant(endDateValue));
                        result = Expression.AndAlso(startDate, endDate);
                        break;
                }
            }
            
            // Add Cache
            if(result != null)
                ExpressionCache.TryAdd(cacheKey, result);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }
        return result;
    }
        
    /// <summary>
    /// GetParseConstant
    /// </summary>
    /// <param name="target">target</param>
    /// <param name="property">property</param>
    /// <returns></returns>
    private ConstantExpression GetParseConstant(string target, Type property)
    {
        QuerySearch querySearch = new QuerySearch
        {
            Keyword = target,
            Field = ""
        };

        return GetParseConstant(querySearch, property);
    }
    
    /// <summary>
    /// GetParseConstant
    /// </summary>
    /// <param name="querySearch">querySearch</param>
    /// <param name="property">property</param>
    /// <returns></returns>
    private static ConstantExpression GetParseConstant(QuerySearch querySearch, Type property)
    {
        object? value = null;

        // Bool
        if (property == typeof(bool) && bool.TryParse(querySearch.Keyword, out bool boolValue))
        {
            value = boolValue;
        }
        // Int
        else if (property == typeof(int) && int.TryParse(querySearch.Keyword, out int intValue))
        {
            value = intValue;
        }
        // Float
        else if (property == typeof(float) && float.TryParse(querySearch.Keyword, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
        {
            value = floatValue;
        }
        // Double
        else if (property == typeof(double) && double.TryParse(querySearch.Keyword, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
        {
            value = doubleValue;
        }
        // Enum
        else if (property.IsEnum)
        {
            try
            {
                if (querySearch.Keyword != null)
                    value = Enum.Parse(property, querySearch.Keyword, true); 
            }
            catch (Exception)
            {
                value = 0;
            }
        }
    
        if (value != null)
        {
            return Expression.Constant(value, property);
        }
        return Expression.Constant(querySearch.Keyword, typeof(string));
    }
    
    /// <summary>
    /// Create OrderBy statements
    /// </summary>
    /// <typeparam name="T">Entity Type</typeparam>
    /// <param name="requestQuery">requestQuery</param>
    // private List<Expression<Func<T, object>>> CreateSortOrders<T>(RequestQuery requestQuery) 
    //     where T : class
    // {
    //     List<Expression<Func<T, object>>> orders = new List<Expression<Func<T, object>>>();
    //     IEnumerable<QuerySortOrder> sortOrders = ConvertToQuerySortList(requestQuery);
    //     foreach (QuerySortOrder sortOrder in sortOrders)
    //     {
    //         Expression<Func<T, object>>? orderExpression = CreateSingleSortExpression<T>(sortOrder);
    //         if (orderExpression != null)
    //             orders.Add(orderExpression);                      
    //     }
    //     return orders;
    // }

    /// <summary>
    /// Creates a single sort expression based on the provided sort order.
    /// </summary>
    /// <typeparam name="T">The type of the entity being sorted.</typeparam>
    /// <param name="sortOrder">The sort order that specifies the field and the direction for sorting.</param>
    /// <returns>An Expression that represents the sort operation or null if the property is not found or an error occurs.</returns>
    // private Expression<Func<T, object>>? CreateSingleSortExpression<T>(
    //     QuerySortOrder sortOrder) where T : class
    // {
    //     try
    //     {
    //         // Get Entity Type
    //         Type entityType = typeof(T);
    //         
    //         // Get PropertyInfo
    //         PropertyInfo? propertyInfo = GetCachedProperties<T>()
    //             .FirstOrDefault(i => i.Name.Equals(sortOrder.Field, StringComparison.OrdinalIgnoreCase));
    //         
    //         if (propertyInfo == null)
    //             return null;
    //         
    //         // Create parameter expression: x => x.Property
    //         ParameterExpression entityParameter = Expression.Parameter(typeof(T), "x");
    //         MemberExpression property = Expression.Property(entityParameter, propertyInfo);
    //
    //         
    //         // typeof(Queryable).GetMethods().First(
    //         //     method => method.Name == (sortOrder.Order == EnumQuerySortOrder.Asc ? "OrderBy" : "OrderByDescending") &&
    //         //               method.GetParameters().Length == 2).MakeGenericMethod(entityType, propertyInfo.PropertyType);
    //   
    //         // Convert to object for generic handling
    //         UnaryExpression convertToObject = Expression.Convert(property, typeof(object));
    //         
    //         return Expression.Lambda<Func<T, object>>(convertToObject, entityParameter);
    //         
    //         
    //         //
    //         // ParameterExpression queryParameter = Expression.Parameter(typeof(IQueryable<T>), "q");
    //         // ParameterExpression entityParameter = Expression.Parameter(entityType, "x");
    //         // MemberExpression property = Expression.Property(entityParameter, propertyInfo);
    //         // LambdaExpression propertyAccessLambda = Expression.Lambda(property, entityParameter);
    //         //
    //         // typeof(Queryable).GetMethods().First(
    //         //     method => method.Name == (sortOrder.Order == EnumQuerySortOrder.Asc ? "OrderBy" : "OrderByDescending") &&
    //         //               method.GetParameters().Length == 2).MakeGenericMethod(entityType, propertyInfo.PropertyType);
    //         //
    //         // MethodCallExpression orderByExpression = Expression.Call(
    //         //     typeof(Queryable),
    //         //     sortOrder.Order == EnumQuerySortOrder.Asc ? "OrderBy" : "OrderByDescending",
    //         //     [ entityType, propertyInfo.PropertyType ],
    //         //     queryParameter,
    //         //     propertyAccessLambda);
    //         //
    //         // return Expression.Lambda<Func<IQueryable<T>, IOrderedQueryable<T>>>(orderByExpression, queryParameter);
    //     }
    //     catch (Exception e)
    //     {
    //         logger.LogError(e, e.Message);
    //         return null;
    //     }
    // }
    //
    
    /// <summary>
    /// ConvertToQuerySortList
    /// </summary>
    /// <param name="requestQuery">requestQuery</param>
    /// <returns></returns>
    private IEnumerable<QuerySortOrder> ConvertToQuerySortList(RequestQuery requestQuery)
    {
        List<QuerySortOrder> result = [];
        
        try
        {
            if (requestQuery is { SortOrders: not null, SortFields: not null }
                && requestQuery.SortFields.Count != requestQuery.SortOrders.Count)
            {
                throw new ArgumentException("Unmatched sort fields");
            }

            for (int i = 0; i < requestQuery.SortFields?.Count; i++)
            {
                if(requestQuery.SortOrders == null)
                    continue;
                
                QuerySortOrder add = new QuerySortOrder
                {
                    Field = requestQuery.SortFields[i] ,
                    Order = ConvertStringToEnum(requestQuery.SortOrders[i])
                };
                result.Add(add);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, e.Message);
        }

        return result;
    }
    
    /// <summary>
    /// ConvertStringToEnum
    /// </summary>
    /// <param name="sortOrder">오더정보</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static EnumQuerySortOrder ConvertStringToEnum(string sortOrder)
    {
        if (Enum.TryParse<EnumQuerySortOrder>(sortOrder, true, out var result))
            return result;
        
        throw new ArgumentException("Invalid sort order value.");
    }

    /// <summary>
    /// Retrieves the cached properties for the specified entity type, or computes and caches them if not already cached.
    /// </summary>
    /// <typeparam name="T">The type of entity whose properties are to be retrieved.</typeparam>
    /// <returns>An array of PropertyInfo objects representing the properties of the given entity type.</returns>
    private PropertyInfo[] GetCachedProperties<T>()
        => PropertyCache.GetOrAdd(typeof(T), type => type.GetProperties());   
}