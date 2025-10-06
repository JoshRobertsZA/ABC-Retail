using Azure.Data.Tables;
using Function.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Threading.Tasks;

namespace Function.Functions
{
    public static class SendCustomerMessage
    {
        private static readonly string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        [FunctionName("SendCustomerMessage")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "customer/send")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string body = await new StreamReader(req.Body).ReadToEndAsync();
                var customer = JsonConvert.DeserializeObject<Customer>(body);

                if (customer == null)
                {
                    log.LogError("Failed to deserialize customer message");
                    return new BadRequestObjectResult("Invalid customer data");
                }

                customer.RowKey ??= Guid.NewGuid().ToString();
                customer.PartitionKey ??= "Customer";

                var tableClient = new TableClient(storageConnection, "Customer");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(customer);

                log.LogInformation($"Customer {customer.FullName} saved to table.");
                return new OkObjectResult(new { Message = $"Customer {customer.FullName} saved." });
            }
            catch (Exception ex)
            {
                log.LogError($"Error saving customer: {ex.Message}");
                return new ObjectResult($"Error saving customer: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}