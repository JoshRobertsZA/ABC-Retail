using Azure.Storage.Queues;
using System.Text.Json;

public class QueueStorageService<T>
{
    private readonly QueueClient _queueClient;

    public QueueStorageService(string connectionString, string queueName)
    {
        _queueClient = new QueueClient(connectionString, queueName);
        _queueClient.CreateIfNotExists();
    }


    public async Task SendMessageAsync(string message)
    {
        await _queueClient.SendMessageAsync(message);
    }


    //Peek method
}
