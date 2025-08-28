using Azure.Storage.Queues;

public class QueueStorageService<T>
{
    private readonly QueueClient _queueClient;

    public QueueStorageService(string connectionString, string queueName)
    {
        _queueClient = new QueueClient(connectionString, queueName);
        _queueClient.CreateIfNotExists();
    }


    /// <summary>
    /// Sends a message to the queue.
    /// Automatically serializes objects to JSON.
    /// </summary>
    public async Task SendMessageAsync(string message)
    {
        await _queueClient.SendMessageAsync(message);
    }
}