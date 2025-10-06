using Azure.Data.Tables;
using Azure.Storage.Queues.Models;
using Function.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Function
{
    public class ProcessOrderQueue
    {
        private readonly ILogger<ProcessOrderQueue> _logger;
        private readonly string _storageConnectionString;
        private readonly TableClient _tableClient;

        public ProcessOrderQueue(ILogger<ProcessOrderQueue> logger)
        {
            _logger = logger;
            _storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var serviceClient = new TableServiceClient(_storageConnectionString);
            _tableClient = serviceClient.GetTableClient("Order");
        }

        [FunctionName("ProcessOrderQueue")]
        public async Task Run([QueueTrigger("order-queue", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message.MessageText}");

            await _tableClient.CreateIfNotExistsAsync();

            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var order = JsonConvert.DeserializeObject<Order>(message.MessageText, settings);

            if (order == null)
            {
                _logger.LogError("Failed to deserialize order from queue message");
                return;
            }

            order.RowKey = Guid.NewGuid().ToString();
            order.PartitionKey = "Order";

            _logger.LogInformation($"Saving entity with RowKey: {order.RowKey}");

            await _tableClient.AddEntityAsync(order);
            _logger.LogInformation($"Successfully saved order entity with RowKey: {order.RowKey}");
        }

    }
}