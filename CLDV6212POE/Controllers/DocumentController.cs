using CLDV6212POE.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;

public class DocumentController : Controller
{
    private readonly FileStorageService _fileStorageService;

    public DocumentController(FileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }


    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var files = await _fileStorageService.ListFilesAsync();

        var model = new DocumentIndexViewModel()
        {
            UploadModel = new FileUploadViewModel(),
            Files = files
        };

        return View(model);
    }


    [HttpPost]
    public async Task<IActionResult> Index(FileUploadViewModel uploadModel)
    {
        var files = await _fileStorageService.ListFilesAsync();
        var model = new DocumentIndexViewModel
        {
            UploadModel = uploadModel,
            Files = files
        };

        try
        {
            await _fileStorageService.UploadFileAsync(uploadModel.File);
            model.UploadModel.Message = "File uploaded successfully.";
        }
        catch (ArgumentException ex)
        {
            // This catches both "no file uploaded" and invalid file type
            model.UploadModel.Message = ex.Message;
        }
        catch
        {
            model.UploadModel.Message = "An error occurred while uploading the file.";
        }

        return View(model);
    }


    public async Task<IActionResult> Download(string fileName)
    {
        var stream = await _fileStorageService.DownloadFileAsync(fileName);
        return File(stream, "application/octet-stream", fileName);
    }


    [HttpPost]
    public async Task<IActionResult> Delete(string fileName)
    {
        await _fileStorageService.DeleteFileAsync(fileName);
        return RedirectToAction("Index");
    }
}