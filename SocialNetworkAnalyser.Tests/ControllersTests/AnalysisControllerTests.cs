using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SocialNetworkAnalyser.Controllers;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Tests.ControllersTests;

public class AnalysisControllerTests
{
    private readonly Mock<IDatasetRepository> _datasetRepositoryMock;
    private readonly Mock<IAnalysisService> _analysisServiceMock;
    private readonly Mock<IGraphBuilderService> _graphBuilderServiceMock;
    private readonly Mock<ILogger<AnalysisController>> _loggerMock;
    private readonly AnalysisController _controller;

    public AnalysisControllerTests()
    {
        _datasetRepositoryMock = new Mock<IDatasetRepository>();
        _analysisServiceMock = new Mock<IAnalysisService>();
        _graphBuilderServiceMock = new Mock<IGraphBuilderService>();
        _loggerMock = new Mock<ILogger<AnalysisController>>();

        _controller = new AnalysisController(
            _datasetRepositoryMock.Object,
            _analysisServiceMock.Object,
            _graphBuilderServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Index_ReturnsViewWithDatasets()
    {
        // Arrange
        var datasets = new List<DatasetModel>
        {
            new DatasetModel { Id = 1, Name = "Dataset1" },
            new DatasetModel { Id = 2, Name = "Dataset2" }
        };

        _datasetRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(datasets);

        // Act
        var result = await _controller.Index(CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(datasets, viewResult.Model);
    }

    [Fact]
    public async Task BasicAnalysis_ReturnsNotFound_WhenDatasetDoesNotExist()
    {
        // Arrange
        int datasetId = 1;
        _datasetRepositoryMock.Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatasetModel)null);

        // Act
        var result = await _controller.BasicAnalysis(datasetId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task BasicAnalysis_ReturnsView_WhenDatasetFound()
    {
        // Arrange
        int datasetId = 1;
        var dataset = new DatasetModel { Id = datasetId, Name = "TestDataset" };
        var analysisResult = new AnalysisResultModel
        {
            TotalUsers = 10,
            AverageFriendsPerUser = 2.5,
            AverageMaximalCliqueSize = 4,
            AverageCountsPerDistance = new Dictionary<int, double> { { 1, 3 } }
        };

        _datasetRepositoryMock
            .Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _analysisServiceMock
            .Setup(service => service.GetAnalysisAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        // Act
        var result = await _controller.BasicAnalysis(datasetId, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("BasicAnalysis", viewResult.ViewName);
        var viewModel = Assert.IsType<AnalysisViewModel>(viewResult.Model);
        Assert.Equal(dataset, viewModel.Dataset);
        Assert.Equal(analysisResult, viewModel.AnalysisData);
    }

    [Fact]
    public async Task BasicAnalysis_ReturnsStatusCode499_WhenOperationCanceledExceptionThrown()
    {
        // Arrange
        int datasetId = 1;
        var dataset = new DatasetModel { Id = datasetId, Name = "TestDataset" };

        _datasetRepositoryMock
            .Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _analysisServiceMock
            .Setup(service => service.GetAnalysisAsync(datasetId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.BasicAnalysis(datasetId, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(499, statusResult.StatusCode);
    }

    [Fact]
    public async Task BasicAnalysis_ReturnsStatusCode500_WhenExceptionThrown()
    {
        // Arrange
        int datasetId = 1;
        var dataset = new DatasetModel { Id = datasetId, Name = "TestDataset" };

        _datasetRepositoryMock
            .Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _analysisServiceMock
            .Setup(service => service.GetAnalysisAsync(datasetId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test Exception"));

        // Act
        var result = await _controller.BasicAnalysis(datasetId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while processing your request.", objectResult.Value);
    }

    [Fact]
    public async Task DeepAnalysis_ReturnsNotFound_WhenDatasetDoesNotExist()
    {
        // Arrange
        int datasetId = 1;
        _datasetRepositoryMock.Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatasetModel)null);

        // Act
        var result = await _controller.DeepAnalysis(datasetId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeepAnalysis_ReturnsViewWithGraphData_WhenSuccessful()
    {
        // Arrange
        int datasetId = 1;
        var dataset = new DatasetModel { Id = datasetId, Name = "TestDataset" };
        var analysisResult = new AnalysisResultModel
        {
            TotalUsers = 20,
            AverageFriendsPerUser = 3.5,
            AverageMaximalCliqueSize = 5,
            AverageCountsPerDistance = new Dictionary<int, double> { { 1, 4 } }
        };
        string graphJson = "{\"nodes\":[],\"edges\":[]}";

        _datasetRepositoryMock.Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _analysisServiceMock.Setup(service => service.GetAnalysisAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);
        _graphBuilderServiceMock.Setup(service => service.BuildGraphDataAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(graphJson);

        // Act
        var result = await _controller.DeepAnalysis(datasetId, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("DeepAnalysis", viewResult.ViewName);
        var viewModel = Assert.IsType<AnalysisViewModel>(viewResult.Model);
        Assert.Equal(dataset, viewModel.Dataset);
        Assert.Equal(analysisResult, viewModel.AnalysisData);

        Assert.Equal(graphJson, _controller.ViewBag.GraphData);
        Assert.Equal(dataset.Name, _controller.ViewBag.DatasetName);
    }

    [Fact]
    public async Task DeepAnalysis_ReturnsStatusCode499_WhenOperationCanceledExceptionThrown()
    {
        // Arrange
        int datasetId = 1;
        var dataset = new DatasetModel { Id = datasetId, Name = "TestDataset" };

        _datasetRepositoryMock.Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _graphBuilderServiceMock.Setup(service => service.BuildGraphDataAsync(datasetId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _controller.DeepAnalysis(datasetId, CancellationToken.None);

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(499, statusResult.StatusCode);
    }

    [Fact]
    public async Task DeepAnalysis_ReturnsStatusCode500_WhenExceptionThrown()
    {
        // Arrange
        int datasetId = 1;
        var dataset = new DatasetModel { Id = datasetId, Name = "TestDataset" };

        _datasetRepositoryMock.Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _graphBuilderServiceMock.Setup(service => service.BuildGraphDataAsync(datasetId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Graph error"));

        // Act
        var result = await _controller.DeepAnalysis(datasetId, CancellationToken.None);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while generating the graph.", objectResult.Value);
    }

    [Fact]
    public async Task Graph_ReturnsNotFound_WhenDatasetDoesNotExist()
    {
        // Arrange
        int datasetId = 1;
        _datasetRepositoryMock.Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatasetModel)null);

        // Act
        var result = await _controller.Graph(datasetId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Graph_ReturnsViewWithGraphData_WhenDatasetFound()
    {
        // Arrange
        int datasetId = 1;
        var dataset = new DatasetModel { Id = datasetId, Name = "TestDataset" };
        string graphJson = "{\"nodes\":[],\"edges\":[]}";

        _datasetRepositoryMock.Setup(repo => repo.GetByIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataset);
        _graphBuilderServiceMock.Setup(service => service.BuildGraphDataAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(graphJson);

        // Act
        var result = await _controller.Graph(datasetId, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);

        Assert.Null(viewResult.ViewName);
        Assert.Equal(graphJson, _controller.ViewBag.GraphData);
        Assert.Equal(dataset.Name, _controller.ViewBag.DatasetName);
    }
}