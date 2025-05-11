using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialNetworkAnalyser.Models;
using SocialNetworkAnalyser.Data;
using SocialNetworkAnalyser.Interfaces;

namespace SocialNetworkAnalyser.Controllers;

public class DatasetController : Controller
{
    private readonly IDataImportService _importService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatasetController> _logger;

    public DatasetController(IDataImportService importService, ApplicationDbContext context, ILogger<DatasetController> logger) =>
        (_importService, _context, _logger) = (importService, context, logger);

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching datasets ordered by descending import date.");
        var datasets = await _context.Datasets.OrderByDescending(d => d.ImportDate).ToListAsync(cancellationToken);
        _logger.LogInformation("Fetched {Count} datasets.", datasets.Count);
        return View(datasets);
    }

    public IActionResult Import()
    {
        _logger.LogInformation("Rendering Import view for dataset upload.");
        return View(new DatasetImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(DatasetImportViewModel model, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing dataset import for dataset name: {DatasetName}", model.DatasetName);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Model state invalid for dataset import: {DatasetName}", model.DatasetName);
            return View(model);
        }

        if (model.UploadFile == null)
        {
            _logger.LogWarning("No file was uploaded for dataset: {DatasetName}", model.DatasetName);
            ModelState.AddModelError("UploadFile", "Please select a file to upload.");
            return View(model);
        }

        if (!model.UploadFile.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Invalid file extension uploaded for dataset: {DatasetName}", model.DatasetName);
            ModelState.AddModelError(string.Empty, "Only .txt files are allowed.");
            return View(model);
        }

        if (await _context.Datasets.AsNoTracking().AnyAsync(d => d.Name == model.DatasetName, cancellationToken))
        {
            _logger.LogWarning("Duplicate dataset import attempt detected for dataset: {DatasetName}", model.DatasetName);
            ModelState.AddModelError(string.Empty, "Dataset with the same name has already been imported.");
            return View(model);
        }

        var tempFilePath = Path.GetTempFileName();
        _logger.LogDebug("Temporary file created at {TempFilePath}", tempFilePath);

        try
        {
            _logger.LogInformation("Saving uploaded file to temporary file.");
            using (var stream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await model.UploadFile.CopyToAsync(stream, cancellationToken);
            }

            _logger.LogInformation("File saved successfully. Pausing briefly for file system stabilization.");
            await Task.Delay(500);

            _logger.LogInformation("Invoking ImportDatasetAsync for dataset: {DatasetName}", model.DatasetName);
            var imported = await _importService.ImportDatasetAsync(model.DatasetName, tempFilePath, cancellationToken);

            if (!imported)
            {
                _logger.LogError("Import dataset failed for dataset: {DatasetName}", model.DatasetName);
                ModelState.AddModelError(string.Empty, "An error occurred during import.");
                return View(model);
            }

            _logger.LogInformation("Dataset imported successfully: {DatasetName}", model.DatasetName);
            return RedirectToAction(nameof(Index));
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Dataset import was cancelled for dataset: {DatasetName}", model.DatasetName);
            return StatusCode(499);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "File access error during dataset import: {DatasetName}", model.DatasetName);
            ModelState.AddModelError(string.Empty, $"File access error: {ex.Message}");
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during dataset import: {DatasetName}", model.DatasetName);
            ModelState.AddModelError(string.Empty, $"Unexpected error: {ex.Message}");
            return View(model);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                _logger.LogDebug("Deleting temporary file at {TempFilePath}.", tempFilePath);
                System.IO.File.Delete(tempFilePath);
            }
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Attempting to delete dataset with ID {DatasetId}.", id);
        var dataset = await _context.Datasets.FindAsync(new object[] { id }, cancellationToken);
        if (dataset == null)
        {
            _logger.LogWarning("Dataset delete failed: Dataset with ID {DatasetId} not found.", id);
            return NotFound();
        }

        _context.Datasets.Remove(dataset);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Dataset with ID {DatasetId} deleted successfully.", id);
        return RedirectToAction(nameof(Index));
    }
}