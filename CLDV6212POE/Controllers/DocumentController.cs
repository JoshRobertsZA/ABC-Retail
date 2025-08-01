using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

public class DocumentController : Controller
{
    private readonly FileStorageService _fileStorageService;

    public DocumentController(FileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task<IActionResult> Index()
    {
        var files = await _fileStorageService.ListFilesAsync();
        return View(files);
    }


    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file != null)
        {
            await _fileStorageService.UploadFileAsync(file);
        }
        return RedirectToAction("Index");
    }


    public async Task<IActionResult> Download(string fileName)
    {
        var stream = await _fileStorageService.DownloadFileAsync(fileName);
        return File(stream, "application/octet-stream", fileName);
    }
}
