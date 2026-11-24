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
    /// <param name="cancellationToken">모델 요청과 응답 수신을 취소하는 토큰입니다.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the response from the chat client.</returns>
    Task<string> AskAsync(string question, CancellationToken cancellationToken = default);

    /// <summary>
    /// 답변이 생성될 때마다 텍스트 조각을 반환합니다.
    /// </summary>
    /// <param name="question">모델에 전달할 질문입니다.</param>
    /// <param name="cancellationToken">요청과 응답 수신을 취소하는 토큰입니다.</param>
    IAsyncEnumerable<string> AskStreamingAsync(string question, CancellationToken cancellationToken = default);
}
