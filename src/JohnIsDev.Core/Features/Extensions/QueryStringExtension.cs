namespace JohnIsDev.Core.Features.Extensions;
using System.Web;

/// <summary>
/// 
/// </summary>
public static class QueryStringExtension
{
    /// <summary>
    /// ToQueryString
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string ToQueryString(this object request)
    {
        var properties = request.GetType().GetProperties()
            .Where(p => p.GetValue(request, null) != null)
            .Select(p => $"{HttpUtility.UrlEncode(p.Name)}={HttpUtility.UrlEncode(p.GetValue(request, null)?.ToString())}");

        return string.Join("&", properties);
    }
}