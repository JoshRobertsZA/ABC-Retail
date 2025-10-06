using Azure.Storage.Files.Shares;

public class FileStorageService
{
    private readonly string _connectionString;
    private readonly string _shareName = "contracts";

    public FileStorageService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AzureStorage");
    }


    // Lists all file names inside the contracts share.
    public async Task<List<string>> ListFilesAsync()
    {
        var share = new ShareClient(_connectionString, _shareName);
        var rootDir = share.GetRootDirectoryClient();

        var files = new List<string>();

        await foreach (var item in rootDir.GetFilesAndDirectoriesAsync())
            if (!item.IsDirectory)
                files.Add(item.Name);

        return files;
    }


    // Downloads a file from the contracts share as a stream.
    public async Task<Stream> DownloadFileAsync(string fileName)
    {
        var share = new ShareClient(_connectionString, _shareName);
        var rootDir = share.GetRootDirectoryClient();
        var fileClient = rootDir.GetFileClient(fileName);

        var download = await fileClient.DownloadAsync();
        return download.Value.Content;
    }


    // Deletes a file from the contracts share.
    public async Task DeleteFileAsync(string fileName)
    {
        var share = new ShareClient(_connectionString, _shareName);
        var rootDir = share.GetRootDirectoryClient();
        var fileClient = rootDir.GetFileClient(fileName);

        await fileClient.DeleteIfExistsAsync();
    }
}