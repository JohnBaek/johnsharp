using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JohnIsDev.Core.Models.Common.Enums;

namespace JohnIsDev.Core.Models.Common.Query;

/// <summary>
/// Query 요청 
/// </summary>
public class RequestQuery
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RequestQuery() {}

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="skip"></param>
    /// <param name="pageCount"></param>
    public RequestQuery(int skip, int pageCount)
    {
        Skip = skip;
        PageCount = pageCount;
    }


    /// <summary>
    /// 스킵
    /// </summary>
    [Required]
    [DefaultValue(0)]
    public int Skip { get; set; } = 0;

    /// <summary>
    /// 페이지 카운트 
    /// </summary>
    [Required]
    [DefaultValue(20)]
    public int PageCount { get; set; } = 20;

    /// <summary>
    /// (사용자로부터 입력 받음) 검색 키워드 
    /// </summary>
    public List<string> SearchKeywords { get; set; } = new List<string>();
    /// <summary>
    ///(사용자로부터 입력 받음)  검색 필드
    /// </summary>
    public List<string> SearchFields { get; set; } = new List<string>();   
    
    /// <summary>
    /// Greater then 
    /// </summary>
    public List<string> GreaterThenFields { get; set; } = new List<string>();   
    
    /// <summary>
    /// Greater then Keywords
    /// </summary>
    public List<string> GreaterThenValues { get; set; } = new List<string>();   
    
    /// <summary>
    /// Less then 
    /// </summary>
    public List<string> LessThenFields { get; set; } = new List<string>();   
    
    /// <summary>
    /// Less then Keywords
    /// </summary>
    public List<string> LessThenValues { get; set; } = new List<string>();   
    
    
    /// <summary>
    /// Date Fields
    /// </summary>
    public List<string> RangeDateFields { get; set; } = new List<string>();   
    
    /// <summary>
    /// Start Date Values
    /// </summary>
    public List<string> StartDateValues { get; set; } = new List<string>();
    
    /// <summary>
    /// End Date Values
    /// </summary>
    public List<string> EndDateValues { get; set; } = new List<string>();
    
    
    /// <summary>
    /// (사용자로부터 입력 받음) Sort 종류 
    /// </summary>
    public List<string> SortOrders { get; set; } = new List<string>();   
    /// <summary>
    ///(사용자로부터 입력 받음)  Sort 필드
    /// </summary>
    public List<string> SortFields { get; set; } = new List<string>();   
    

    /// <summary>
    /// 검색 메타 정보 
    /// </summary>
    [JsonIgnore]
    public List<RequestQuerySearchMeta> SearchMetas { get; set; } = [];
    
    
    /// <summary>
    /// Extra Header Names 
    /// </summary>
    public List<string> ExtraHeaders { get; set; } = new List<string>();

    /// <summary>
    /// Reset Meta Infos
    /// </summary>
    public void ResetMetas()
    {
        SearchMetas = [];
        ExtraHeaders = [];
    }
    
    /// <summary>
    /// 메타 정보를 추가한다.
    /// </summary>
    /// <param name="searchType"></param>
    /// <param name="fieldName"></param>
    public void AddSearchAndSortDefine(EnumQuerySearchType searchType, string fieldName)
    {
        SearchMetas.Add(new RequestQuerySearchMeta
        {
            SearchType = searchType ,
            Field = fieldName ,
        });
    }

    /// <summary>
    /// Adds a list of search and sort metadata to the current query, replacing any existing metadata.
    /// </summary>
    /// <param name="searchMetas">The list of SearchMeta objects that define the search and sort criteria.</param>
    /// <returns>The updated RequestQuery instance.</returns>
    public RequestQuery PrepareRanges(List<RequestQuerySearchMeta> searchMetas)
    {
        SearchMetas.Clear();
        SearchMetas.AddRange(searchMetas);
        return this;       
    }

    /// <summary>
    /// 메타 정보를 추가한다.
    /// </summary>
    /// <param name="searchType"></param>
    /// <param name="fieldName"></param>
    /// <param name="excelHeaderName"></param>
    /// <param name="useAsExcelHeader"></param>
    /// <param name="isSum"></param>
    /// <param name="enumType"></param>
    /// <param name="boolKeyword"></param>
    public void AddSearchAndSortDefine(EnumQuerySearchType searchType, string fieldName, string excelHeaderName , bool useAsExcelHeader = false, bool isSum = false ,Type? enumType = null, List<string>? boolKeyword = null)
    {
        SearchMetas.Add(new RequestQuerySearchMeta
        {
            SearchType = searchType ,
            Field = fieldName ,
            ExcelHeaderName = excelHeaderName ,
            IsIncludeExcelHeader = useAsExcelHeader ,
            IsSum = isSum ,
            EnumType = enumType ,
            BoolKeywords = boolKeyword
        });
    }
    
    /// <summary>
    /// AddSearchNumeric
    /// </summary>
    /// <param name="fieldName">Fields</param>
    public void AddSearchNumeric(string fieldName)
    {
        SearchMetas.Add(new RequestQuerySearchMeta
        {
            SearchType = EnumQuerySearchType.NumericOrEnums ,
            Field = fieldName ,
        });
    }

    /// <summary>
    /// Indicates whether the query request contains no sorting instructions.
    /// </summary>
    public bool HasNoSortRequest => SortOrders.Count == 0;

    /// <summary>
    /// Indicates whether the query request contains no search instructions.
    /// </summary>
    public bool HasNoSearchRequest => SearchMetas.Count == 0;
}