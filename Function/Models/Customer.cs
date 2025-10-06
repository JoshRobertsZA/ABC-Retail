using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace Function.Models
{
    public class Customer : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customer";

        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        public DateTimeOffset DateOfBirth { get; set; }

        public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;

    }
}