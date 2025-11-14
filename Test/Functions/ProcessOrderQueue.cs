using Azure.Data.Tables;
using CLDV6212POE.Models;
using CLDV6212POE.ViewModel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Function.Functions
{
    public class ProcessOrderQueue
    {
        private readonly ILogger<ProcessOrderQueue> _logger;

        public ProcessOrderQueue(ILogger<ProcessOrderQueue> logger)
        {
            _logger = logger;
        }

        [Function("ProcessOrderQueue")]
        public async Task Run(
            [QueueTrigger("order-queue", Connection = "AzureWebJobsStorage")] string message)
        {
            _logger.LogInformation($"Queue trigger processed: {message}");

            // Try to deserialize as QueueMessageViewModel first
            var queueMessage = JsonConvert.DeserializeObject<QueueMessageViewModel>(message);

            if (queueMessage != null && queueMessage.Action == "Delete")
            {
                var storageConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                var tableClient = new TableClient(storageConnectionString, queueMessage.TableName);

                var partitionKey = "Order";
                await tableClient.DeleteEntityAsync(partitionKey, queueMessage.EntityId);

                _logger.LogInformation($"Deleted entity {queueMessage.EntityId} from {queueMessage.TableName}");
                return;
            }

            // Else treat as normal Order insert
            var order = JsonConvert.DeserializeObject<Order>(message);
            if (order == null)
            {
                _logger.LogError("Failed to deserialize order");
                return;
            }

            var serviceClient2 = new TableServiceClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var tableClient2 = serviceClient2.GetTableClient("Order");
            await tableClient2.CreateIfNotExistsAsync();

            order.RowKey = Guid.NewGuid().ToString();
            order.PartitionKey = "Order";

            // Ensure OrderDate is valid
            if (order.OrderDate == default)
                order.OrderDate = DateTimeOffset.UtcNow;

            await tableClient2.AddEntityAsync(order);
            _logger.LogInformation($"Saved order with RowKey: {order.RowKey}");
        }
    }
}
