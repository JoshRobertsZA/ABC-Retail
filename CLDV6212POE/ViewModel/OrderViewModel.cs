namespace CLDV6212POE.ViewModel
{
    public class OrderViewModel
    {
        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string ProductName { get; set; }

        public string ProductID { get; set; }

        public int Quantity { get; set; }

        public string? ImageUrl { get; set; }

        public string Price { get; set; }

        public DateTime? OrderDate { get; set; }

        public string QueueStatus { get; set; }

        public string RowKey { get; set; }
    }
}