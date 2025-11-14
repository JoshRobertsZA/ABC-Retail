using System.Text.Json.Serialization;
using Azure;
using Azure.Data.Tables;

namespace CLDV6212POE.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; }

        public string RowKey { get; set; }

        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public int Quantity { get; set; }

        public DateTimeOffset OrderDate { get; set; }

        public string QueueStatus { get; set; } = "Queued";

        [JsonIgnore]
        public string? QueueMessageId { get; set; }
        [JsonIgnore]
        public string? PopReceipt { get; set; }

        public DateTimeOffset? Timestamp { get; set; }

        [JsonIgnore]
        public ETag ETag { get; set; }

    }
}