using JohnIsDev.Core.LLM.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace JohnIsDev.Core.LLM.Implements;

/// <summary>
/// Represents a client implementation for interacting with the ChatGpt language model.
/// </summary>
public class ChatGptClient : IChatClient
{
    /// <summary>
    /// Represents the logger instance used for logging within the ChatGptClient class.
    /// </summary>
    private readonly ILogger<ChatGptClient> _logger;

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
    /// Represents the internal chat client instance used for communicating with the OpenAI ChatGpt service.
    /// </summary>
    private readonly ChatClient _client;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="logger">logger</param>
    /// <param name="configuration">configuration</param>
    /// <exception cref="Exception"></exception>
    public ChatGptClient(ILogger<ChatGptClient> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Throws an exception if the API key is not set in the appsettings.Development.json file.
        if (string.IsNullOrEmpty(configuration["ChatGpt:ApiKey"]) || string.IsNullOrEmpty(configuration["ChatGpt:Model"]) )
            throw new Exception("ChatGpt:ApiKey has not been set in the appsettings.Development.json file.");

        // Initialize ChatGptClient
        _apiKey = configuration["ChatGpt:ApiKey"] ?? "";
        _model = configuration["ChatGpt:Model"] ?? "";
        _client = new ChatClient(model: "gpt-4o", apiKey: _apiKey);
    }

    /// <summary>
    /// Sends a question to the ChatGpt client and retrieves a response asynchronously.
    /// </summary>
    /// <param name="question">The question to be sent to the ChatGpt client.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response from the ChatGpt client, or an empty string if an error occurs or no response is received.</returns>
    public async Task<string> AskAsync(string question)
    {
        try
        {
            // Send question to ChatGpt
            ChatCompletion completion = await _client.CompleteChatAsync(question);
            if(completion == null || completion.Content.Count == 0)
                return "";

            return completion.Content[0].Text;
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            return "";
        }
    }
}