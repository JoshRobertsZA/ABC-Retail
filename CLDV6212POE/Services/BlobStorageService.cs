using Azure.Storage.Blobs;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient("productimages");
        _containerClient.CreateIfNotExists();
    }


    /// <summary>
    /// Uploads an image to blob storage and returns its public URL.
    /// </summary>
    public async Task<string> UploadFileAsync(IFormFile file)
    {
        // Generate a unique name each time
        var extension = Path.GetExtension(file.FileName);
        var blobName = $"{Guid.NewGuid()}{extension}";

        var blobClient = _containerClient.GetBlobClient(blobName);

        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, overwrite: false);
        }

        return blobClient.Uri.ToString();
    }


    /// <summary>
    /// Deletes a file from blob storage by its URL.
    /// </summary>
    public async Task DeleteFileAsync(string fileUrl)
    {
        var blobName = Path.GetFileName(new Uri(fileUrl).AbsolutePath);
        await _containerClient.DeleteBlobIfExistsAsync(blobName);
    }
}