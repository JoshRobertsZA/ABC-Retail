namespace CLDV6212POE.ViewModel
{
    public class QueueMessageViewModel
    {
        public string? TableName { get; set; }
        public string? EntityId { get; set; }
        public string? Action { get; set; }
        public string? MessageText { get; set; }
        public string? MessageId { get; set; }
        public string? EntityJson { get; set; }

        public string? PopReceipt { get; set; }
    }
}
