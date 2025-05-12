using Microsoft.AspNetCore.Mvc;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Controllers;

public class AnalysisController : Controller
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly ILogger<AnalysisController> _logger;
    private readonly IAnalysisService _analysisService;
    private readonly IGraphBuilderService _graphBuilderService;

    public AnalysisController(
        IDatasetRepository datasetRepository,
        IAnalysisService analysisService,
        IGraphBuilderService graphBuilderService,
        ILogger<AnalysisController> logger)
    {
        _datasetRepository = datasetRepository;
        _analysisService = analysisService;
        _graphBuilderService = graphBuilderService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all datasets.");
        var datasets = await _datasetRepository.GetAllAsync(cancellationToken);
        _logger.LogInformation("Fetched {Count} datasets.", datasets.Count());
        return View(datasets);
    }

    public async Task<IActionResult> BasicAnalysis(int id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting basic analysis for dataset ID {DatasetId}.", id);

            var dataset = await _datasetRepository.GetByIdAsync(id, cancellationToken);
            if (dataset is null)
            {
                _logger.LogWarning("Basic analysis failed: Dataset ID {DatasetId} not found.", id);
                return NotFound();
            }

            var analysis = await _analysisService.GetAnalysisAsync(id, cancellationToken);
            _logger.LogInformation("Analysis computed for dataset ID {DatasetId}.", id);

            var viewModel = new AnalysisViewModel(dataset, analysis);
            _logger.LogInformation("Basic analysis completed successfully for dataset ID {DatasetId}.", id);
            return View("BasicAnalysis", viewModel);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Basic analysis for dataset ID {DatasetId} was cancelled.", id);
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing basic analysis for dataset ID {DatasetId}.", id);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    public async Task<IActionResult> DeepAnalysis(int id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting deep analysis for dataset ID {DatasetId}.", id);

            var dataset = await _datasetRepository.GetByIdAsync(id, cancellationToken);
            if (dataset is null)
            {
                _logger.LogWarning("Deep analysis failed: Dataset ID {DatasetId} not found.", id);
                return NotFound();
            }

            var analysis = await _analysisService.GetAnalysisAsync(id, cancellationToken);
            _logger.LogInformation("Analysis computed for dataset ID {DatasetId}.", id);

            string graphData = await _graphBuilderService.BuildGraphDataAsync(dataset.Id, cancellationToken);
            ViewBag.GraphData = graphData;
            ViewBag.DatasetName = dataset.Name;
            _logger.LogInformation("Graph built successfully for dataset ID {DatasetId}.", id);

            var viewModel = new AnalysisViewModel(dataset, analysis);
            _logger.LogInformation("Deep analysis completed successfully for dataset ID {DatasetId}.", id);
            return View("DeepAnalysis", viewModel);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Deep analysis for dataset ID {DatasetId} was cancelled.", id);
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating the graph for dataset ID {DatasetId}.", id);
            return StatusCode(500, "An error occurred while generating the graph.");
        }
    }

    public async Task<IActionResult> Graph(int id, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting graph generation for dataset ID {DatasetId}.", id);

            var dataset = await _datasetRepository.GetByIdAsync(id, cancellationToken);
            if (dataset is null)
            {
                _logger.LogWarning("Graph generation failed: Dataset ID {DatasetId} not found.", id);
                return NotFound();
            }

            string graphData = await _graphBuilderService.BuildGraphDataAsync(dataset.Id, cancellationToken);
            ViewBag.GraphData = graphData;
            ViewBag.DatasetName = dataset.Name;
            _logger.LogInformation("Graph generation completed successfully for dataset ID {DatasetId}.", id);
            return View();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Graph generation for dataset ID {DatasetId} was cancelled.", id);
            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating the graph for dataset ID {DatasetId}.", id);
            return StatusCode(500, "An error occurred while generating the graph.");
        }
    }
}