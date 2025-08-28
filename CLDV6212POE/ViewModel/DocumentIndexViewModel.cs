using System.Collections.Generic;

namespace CLDV6212POE.ViewModel
{
    public class DocumentIndexViewModel
    {
        public FileUploadViewModel UploadModel { get; set; } = new FileUploadViewModel();
        public List<string> Files { get; set; } = new List<string>();
    }
}