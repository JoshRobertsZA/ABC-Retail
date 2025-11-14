using CLDV6212POE.Services;
using CLDV6212POE.ViewModel;
using Microsoft.AspNetCore.Mvc;

public class DocumentController : Controller
{
    private readonly FunctionConnector _functionConnector;
    private readonly FileStorageService _fileStorageService;

    public DocumentController(
        FunctionConnector functionConnector,
        FileStorageService fileStorageService)
    {
        _functionConnector = functionConnector;
        _fileStorageService = fileStorageService;
    }

    // Displays uploaded files and the upload form (Admin only)
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Admin")
            return Unauthorized();

        var model = new DocumentIndexViewModel
        {
            UploadModel = new FileUploadViewModel(),
            Files = await _fileStorageService.ListFilesAsync()
        };
        return View(model);
    }


    // Handles file upload via Azure Function and refreshes file list (Admin only)
    [HttpPost]
    public async Task<IActionResult> Index(FileUploadViewModel uploadModel)
    {
        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Admin")
            return Unauthorized();

        var model = new DocumentIndexViewModel
        {
            UploadModel = uploadModel,
            Files = await _fileStorageService.ListFilesAsync()
        };

        try
        {
            var uploadedFileName = await _functionConnector.UploadFileAsync(uploadModel.File);
            model.UploadModel.Message = $"File '{uploadedFileName}' uploaded successfully.";
            model.Files = await _fileStorageService.ListFilesAsync();
        }
        catch (ArgumentException ex)
        {
            model.UploadModel.Message = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            model.UploadModel.Message = $"Failed to upload: {ex.Message}";
        }
        catch (Exception ex)
        {
            model.UploadModel.Message = $"Unexpected error: {ex.Message}";
        }

        return View(model);
    }


    // Downloads a selected file from Azure Blob Storage (Admin only)
    public async Task<IActionResult> Download(string fileName)
    {
        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Admin")
            return Unauthorized();

        var stream = await _fileStorageService.DownloadFileAsync(fileName);
        return File(stream, "application/octet-stream", fileName);
    }


    // Deletes a file from Azure Blob Storage (Admin only)
    [HttpPost]
    public async Task<IActionResult> Delete(string fileName)
    {
        var role = HttpContext.Session.GetString("UserRole");

        if (role != "Admin")
            return Unauthorized();

        await _fileStorageService.DeleteFileAsync(fileName);
        return RedirectToAction("Index");
    }
}
