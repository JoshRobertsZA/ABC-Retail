using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Function.Functions;

public static class StoreProduct
{
    private static readonly string StorageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;

    [Function("StoreProduct")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        var logger = context.GetLogger("StoreProduct");

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

            logger.LogInformation("Product entity stored successfully.");

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync("Product entity stored successfully.");
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error storing Product: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error storing Product: {ex.Message}");
            return errorResponse;
        }
    }
}
