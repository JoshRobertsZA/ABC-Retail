using System.Net;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Function.Functions;

public static class WriteToFile
{
    private static readonly string StorageConnection = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
    private const string ShareName = "contracts";

    [Function("WriteToFile")]
    public static async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData req,
        FunctionContext context)
    {
        var logger = context.GetLogger("WriteToFile");

        try
        {
            // Check for Content-Type header
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Missing Content-Type header.");
                return badResponse;
            }

            var contentType = string.Join(";", contentTypeValues);
            if (!contentType.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid Content-Type. Expected multipart/form-data.");
                return badResponse;
            }

            // Get boundary
            var mediaTypeHeader = MediaTypeHeaderValue.Parse(contentType);
            var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary).Value;
            var reader = new MultipartReader(boundary, req.Body);

            // Connect to Azure File Share
            var shareClient = new ShareClient(StorageConnection, ShareName);
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

            logger.LogInformation($"Uploaded {uploadedFiles.Count} file(s) successfully.");
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { message = $"Uploaded {uploadedFiles.Count} file(s).", files = uploadedFiles });
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error uploading files: {ex.Message}");
            var response = req.CreateResponse(HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error uploading files: {ex.Message}");
            return response;
        }
    }
}
