using System.Reflection;
using System.Text.RegularExpressions;
using Gridify;
using JohnIsDev.Core.Models.Common.Enums;
using JohnIsDev.Core.Models.Common.Query;

namespace JohnIsDev.Core.EntityFramework.Test.EFQueryProvider.implements;

/// <summary>
/// Prototype adapter that expresses a <see cref="RequestQuery"/> as a Gridify filter string.
/// </summary>
/// <remarks>
/// This lives in the test project on purpose. It lets the Gridify approach be measured against the current
/// QueryBuilder without adding a dependency to, or changing a single line of, the production library.
/// </remarks>
internal static class GridifyAdapter
{
    /// <summary>
    /// Applies only the filtering part of the request query, so the comparison stays focused on the WHERE clause.
    /// </summary>
    /// <typeparam name="T">The entity being queried.</typeparam>
    /// <param name="queryable">The source queryable.</param>
    /// <param name="requestQuery">The request carrying the search instructions.</param>
    /// <returns>The queryable with the Gridify filter applied.</returns>
    internal static IQueryable<T> ApplyFilter<T>(IQueryable<T> queryable, RequestQuery requestQuery) where T : class
    {
        string filter = BuildFilter(requestQuery);

        return filter.Length == 0
            ? queryable
            : queryable.ApplyFiltering(filter, CreateMapper<T>(requestQuery));
    }

    /// <summary>
    /// Builds the Gridify filter string out of every search instruction the request query carries.
    /// </summary>
    /// <param name="requestQuery">The request carrying the search instructions.</param>
    /// <returns>A Gridify filter string, empty when the request asks for no filtering.</returns>
    internal static string BuildFilter(RequestQuery requestQuery)
    {
        List<string> conditions = [];

        // SearchFields / SearchKeywords - one keyword may hold several values separated by ';', OR-ed together
        for (int i = 0; i < requestQuery.SearchFields.Count; i++)
        {
            RequestQuerySearchMeta? meta = requestQuery.SearchMetas
                .Find(m => m.Field.Equals(requestQuery.SearchFields[i], StringComparison.OrdinalIgnoreCase));

            if (meta == null)
                continue;

            string @operator = ToOperator(meta.SearchType);
            List<string> keywords = (requestQuery.SearchKeywords[i] ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            // Boolean keywords follow the legacy convention, where "0" reads as true and "1" as false.
            // Anything unrecognised is dropped, which is what the current implementation does too.
            if (meta.SearchType == EnumQuerySearchType.Boolean)
                keywords = keywords.Select(NormalizeBoolean).OfType<string>().ToList();

            if (keywords.Count > 0)
                conditions.Add(OrGroup(keywords.Select(k => $"{meta.Field}{@operator}{Escape(k)}")));
        }

        // RangeDate - the end date stays exclusive, matching the current implementation
        for (int i = 0; i < requestQuery.RangeDateFields.Count; i++)
        {
            if (!DateTime.TryParse(requestQuery.StartDateValues[i]?.Trim(), out DateTime startDate))
                continue;
            if (!DateTime.TryParse(requestQuery.EndDateValues[i]?.Trim(), out DateTime endDate))
                continue;

            string field = requestQuery.RangeDateFields[i];
            conditions.Add($"{field}>={startDate:yyyy-MM-dd},{field}<{endDate.AddDays(1):yyyy-MM-dd}");
        }

        // GreaterThen / LessThen
        for (int i = 0; i < requestQuery.GreaterThenFields.Count; i++)
            conditions.Add($"{requestQuery.GreaterThenFields[i]}>{Escape(requestQuery.GreaterThenValues[i])}");

        for (int i = 0; i < requestQuery.LessThenFields.Count; i++)
            conditions.Add($"{requestQuery.LessThenFields[i]}<{Escape(requestQuery.LessThenValues[i])}");

        // Global search - an OR group, AND-ed with everything above
        if (requestQuery.HasGlobalSearch)
        {
            string keyword = Escape(requestQuery.GlobalSearchKeyword!.Trim());

            conditions.Add(OrGroup(requestQuery.GlobalSearchFields
                .Where(f => requestQuery.SearchMetas.Any(m => m.Field.Equals(f, StringComparison.OrdinalIgnoreCase)))
                .Select(f => $"{f}=*{keyword}")));
        }

        return string.Join(",", conditions.Where(c => c.Length > 0));
    }

    /// <summary>
    /// Builds a mapper whose allowed fields are exactly the ones declared through the search metadata,
    /// which is what <c>[QueryMetaConvert]</c> produces today.
    /// </summary>
    /// <typeparam name="T">The entity being queried.</typeparam>
    /// <param name="requestQuery">The request carrying the search metadata.</param>
    /// <returns>A mapper restricted to the declared fields.</returns>
    internal static GridifyMapper<T> CreateMapper<T>(RequestQuery requestQuery)
    {
        GridifyMapper<T> mapper = new GridifyMapper<T>(new GridifyMapperConfiguration
        {
            CaseSensitive = false ,
            IgnoreNotMappedFields = true
        });
        mapper.GenerateMappings();

        HashSet<string> allowed = requestQuery.SearchMetas
            .Select(i => i.Field)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo property in typeof(T).GetProperties().Where(p => !allowed.Contains(p.Name)))
            mapper.RemoveMap(property.Name);

        return mapper;
    }

    /// <summary>
    /// Joins conditions with Gridify's OR operator, wrapping them in a group only when there is more than one.
    /// </summary>
    private static string OrGroup(IEnumerable<string> parts)
    {
        List<string> list = parts.ToList();

        return list.Count switch
        {
            0 => "" ,
            1 => list[0] ,
            _ => $"({string.Join("|", list)})"
        };
    }

    /// <summary>
    /// Translates a boolean keyword from the convention the current implementation uses into one Gridify parses.
    /// </summary>
    /// <param name="keyword">The raw keyword as the client sent it.</param>
    /// <returns><c>"true"</c>, <c>"false"</c>, or null when the keyword carries no boolean meaning.</returns>
    private static string? NormalizeBoolean(string keyword) => keyword switch
    {
        "true" or "0" => "true" ,
        "false" or "1" => "false" ,
        _ => null
    };

    /// <summary>
    /// Maps a search type onto the matching Gridify operator.
    /// </summary>
    private static string ToOperator(EnumQuerySearchType searchType) => searchType switch
    {
        EnumQuerySearchType.Like => "=*" ,
        EnumQuerySearchType.GreaterThen => ">" ,
        EnumQuerySearchType.LessThen => "<" ,
        _ => "="
    };

    /// <summary>
    /// Escapes the characters Gridify reads as operators. Gridify ships no helper for this, so every value
    /// reaching a filter string has to pass through here.
    /// </summary>
    private static string Escape(string? value)
        => Regex.Replace(value ?? "", @"([(),|\\]|/i)", @"\$1");
}
