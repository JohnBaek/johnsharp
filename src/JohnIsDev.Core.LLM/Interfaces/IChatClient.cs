namespace JohnIsDev.Core.LLM.Interfaces;

/// <summary>
/// Represents a client interface for interacting with a chat service.
/// </summary>
public interface IChatClient
{
    /// <summary>
    /// Sends a question to the chat client and retrieves a response asynchronously.
    /// </summary>
    /// <param name="question">The question to be sent to the chat client.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response from the chat client.</returns>
    Task<string> AskAsync(string question);
    
    
}