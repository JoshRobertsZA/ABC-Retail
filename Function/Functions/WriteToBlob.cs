using System.Net;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Function.Functions
{
    public static class WriteToBlob
    {
        private static readonly string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static readonly string containerName = "productimages";

        [FunctionName("WriteToBlob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                // Create blob service and container
                var blobServiceClient = new BlobServiceClient(storageConnection);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                // Generate unique blob name
                string blobName = $"{Guid.NewGuid()}.jpg";
                var blobClient = containerClient.GetBlobClient(blobName);

                // Upload file directly from request body
                await blobClient.UploadAsync(req.Body, overwrite: true);

                log.LogInformation($"File uploaded successfully: {blobClient.Uri}");

                return new OkObjectResult(new
                {
                    message = "File uploaded successfully",
                    url = blobClient.Uri.ToString()
                });
            }
            catch (Exception ex)
            {
                log.LogError($"Error uploading file: {ex.Message}");
                return new ObjectResult($"Error: {ex.Message}")
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError
                };
            }
        }
    }
}
