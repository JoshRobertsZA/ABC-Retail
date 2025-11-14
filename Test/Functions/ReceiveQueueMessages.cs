using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Function.Functions
{
    public static class ReceiveQueueMessages
    {
        private static readonly string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private const string queueName = "order-queue";

        [Function("ReceiveQueueMessages")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("ReceiveQueueMessages");

            try
            {
                var queueClient = new QueueClient(storageConnection, queueName);
                await queueClient.CreateIfNotExistsAsync();

                var messages = await queueClient.PeekMessagesAsync(10);

                var response = req.CreateResponse();

                if (messages.Value.Length == 0)
                {
                    logger.LogInformation("No messages found in queue.");
                    await response.WriteAsJsonAsync(new { Status = "No messages in queue." });
                    return response;
                }

                var result = messages.Value.Select(msg => new
                {
                    msg.MessageId,
                    MessageText = msg.Body.ToString()
                });

                logger.LogInformation($"Peeked {messages.Value.Length} messages from {queueName}.");
                await response.WriteAsJsonAsync(result);
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error receiving messages: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { Error = ex.Message });
                return errorResponse;
            }
        }
    }
}
