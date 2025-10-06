using System.Text.Json;
using Azure.Data.Tables;
using Function.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Function.Functions;

public static class StoreCustomer
{
    private static readonly string StorageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
    private static TableClient _tableClient;

    // Function to store Customer data in Azure Table
    [FunctionName("StoreCustomer")]
    public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
        HttpRequest req,
        ILogger log)
    {
        try
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string partitionKey = root.TryGetProperty("PartitionKey", out var pkEl) ? pkEl.GetString()! : "Customer";
            string rowKey = root.TryGetProperty("RowKey", out var rkEl) ? rkEl.GetString()! : Guid.NewGuid().ToString();

            var entity = new TableEntity(partitionKey, rowKey);

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "PartitionKey" || prop.Name == "RowKey") continue;

                switch (prop.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        entity[prop.Name] = prop.Value.GetString();
                        break;
                    case JsonValueKind.Number:
                        entity[prop.Name] = prop.Value.GetDouble();
                        break;
                    case JsonValueKind.True:
                        entity[prop.Name] = true;
                        break;
                    case JsonValueKind.False:
                        entity[prop.Name] = false;
                        break;
                    default:
                        entity[prop.Name] = prop.Value.ToString();
                        break;
                }
            }

            var tableClient = new TableClient(StorageConnection, "Customer");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(entity);

            log.LogInformation("Customer entity stored successfully.");
            return new OkObjectResult("Customer entity stored successfully.");
        }
        catch (Exception ex)
        {
            log.LogError($"Error storing Customer: {ex.Message}");
            return new ObjectResult($"Error storing Customer: {ex.Message}") { StatusCode = 500 };
        }
    }
}

