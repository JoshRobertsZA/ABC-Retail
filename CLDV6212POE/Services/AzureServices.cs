using Function.Models;

namespace CLDV6212POE.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorageServices(this IServiceCollection services, string connectionString)
    {
        // Table Storage
        services.AddScoped<TableStorageService<Customer>>(_ =>
            new TableStorageService<Customer>(connectionString, "Customer"));

        services.AddScoped<TableStorageService<Product>>(_ =>
            new TableStorageService<Product>(connectionString, "Product"));

        services.AddScoped<TableStorageService<Order>>(_ =>
            new TableStorageService<Order>(connectionString, "Order"));

        // File Sharing
        services.AddSingleton<FileStorageService>();

        // Function Connector for Azure Functions integration
        services.AddHttpClient<FunctionConnector>();

        return services;
    }
}