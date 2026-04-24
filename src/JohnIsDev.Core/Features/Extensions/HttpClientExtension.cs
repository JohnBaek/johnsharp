using Newtonsoft.Json;

namespace JohnIsDev.Core.Features.Extensions;

/// <summary>
/// An Extension class for the HttpClient
/// </summary>
public static class HttpClientExtension
{
    /// <summary>
    /// Sends an HTTP GET request to the specified base URL with query parameters derived from the provided request object
    /// and returns the deserialized response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The type of the object expected in the response.
    /// </typeparam>
    /// <param name="client">
    /// The <see cref="HttpClient"/> instance used to send the request.
    /// </param>
    /// <param name="baseUrl">
    /// The base URL for the HTTP GET request.
    /// </param>
    /// <param name="request">
    /// An object whose properties are converted into query parameters for the request URL.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. Upon completion, the task contains
    /// the deserialized response object of type <typeparamref name="TResponse"/>, or null if deserialization fails.
    /// </returns>
    public static async Task<TResponse?> GetAsync<TResponse>(this HttpClient client, string baseUrl, object? request)
    {
        string fullUrl = baseUrl;
    
        HttpResponseMessage response = await client.GetAsync(fullUrl);
        response.EnsureSuccessStatusCode();
    
        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(responseBody);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="client"></param>
    /// <param name="url"></param>
    /// <typeparam name="TResponse"></typeparam>
    /// <returns></returns>
    public static async Task<TResponse?> PostAsync<TResponse>(this HttpClient client, string url , object request)
    {
        try
        {
            // Prepares a Request Object
            string requestJson = JsonConvert.SerializeObject(request);
            Console.WriteLine($"ProcessURL: {url}");
            StringContent content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        
            // Invokes a Post request to endpoint
            HttpResponseMessage response = await client.PostAsync(url, content);
            
            // Checks on response status
            response.EnsureSuccessStatusCode();
            
            // Reads a response body 
            string responseBody = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseBody);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"{url} Error");
            Console.Error.WriteLine(e);
            throw;
        }
    }


    /// <summary>
    /// Sends an HTTP PUT request with the specified request object to the provided URL
    /// and returns the deserialized response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The type of the object expected in the response.
    /// </typeparam>
    /// <param name="client">
    /// The <see cref="HttpClient"/> instance used to send the request.
    /// </param>
    /// <param name="url">
    /// The target URL for the HTTP PUT request.
    /// </param>
    /// <param name="request">
    /// The request object to be serialized and sent in the request body.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. Upon completion, the task contains
    /// the deserialized response object of type <typeparamref name="TResponse"/>, or null if deserialization fails.
    /// </returns>
    public static async Task<TResponse?> PutAsync<TResponse>(this HttpClient client, string url, object request)
    {
        // Prepares a Request Object
        string requestJson = JsonConvert.SerializeObject(request);
        StringContent content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
        
        // Invokes a Post request to endpoint
        HttpResponseMessage response = await client.PutAsync(url, content);
            
        // Checks on response status
        response.EnsureSuccessStatusCode();
            
        // Reads a response body 
        string responseBody = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(responseBody);
    }
}