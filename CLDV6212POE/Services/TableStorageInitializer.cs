using Azure.Data.Tables;

namespace CLDV6212POE.Services;

// Hosted service to ensure required Azure Tables exist at startup
public class TableStorageInitializer : IHostedService
{
    private readonly IConfiguration _config;

    public TableStorageInitializer(IConfiguration config)
    {
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var conn = _config.GetConnectionString("AzureStorage");
        if (string.IsNullOrWhiteSpace(conn))
            return;

        var tables = new[]
        {
            "Customer",
            "Product",
            "Order",
            "Cart"
        };

        foreach (var name in tables)
        {
            var client = new TableClient(conn, name);
            try
            {
                await client.CreateIfNotExistsAsync(cancellationToken);
            }
            catch
            {
                // ignored
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
