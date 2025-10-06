using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Function.Functions
{
    public static class WriteToFile
    {
        private static readonly string storageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        private static readonly string shareName = "contracts";

        [FunctionName("WriteToFile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                // Check for valid content type
                if (!req.Headers.TryGetValue("Content-Type", out var contentTypeValues))
                    return new BadRequestObjectResult("Missing Content-Type header.");

                var contentType = contentTypeValues.ToString();
                if (!contentType.StartsWith("multipart/form-data"))
                    return new BadRequestObjectResult("Invalid Content-Type. Expected multipart/form-data.");

                // Get boundary
                var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);
                var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
                var reader = new MultipartReader(boundary, req.Body);

                // Connect to Azure File Share
                var shareClient = new ShareClient(storageConnection, shareName);
                await shareClient.CreateIfNotExistsAsync();
                var directory = shareClient.GetRootDirectoryClient();

                var uploadedFiles = new List<string>();

                // Read each uploaded section
                for (var section = await reader.ReadNextSectionAsync(); section != null; section = await reader.ReadNextSectionAsync())
                {
                    if (!ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition) ||
                        string.IsNullOrEmpty(contentDisposition.FileName.Value))
                        continue;

                    var fileName = contentDisposition.FileName.Value.Trim('"');
                    using var ms = new MemoryStream();
                    await section.Body.CopyToAsync(ms);

                    if (ms.Length == 0) continue;

                    ms.Position = 0;
                    var fileClient = directory.GetFileClient(fileName);
                    await fileClient.CreateAsync(ms.Length);
                    await fileClient.UploadRangeAsync(new Azure.HttpRange(0, ms.Length), ms);

                    uploadedFiles.Add(fileName);
                }

                log.LogInformation($"Uploaded {uploadedFiles.Count} file(s) successfully.");
                return new OkObjectResult(new { message = $"Uploaded {uploadedFiles.Count} file(s).", files = uploadedFiles });
            }
            catch (Exception ex)
            {
                log.LogError($"Error uploading files: {ex.Message}");
                return new ObjectResult($"Error uploading files: {ex.Message}") { StatusCode = 500 };
            }
        }
    }
}
