using System.ComponentModel.DataAnnotations;

namespace CLDV6212POE.ViewModel
{
    public class AlertViewModel
    {
        public string Title { get; set; } = "Success";
        public string Message { get; set; } = "Your changes have been saved.";
        public string Type { get; set; } = "success";
    }
}