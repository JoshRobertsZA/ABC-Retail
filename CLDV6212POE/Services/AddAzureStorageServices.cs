using Azure.Storage.Blobs;
using CLDV6212POE.Models.Entities;

namespace CLDV6212POE.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorageServices(this IServiceCollection services, string connectionString)
    {
        // Table Storage
        services.AddScoped<TableStorageService<CustomerProfile>>(_ =>
            new TableStorageService<CustomerProfile>(connectionString, "Customer"));

        services.AddScoped<TableStorageService<ProductInfo>>(_ =>
            new TableStorageService<ProductInfo>(connectionString, "Product"));

        // Queue Storage
        services.AddScoped<QueueStorageService<ProductInfo>>(_ =>
            new QueueStorageService<ProductInfo>(connectionString, "product-queue"));

        // Blob Storage
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddSingleton<BlobStorageService>();

        // File Sharing
        services.AddSingleton<FileStorageService>();

        return services;
    }
}