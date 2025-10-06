using System.Text.Json;
using Azure.Data.Tables;
using Function.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Function.Functions;

public static class StoreProduct
{
    private static readonly string StorageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
    private static TableClient _tableClient;

    // Function to store Product data in Azure Table
    [FunctionName("StoreProduct")]
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

            string partitionKey = root.TryGetProperty("PartitionKey", out var pkEl) ? pkEl.GetString()! : "Product";
            string rowKey = root.TryGetProperty("RowKey", out var rkEl) ? rkEl.GetString()! : Guid.NewGuid().ToString();

            var entity = new TableEntity(partitionKey, rowKey);

            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Name == "PartitionKey" || prop.Name == "RowKey") continue;

                entity[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => prop.Value.ToString()
                };
            }

            var tableClient = new TableClient(StorageConnection, "Product");
            await tableClient.CreateIfNotExistsAsync();
            await tableClient.AddEntityAsync(entity);

            log.LogInformation("Product entity stored successfully.");
            return new OkObjectResult("Product entity stored successfully.");
        }
        catch (Exception ex)
        {
            log.LogError($"Error storing Product: {ex.Message}");
            return new ObjectResult($"Error storing Product: {ex.Message}") { StatusCode = 500 };
        }
    }
}

