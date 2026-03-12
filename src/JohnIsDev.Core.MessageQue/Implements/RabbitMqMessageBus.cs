using System.Text;
using JohnIsDev.Core.MessageQue.Interfaces;
using JohnIsDev.Core.MessageQue.Models.Configs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace JohnIsDev.Core.MessageQue.Implements;

/// <summary>
/// The RabbitMqMessageBus class provides functionality for managing and interacting with a message queue
/// using RabbitMQ. It serves as a message bus implementation to enable communication between different
/// parts of the application or distributed system.
/// Should be singleton
/// </summary>
public class RabbitMqMessageBus : IMessageBus
{
    /// <summary>
    /// Represents the logging mechanism used to capture, record, and output diagnostic or operational
    /// information for the <see cref="RabbitMqMessageBus"/>. This variable is of type
    /// <see cref="ILogger{RabbitMqMessageBus}"/> and is utilized to log messages such as errors,
    /// warnings, or informational details during the operation of the message bus,
    /// including publishing and subscribing to messages.
    /// </summary>
    private readonly ILogger<RabbitMqMessageBus> _logger;

    /// <summary>
    /// Represents the connection interface to the RabbitMQ server. This variable is of type
    /// <see cref="IConnection"/> and is used by the <see cref="RabbitMqMessageBus"/> to establish
    /// and manage communication with the RabbitMQ message broker, including creating channels,
    /// publishing messages, and subscribing to queues. The connection is intended to persist
    /// throughout the lifecycle of the <see cref="RabbitMqMessageBus"/> to avoid the overhead
    /// of repeatedly opening and closing connections.
    /// </summary>
    private readonly IConnection _connection;

    /// <summary>
    /// Represents the configuration settings required to establish a connection and interact
    /// with the RabbitMQ message broker. This includes configuration options such as the host name,
    /// port, user credentials, virtual host, and exchange type. This variable is of type
    /// <see cref="RabbitMqConfig"/> and is used internally by the <see cref="RabbitMqMessageBus"/>
    /// to configure its connection to RabbitMQ.
    /// </summary>
    private readonly RabbitMqConfig _config;

    /// <summary>
    /// Maintains a collection of subscribed channels represented by <see cref="IChannel"/> instances
    /// that are utilized for managing and processing message subscriptions within the RabbitMQ message
    /// bus system. This list is used to track active subscription channels and ensure proper handling
    /// of incoming messages for various topics and routing keys.
    /// </summary>
    private readonly List<IChannel> _subscribeChannels = [];

    /// <summary>
    /// Represents a collection of asynchronous event-based consumers, specifically of type
    /// <see cref="AsyncEventingBasicConsumer"/>. These consumers are responsible for receiving
    /// and processing messages from RabbitMQ. The collection is utilized to manage and coordinate
    /// multiple consumers within the scope of the message bus, enabling subscriptions to various
    /// message queues and topics.
    /// </summary>
    private readonly List<AsyncEventingBasicConsumer> _consumers = [];
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="connection"></param>
    /// <param name="config"></param>
    public RabbitMqMessageBus(ILogger<RabbitMqMessageBus> logger, IConnection connection, RabbitMqConfig config)
    {
        _logger = logger;
        _connection = connection;
        _config = config;
    }


    /// <summary>
    /// Publishes a message to a specified topic and routing key in the RabbitMQ exchange. This method allows
    /// sending messages to RabbitMQ with a defined exchange type and routing key.
    /// </summary>
    /// <typeparam name="T">The type of the message to be published.</typeparam>
    /// <param name="topic">The topic or exchange name where the message will be published.</param>
    /// <param name="routingKey">The routing key that determines how the message will be routed.</param>
    /// <param name="exchangeType">The type of the RabbitMQ exchange (e.g., direct, fanout, topic).</param>
    /// <param name="message">The message payload to be published.</param>
    /// <returns>A Task that represents the asynchronous operation for message publishing.</returns>
    public async Task PublishAsync<T>(string topic, string routingKey, string exchangeType, T message)
    {
        try
        {
            // Create Chanel
            await using IChannel channel = await _connection.CreateChannelAsync();

            // Declare an exchange 
            await channel.ExchangeDeclareAsync(topic, exchangeType, durable: true, autoDelete: false);

            // Serialize
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            
            _logger.LogInformation($"Publish to {topic} with routingKey {routingKey} with body {body}");

            // Publish to MQ
            await channel.BasicPublishAsync(
                exchange: topic,
                routingKey: routingKey,
                body: body
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    /// <summary>
    /// Publishes a message to a specified topic and routing key as an RPC (Remote Procedure Call) in the RabbitMQ exchange.
    /// This method waits for a response within a specified timeout period.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message to be published.</typeparam>
    /// <typeparam name="TResponse">The type of the expected response message.</typeparam>
    /// <param name="exchangeName">The topic or exchange name where the request message will be published.</param>
    /// <param name="routingKey">The routing key that determines how the request message will be routed.</param>
    /// <param name="exchangeType">The type of the RabbitMQ exchange (e.g., direct, fanout, topic).</param>
    /// <param name="message">The request message payload to be sent as part of the RPC.</param>
    /// <param name="timeoutSec">The maximum time, in seconds, to wait for a response before timing out. Default value is 10.</param>
    /// <returns>A Task representing the asynchronous operation, containing the response message of type <typeparamref name="TResponse"/> if the operation succeeds, or null if no response is received.</returns>
    /// <exception cref="TimeoutException">Thrown when the RPC request does not receive a response within the specified timeout period.</exception>
    public async Task<TResponse?> PublishRpcAsync<TRequest, TResponse>(
        string exchangeName,
        string routingKey,
        string exchangeType,
        TRequest message,
        int timeoutSec = 100)
    {
        TaskCompletionSource<TResponse?> taskCompletion = new TaskCompletionSource<TResponse?>();
        try
        {
            await using IChannel channel = await _connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable: true, autoDelete: false);
        
            // Declare a temporary queue
            QueueDeclareOk replyQueue = await channel.QueueDeclareAsync(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null);
        
            // Store the temporary queue name
            string replyQueueName = replyQueue.QueueName;
            _logger.LogInformation($"Reply Queue Name {replyQueueName}");
            
            // Consumes temporary queue
            AsyncEventingBasicConsumer replyConsumer = new AsyncEventingBasicConsumer(channel);
            string correlationId = Guid.CreateVersion7().ToString();

            // Consume Events
            replyConsumer.ReceivedAsync += (model, eventArgs) =>
            {
                if (eventArgs.BasicProperties.CorrelationId != correlationId) 
                    return Task.CompletedTask;
                
                string responseBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                TResponse? response = JsonConvert.DeserializeObject<TResponse>(responseBody);
                    
                // Change awaiting Task to complete
                taskCompletion.SetResult(response);
                return Task.CompletedTask;
            };
            
            // Consumes
            await channel.BasicConsumeAsync(queue: replyQueueName, autoAck: true, consumer: replyConsumer);
            
            // Serialize message
            byte[] body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            _logger.LogInformation($"Publishing RPC to exchangeName: {exchangeName} RoutingKey: {routingKey}  with CorrelationId: {correlationId}");
            
            // Request message publishes
            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: new BasicProperties
                {
                    CorrelationId = correlationId,
                    ReplyTo = replyQueueName
                },
                body: body
            );
            
            // Wait for response
            var completedTask = await Task.WhenAny(taskCompletion.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSec)));

            // Get Response Successfully
            if(completedTask == taskCompletion.Task)
                return await taskCompletion.Task;

            throw new TimeoutException("The RPC request timed out.");
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
            throw;
        }
    }


    /// <summary>
    /// Subscribes to an RPC (Remote Procedure Call) queue, enabling bidirectional communication where the server
    /// can process a request received from the client and send a response back. This method sets up the queue,
    /// binds it to the specified routing key and exchange, and listens for incoming messages.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request messages being received.</typeparam>
    /// <typeparam name="TResponse">The type of the response messages to be sent.</typeparam>
    /// <param name="queue">The name of the queue to subscribe to.</param>
    /// <param name="routingKey">The routing key used to bind the queue to the exchange for message processing.</param>
    /// <param name="exchangeType">The type of the RabbitMQ exchange (e.g., direct, fanout, topic) used for routing.</param>
    /// <param name="messageHandler">
    /// A function to handle incoming messages. It takes two arguments:
    /// the request message of type <typeparamref name="TRequest"/>, and the routing key as a string.
    /// The function returns a task that resolves to the response of type <typeparamref name="TResponse"/>.
    /// </param>
    /// <returns>A Task representing the asynchronous operation of subscribing to the RPC queue.</returns>
    public async Task SubscribeRpcAsync<TRequest, TResponse>(
        string queue,
        string routingKey,
        string exchangeType,
        Func<TRequest, string, Task<TResponse>> messageHandler)
    {
        try
        {
            IChannel channel = await _connection.CreateChannelAsync();
            _subscribeChannels.Add(channel);
            
            // Declares queue
            string exchangeName = $"{_config.ExchangeName}.rpc.{exchangeType.ToLower()}";
            await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable: true, autoDelete: false);
            await channel.QueueDeclareAsync(queue: queue, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync( queue: queue, exchange: exchangeName, routingKey: routingKey);
            
            // Adds Consumer
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
            _consumers.Add(consumer);
            _logger.LogInformation($"RPC Consumer ready on {queue} with routingKey {routingKey}");

            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                // Extract replyTo and CorrelationId ID
                string? replyTo = eventArgs.BasicProperties.ReplyTo;
                string correlationId = eventArgs.BasicProperties.CorrelationId ?? "";
                
                // Do not process with no ReplyTo
                if (string.IsNullOrEmpty(replyTo))
                {
                    // Remove
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false); 
                    _logger.LogWarning("Received a message without a ReplyTo property. Discarding.");
                    return;
                }

                try
                {
                    // Deserialize request message
                    byte[] body = eventArgs.Body.ToArray();
                    string jsonRaw = Encoding.UTF8.GetString(body);
                    TRequest? request = JsonConvert.DeserializeObject<TRequest>(jsonRaw);
                    
                    // Do not process with no request
                    if (request == null)
                    {
                        // Remove
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false); 
                        _logger.LogWarning("Received a message without a request. Discarding.");
                        return;
                    }
                    
                    // Create Response Object
                    TResponse response = await messageHandler(request, correlationId);
                    
                    // Deserialize Response Object
                    byte[] responseBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
                    await channel.BasicPublishAsync(exchange: "", routingKey: replyTo, mandatory: true, 
                        basicProperties: new BasicProperties()
                        {
                            CorrelationId = correlationId,
                        }, body: responseBody);
                    
                    // Notify 
                    await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Error processing RPC request with CorrelationId: {correlationId}");
                    await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                }
            };
            
            await channel.BasicConsumeAsync(queue: queue, autoAck: false, consumer: consumer);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    /// <summary>
    /// Subscribes to an RPC queue in RabbitMQ to handle incoming messages and send responses asynchronously.
    /// This method establishes a connection to the specified queue and processes messages using a defined handler.
    /// </summary>
    /// <typeparam name="TRequest">The type of the incoming message that will be received from the queue.</typeparam>
    /// <typeparam name="TResponse">The type of the response message that will be sent back to the queue.</typeparam>
    /// <param name="queue">The name of the queue to subscribe to for receiving requests.</param>
    /// <param name="exchangeType">The type of the RabbitMQ exchange (e.g., direct, fanout, topic) through which the queue is bound.</param>
    /// <param name="messageHandler">
    /// A function delegate used to process incoming messages. This function takes the received message
    /// as the first parameter, an optional correlationId as the second parameter, and returns
    /// a response message asynchronously.
    /// </param>
    /// <returns>A Task that represents the asynchronous operation of subscribing and processing RPC messages.</returns>
    public async Task SubscribeRpcAsync<TRequest, TResponse>(string queue, string exchangeType,
        Func<TRequest, string, Task<TResponse>> messageHandler)
        => await SubscribeRpcAsync(queue, queue.Replace("_", "."), exchangeType, messageHandler);


    /// <summary>
    /// Subscribes to a specified topic and routing key in the message queue and processes messages using
    /// the provided message handler function. The subscription requires specifying the topic, routing key,
    /// exchange type, and the handler function for processing incoming messages.
    /// </summary>
    /// <typeparam name="T">The type of the message to be handled.</typeparam>
    /// <param name="queue">The topic or queue name to subscribe to.</param>
    /// <param name="routingKey">The routing key associated with the subscription.</param>
    /// <param name="exchangeType">The type of the exchange (e.g., direct, fanout, topic).</param>
    /// <param name="messageHandler">A function that handles the incoming message and returns a Task<bool>
    /// indicating whether the message was successfully handled or not.</param>
    /// <returns>A Task representing the asynchronous operation for message subscription.</returns>
    public async Task SubscribeAsync<T>(string queue, string routingKey, string exchangeType,
        Func<T, string,Task<bool>> messageHandler) 
    {
        try
        {
            IChannel channel = await _connection.CreateChannelAsync();
            
            // To store in memory implicitly to avoid GC
            _subscribeChannels.Add(channel);
            
            // Declare an exchange 
            string exchangeName = $"{_config.ExchangeName}.{exchangeType.ToLower()}";
            await channel.ExchangeDeclareAsync(exchangeName, exchangeType, durable: true, autoDelete: false);
            
            // Declare a Queue 
            await channel.QueueDeclareAsync(
                  queue: $"{queue}" 
                , durable: true
                , exclusive: false
                , autoDelete: false
                , arguments: null);
            
            await channel.QueueBindAsync(
                  queue: $"{queue}" 
                , exchange: exchangeName
                , routingKey: routingKey);

            // Declare consumer and add memory 
            AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(channel);
            _consumers.Add(consumer);
            
            _logger.LogInformation($"Subscribe to {exchangeName} with routingKey {routingKey} with queueName {queue}");
            
            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                try
                {
                    // Get dummy queue
                    string? replyTo = eventArgs.BasicProperties.ReplyTo;
                    
                    // Get Publisher's correlationId
                    string correlationId = eventArgs.BasicProperties.CorrelationId ?? "";



                    // Gets a body 
                    // byte[] body = eventArgs.Body.ToArray();
                    // string jsonRaw = Encoding.UTF8.GetString(body);



                    // T? message = JsonConvert.DeserializeObject<T>(jsonRaw) ;    
                    //
                    // string actualRoutingKey = eventArgs.RoutingKey;
                    //
                    // // Get a result 
                    // bool isSuccess = message != null && await messageHandler(message,actualRoutingKey);
                    //
                    // if(isSuccess)
                    //     await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    // else
                    //     await channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            };
            
            // Consume
            await channel.BasicConsumeAsync(queue: queue , autoAck: false , consumer: consumer);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);
        }
    }

    // public async Task<TResponse> PublishAndWaitForResponseAsync<TResponse>(string topic, string routingKey, string exchangeType,
    //     object? request = null, TimeSpan timeout = default)
    // {
    //     return new TResponse(EnumResponseResult.Error,"","");
    //
    //     // if (timeout == default)
    //     //     timeout = TimeSpan.FromSeconds(30);
    //     //
    //     // try
    //     // {
    //     //     await using IChannel channel = await _connection.CreateChannelAsync();
    //     //     
    //     //     // Response Queue 
    //     //     QueueDeclareOk replyQueueResult = await channel.QueueDeclareAsync(
    //     //         queue: "", 
    //     //         durable: false, 
    //     //         exclusive: true, 
    //     //         autoDelete: true);
    //     //     
    //     //     string replyQueueName = replyQueueResult.QueueName;
    //     //     string correlationId = Guid.CreateVersion7().ToString();
    //     //     
    //     //     // Thread for wating response 
    //     //     TaskCompletionSource<TResponse> waitingTaks = new TaskCompletionSource<TResponse>();
    //     //     
    //     //     // Consumer for Response
    //     //     AsyncEventingBasicConsumer replyConsumer = new AsyncEventingBasicConsumer(channel);
    //     //     replyConsumer.ReceivedAsync += async (Models, eventArgs) =>
    //     //     {
    //     //         try
    //     //         {
    //     //             // 
    //     //             if (eventArgs.BasicProperties.CorrelationId == correlationId)
    //     //             {
    //     //                 
    //     //             }
    //     //         }
    //     //         catch (Exception e)
    //     //         {
    //     //             _logger.LogError(e, e.Message);;
    //     //         }
    //     //     };
    //     // }
    //     // catch (Exception e)
    //     // {
    //     //     _logger.LogError(e, e.Message);;
    //     // }
    //     //
    //     // return new TResponse();
    // }

    /// <summary>
    /// Disposes of the resources used by the RabbitMqMessageBus instance, including the underlying RabbitMQ connection.
    /// This method should be called to release unmanaged resources and ensure proper cleanup.
    /// </summary>
    public void Dispose()
    {
        // Dispose all subscriptions
        foreach (var channel in _subscribeChannels)
        {
            try
            {
                channel?.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error disposing subscribe channel: {Message}", e.Message);
            }
        }
        
        _subscribeChannels.Clear();
        _consumers.Clear();
        _connection.Dispose();
    }
}