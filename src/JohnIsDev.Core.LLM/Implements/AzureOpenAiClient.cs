using System.Runtime.CompilerServices;
using System.Net.Http.Headers;
using System.Text;
using JohnIsDev.Core.LLM.Interfaces;
using JohnIsDev.Core.LLM.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace JohnIsDev.Core.LLM.Implements;

/// <summary>
/// Azure OpenAI Client
/// </summary>
public class AzureOpenAiClient : IChatClient, IDisposable
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
            throw new Exception("AzureOpenAi:ApiKey has not been set in the appsettings.Development.json file.");

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
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            StringContent content = new StringContent(JsonConvert.SerializeObject(new
            {
                model = _model,
                input = question,
            }), Encoding.UTF8, "application/json");
            
            // Request to DirectLine API
            var response = await _httpClient.PostAsync("https://eastus2.api.cognitive.microsoft.com/openai/v1/responses", content);
            string responseContent = await response.Content.ReadAsStringAsync();
            
            if(! response.IsSuccessStatusCode)
            {
                _logger.LogError($"Azure OpenAI request failed with status code: {response.StatusCode}");
                return "";
            }
            
            // Deserialize response content
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            ResponseAzureOpenAi? deserialized = JsonSerializer.Deserialize<ResponseAzureOpenAi>(responseContent , options);
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

    /// <summary>
    /// Sends a question to the Azure OpenAI service and asynchronously streams the generated response content as it is received.
    /// </summary>
    /// <param name="question">The question or prompt to submit to the model.</param>
    /// <param name="cancellationToken">A token used to cancel the streaming request.</param>
    /// <returns>An asynchronous sequence of response text fragments returned by the model.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the client is not in a valid state to perform the request.</exception>
    /// <exception cref="IOException">Thrown when the response stream ends before the model response is completed.</exception>
    public async IAsyncEnumerable<string> AskStreamingAsync(
        string question,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = _model,
            input = question,
            stream = true
        }), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        // SSE 이벤트는 빈 줄로 구분되며 여러 data 줄이 하나의 이벤트를 구성할 수 있습니다.
        var data = new StringBuilder();
        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (!string.IsNullOrEmpty(line))
            {
                if (line.StartsWith("data:", StringComparison.Ordinal))
                    data.AppendLine(line[5..].TrimStart(' '));
                continue;
            }

            if (data.Length > 0)
            {
                string payload = data.ToString();
                data.Clear();
                if (payload.Trim() == "[DONE]")
                    break;

                using JsonDocument document = JsonDocument.Parse(payload);
                JsonElement item = document.RootElement;
                string? type = item.GetProperty("type").GetString();
                switch (type)
                {
                    case "response.output_text.delta":
                        string? delta = item.GetProperty("delta").GetString();
                        if (!string.IsNullOrEmpty(delta))
                            yield return delta;
                        break;
                    case "response.completed":
                        yield break;
                    case "error":
                    case "response.failed":
                    case "response.incomplete":
                        throw new InvalidOperationException($"모델 응답 스트림이 실패했습니다 ({type}).");
                }
            }

            if (line is null)
                break;
        }

        cancellationToken.ThrowIfCancellationRequested();
        throw new IOException("모델 응답이 완료되기 전에 스트림이 종료되었습니다.");
    }

    /// <summary>
    /// 클라이언트가 소유한 HTTP 자원을 해제합니다.
    /// </summary>
    public void Dispose() => _httpClient.Dispose();
}
