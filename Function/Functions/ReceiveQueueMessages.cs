using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Function.Functions
{
    public static class ReceiveQueueMessages
    {
        private static readonly string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private const string queueName = "order-queue";

        // Peek up to 10 messages (does NOT remove them from queue)
        [FunctionName("ReceiveQueueMessages")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var queueClient = new QueueClient(storageConnection, queueName);
                await queueClient.CreateIfNotExistsAsync();

                var messages = await queueClient.PeekMessagesAsync(10);

                if (messages.Value.Length == 0)
                {
                    log.LogInformation("No messages found in queue.");
                    return new OkObjectResult(new { Status = "No messages in queue." });
                }

                var result = messages.Value.Select(msg => new
                {
                    msg.MessageId,
                    MessageText = msg.Body.ToString()
                });

                log.LogInformation($"Peeked {messages.Value.Length} messages from {queueName}.");

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError($"Error receiving messages: {ex.Message}");
                return new ObjectResult(new { Error = ex.Message }) { StatusCode = 500 };
            }
        }
    }
}
