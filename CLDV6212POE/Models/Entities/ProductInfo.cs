using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE.Models.Entities
{
    public class ProductInfo : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";

        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public string ProductName { get; set; }

        public string Description { get; set; }

        public string Price { get; set; }

        public string Category { get; set; }

        public int StockQuantity { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string? ImageUrl { get; set; }
    }
}