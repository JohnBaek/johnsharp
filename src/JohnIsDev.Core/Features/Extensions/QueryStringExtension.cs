using System.Collections;
using System.Reflection;
using System.Web;

namespace JohnIsDev.Core.Features.Extensions;

/// <summary>
/// QueryString Extension
/// </summary>
public static class QueryStringExtension
{
    /// <summary>
    /// Object to QueryString
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public static string ToQueryString(this object request)
    {
        PropertyInfo[] properties = request.GetType().GetProperties();
        List<string> queryParams = new List<string>();

        // Process each property of the request object
        foreach (PropertyInfo property in properties)
        {
            object? value = property.GetValue(request, null);
            if (value == null) 
                continue;

            if (value is IEnumerable enumerable && !(value is string))
            {
                foreach (object? item in enumerable)
                {
                    if(item == null)
                        continue;
                    
                    queryParams.Add($"{HttpUtility.UrlEncode(property.Name)}={HttpUtility.UrlEncode(item.ToString())}");
                }
            }
            else
            {
                queryParams.Add($"{HttpUtility.UrlEncode(property.Name)}={HttpUtility.UrlEncode(value.ToString())}");
            }
        }

        return string.Join("&", queryParams);
    }
}