using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE.Models
{
    public class Customer : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";

        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
    }
}