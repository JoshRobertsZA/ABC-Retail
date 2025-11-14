using System.ComponentModel.DataAnnotations;

namespace CLDV6212POE.ViewModel
{
    public class FileUploadViewModel
    {
        [Required(ErrorMessage = "Please select a file.")]
        [DataType(DataType.Upload)]
        public IFormFile File { get; set; }

        public string Message { get; set; }
    }
}