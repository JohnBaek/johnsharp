namespace JohnIsDev.Core.MessageQue.Interfaces;

/// <summary>
/// Represents a message bus interface for asynchronous message publishing
/// </summary>
public interface IMessageBus : IDisposable
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="exchangeName"></param>
    /// <param name="routingKey"></param>
    /// <param name="exchangeType"></param>
    /// <param name="message"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task PublishAsync<T>(string exchangeName, string routingKey, string exchangeType, T message);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="queue"></param>
    /// <param name="routingKey"></param>
    /// <param name="exchangeType"></param>
    /// <param name="messageHandler"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task SubscribeAsync<T>(string queue, string routingKey,string exchangeType, Func<T,string, Task<bool>> messageHandler);

    /// <summary>
    /// Publishes a message for Remote Procedure Call (RPC) and waits for a response asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message to publish.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response message.</typeparam>
    /// <param name="exchangeName">The topic to which the message will be published.</param>
    /// <param name="routingKey">The routing key used for routing the message.</param>
    /// <param name="exchangeType">The exchange type to be used for message publication.</param>
    /// <param name="message">The request message to be sent.</param>
    /// <param name="timeoutSec">The timeout in seconds to wait for a response before completing the operation. Default is 10 seconds.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the response message of type <typeparamref name="TResponse"/> or null if no response is received within the specified timeout.</returns>
    Task<TResponse?> PublishRpcAsync<TRequest, TResponse>(
        string exchangeName,
        string routingKey,
        string exchangeType,
        TRequest message,
        int timeoutSec = 10);


    /// <summary>
    /// Subscribes to a queue for Remote Procedure Call (RPC) messages and processes them asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message expected.</typeparam>
    /// <typeparam name="TResponse">The type of the response message to return.</typeparam>
    /// <param name="queue">The name of the queue to subscribe to.</param>
    /// <param name="routingKey">The routing key used for message binding in the exchange.</param>
    /// <param name="exchangeType">The type of the exchange used for message routing.</param>
    /// <param name="messageHandler">
    /// A delegate that represents a function to handle the incoming messages.
    /// It receives the request message of type <typeparamref name="TRequest"/> and its routing key,
    /// and returns a task containing the response message of type <typeparamref name="TResponse"/>.
    /// </param>
    /// <returns>A task that represents the asynchronous operation of subscribing and handling messages.</returns>
    Task SubscribeRpcAsync<TRequest, TResponse>(
        string queue,
        string routingKey,
        string exchangeType,
        Func<TRequest, string, Task<TResponse>> messageHandler);


    /// <summary>
    /// Subscribes to a queue for Remote Procedure Call (RPC) messages and processes incoming requests using the provided message handler asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message that will be handled.</typeparam>
    /// <typeparam name="TResponse">The type of the response message that will be returned.</typeparam>
    /// <param name="queue">The name of the queue to subscribe to.</param>
    /// <param name="routingKey">The routing key used for filtering messages in the queue.</param>
    /// <param name="exchangeType">The type of the exchange associated with the queue.</param>
    /// <param name="messageHandler">A function that processes the request message and returns a response. The function accepts the request message of type <typeparamref name="TRequest"/> and a string containing message metadata, and returns a task with the response of type <typeparamref name="TResponse"/>.</param>
    /// <returns>A task that represents the asynchronous subscription operation.</returns>
    Task SubscribeRpcAsync<TRequest, TResponse>(
        string queue,
        string exchangeType,
        Func<TRequest, string, Task<TResponse>> messageHandler);
}