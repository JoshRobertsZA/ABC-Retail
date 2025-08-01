using Azure.Storage.Blobs;

public class BlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(BlobServiceClient blobServiceClient)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient("productimages");
        _containerClient.CreateIfNotExists();
    }


    public async Task<string> UploadFileAsync(IFormFile file)
    {
        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var blobClient = _containerClient.GetBlobClient(fileName);

        using (var stream = file.OpenReadStream())
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        }

        return blobClient.Uri.ToString();
    }
}