using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class FileStorageService
{
    private readonly string _connectionString;
    private readonly string _shareName = "contracts";

    public FileStorageService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("AzureStorage");
    }


    public async Task UploadFileAsync(IFormFile file)
    {
        var share = new ShareClient(_connectionString, _shareName);
        await share.CreateIfNotExistsAsync();

        var rootDir = share.GetRootDirectoryClient();
        var fileClient = rootDir.GetFileClient(file.FileName);

        using var stream = file.OpenReadStream();
        await fileClient.CreateAsync(stream.Length);
        await fileClient.UploadAsync(stream);
    }


    public async Task<List<string>> ListFilesAsync()
    {
        var share = new ShareClient(_connectionString, _shareName);
        var rootDir = share.GetRootDirectoryClient();

        var files = new List<string>();

        await foreach (ShareFileItem item in rootDir.GetFilesAndDirectoriesAsync())
        {
            if (!item.IsDirectory)
            {
                files.Add(item.Name);
            }
        }

        return files;
    }


    public async Task<Stream> DownloadFileAsync(string fileName)
    {
        var share = new ShareClient(_connectionString, _shareName);
        var rootDir = share.GetRootDirectoryClient();
        var fileClient = rootDir.GetFileClient(fileName);

        var download = await fileClient.DownloadAsync();
        return download.Value.Content;
    }
}
