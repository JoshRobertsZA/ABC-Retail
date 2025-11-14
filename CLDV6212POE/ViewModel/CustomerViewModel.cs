
namespace CLDV6212POE.ViewModel;
public class CustomerViewModel
{
    public Guid UserId { get; set; }
    public string Fullname { get; set; }
    public string Email { get; set; }

    // Azure Table entity keys
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }

    public DateTimeOffset CreatedDate { get; set; }
}
