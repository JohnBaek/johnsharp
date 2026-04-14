using System.Net.Http.Headers;
using JohnIsDev.Core.LLM.Interfaces;
using JohnIsDev.Core.LLM.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace JohnIsDev.Core.LLM.Implements;

/// <summary>
/// Azure OpenAI Client
/// </summary>
public class AzureOpenAiClient : IChatClient
{
    /// <summary>
    /// Represents the logger instance used for logging within the ChatGptClient class.
    /// </summary>
    private readonly ILogger<AzureOpenAiClient> _logger;

    /// <summary>
    /// Stores the API key required to authenticate requests made by the ChatGptClient.
    /// </summary>
    private readonly string _apiKey;

    /// <summary>
    /// Represents the identifier of the model used for the ChatGpt client operations.
    /// </summary>
    private readonly string _model;

    /// <summary>
    /// Represents the configuration settings utilized by the ChatGptClient class.
    /// </summary>
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Represents the HTTP client used for making HTTP requests to the Azure OpenAI API.
    /// </summary>
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// BaseUrl
    /// </summary>
    private const string BaseUrl = "https://eastus2.api.cognitive.microsoft.com/openai/v1/responses";
    
    /// <summary>
    /// Represents an Azure OpenAI client for interacting with the Azure-hosted OpenAI API.
    /// Implements the <see cref="IChatClient"/> interface.
    /// </summary>
    public AzureOpenAiClient(ILogger<AzureOpenAiClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Throws an exception if the API key is not set in the app settings.json file.
        if (string.IsNullOrEmpty(_configuration["AzureOpenAi:ApiKey"]) || string.IsNullOrEmpty(_configuration["AzureOpenAi:Model"]) )
            throw new Exception("AzureOpenAi:ApiKey has not been set in the appsettings.json file.");

        // Initialize AzureOpenAiClient
        _apiKey = _configuration["AzureOpenAi:ApiKey"] ?? "";
        _model = _configuration["AzureOpenAi:Model"] ?? "";
        
        _httpClient = new HttpClient();
    }
    
    /// <summary>
    /// Ask
    /// </summary>
    /// <param name="question"></param>
    /// <returns></returns>
    public async Task<string> AskAsync(string question)
    {
        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey); 
            
            // Request to DirectLine API
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            string responseContent = await response.Content.ReadAsStringAsync();
            
            if(! response.IsSuccessStatusCode)
            {
                _logger.LogError($"Azure OpenAI request failed with status code: {response.StatusCode}");
                return "";
            }
            
            // Deserialize response content
            ResponseAzureOpenAi? deserialized = JsonConvert.DeserializeObject<ResponseAzureOpenAi>(responseContent);
            if(deserialized == null)
                return "";
            
            foreach (var output in deserialized.Outputs.Where(output => output.Answers.Any(i => i.Answer != "")))
                return output.Answers.First(i => i.Answer != "").Answer;
            
            return "";
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return "";
        }
    }
}