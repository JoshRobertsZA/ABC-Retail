using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

public class FileStorageService
{
    private readonly string _connectionString;
    private readonly string _shareName = "contracts";

    public FileStorageService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AzureStorage");
    }


    /// <summary>
    ///     Uploads a file into the contracts share.
    /// </summary>
    public async Task UploadFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file uploaded.");

        var allowedExtensions = new[] { ".pdf", ".docx", ".txt" };
        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            throw new ArgumentException("Invalid file type. Only PDF, DOCX, and TXT files are allowed.");

        var share = new ShareClient(_connectionString, _shareName);
        await share.CreateIfNotExistsAsync();

        var rootDir = share.GetRootDirectoryClient();
        var fileClient = rootDir.GetFileClient(file.FileName);

        using var stream = file.OpenReadStream();
        await fileClient.CreateAsync(stream.Length);
        await fileClient.UploadAsync(stream);
    }


    /// <summary>
    ///     Lists all file names inside the contracts share.
    /// </summary>
    /// >
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


    /// <summary>
    ///     Downloads a file from the contracts share as a stream.
    /// </summary>
    public async Task<Stream> DownloadFileAsync(string fileName)
    {
        var share = new ShareClient(_connectionString, _shareName);
        var rootDir = share.GetRootDirectoryClient();
        var fileClient = rootDir.GetFileClient(fileName);

        var download = await fileClient.DownloadAsync();
        return download.Value.Content;
    }


    /// <summary>
    ///     Deletes a file from the contracts share.
    /// </summary>
    public async Task DeleteFileAsync(string fileName)
    {
        var share = new ShareClient(_connectionString, _shareName);
        var rootDir = share.GetRootDirectoryClient();
        var fileClient = rootDir.GetFileClient(fileName);

        await fileClient.DeleteIfExistsAsync();
    }
}