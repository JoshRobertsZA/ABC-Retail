using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Function.Functions;
public static class WriteToBlob
{
    private static readonly string StorageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
    private const string ContainerName = "productimages";

    [Function("WriteToBlob")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        var logger = context.GetLogger("WriteToBlob");

        try
        {
            // Create blob service and container
            var blobServiceClient = new BlobServiceClient(StorageConnection);
            var containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            // Generate unique blob name
            string blobName = $"{Guid.NewGuid()}.jpg";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Upload file from request body
            await blobClient.UploadAsync(req.Body, overwrite: true);

            logger.LogInformation($"File uploaded successfully: {blobClient.Uri}");

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "File uploaded successfully",
                url = blobClient.Uri.ToString()
            });

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error uploading file: {ex.Message}");

            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error: {ex.Message}");
            return response;
        }
    }
}
