using Azure.Data.Tables;
using CLDV6212POE.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Function.Functions
{
    public static class SendCustomerMessage
    {
        private static readonly string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        [Function("SendCustomerMessage")]
        public static async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer/send")]
            HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("SendCustomerMessage");

            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                var customer = JsonConvert.DeserializeObject<Customer>(body);

                if (customer == null)
                {
                    logger.LogError("Failed to deserialize customer message");
                    var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                    await badResponse.WriteAsJsonAsync(new { Error = "Invalid customer data" });
                    return badResponse;
                }

                customer.RowKey ??= Guid.NewGuid().ToString();
                customer.PartitionKey ??= "Customer";

                var tableClient = new TableClient(storageConnection, "Customer");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(customer);

                logger.LogInformation($"Customer {customer.FullName} saved to table.");

                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Message = $"Customer {customer.FullName} saved." });
                return response;
            }
            catch (Exception ex)
            {
                logger.LogError($"Error saving customer: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteAsJsonAsync(new { Error = $"Error saving customer: {ex.Message}" });
                return errorResponse;
            }
        }
    }
}