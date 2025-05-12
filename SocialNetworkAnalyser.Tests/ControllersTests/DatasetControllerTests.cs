using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SocialNetworkAnalyser.Controllers;
using SocialNetworkAnalyser.Data;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Tests.ControllersTests;

public class DatasetControllerTests : IDisposable
{
    private readonly Mock<IDataImportService> _dataImportServiceMock;
    private readonly Mock<ILogger<DatasetController>> _loggerMock;
    private readonly ApplicationDbContext _context;
    private readonly DatasetController _controller;

    public DatasetControllerTests()
    {
        _dataImportServiceMock = new Mock<IDataImportService>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestDatasetDb")
            .Options;
        _context = new ApplicationDbContext(options);

        _loggerMock = new Mock<ILogger<DatasetController>>();
        _controller = new DatasetController(_dataImportServiceMock.Object, _context, _loggerMock.Object);
    }

    [Fact]
    public async Task Import_Post_ModelStateInvalid_ReturnsView()
    {
        // Arrange
        _controller.ModelState.AddModelError("DatasetName", "Required");
        var viewModel = new DatasetImportViewModel();

        // Act
        var result = await _controller.Import(viewModel, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task Import_Post_FileMissing_ReturnsViewWithError()
    {
        // Arrange
        var viewModel = new DatasetImportViewModel { DatasetName = "TestDataset", UploadFile = null };

        // Act
        var result = await _controller.Import(viewModel, CancellationToken.None);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
        Assert.True(_controller.ModelState.ErrorCount > 0);
    }

    [Fact]
    public async Task Import_Post_ValidModel_CallsImportServiceAndRedirectsToIndex()
    {
        // Arrange
        var viewModel = new DatasetImportViewModel
        {
            DatasetName = "TestDataset",
            UploadFile = GetTestFormFile("test.txt", "Hello, world!")
        };

        _dataImportServiceMock
            .Setup(s => s.ImportDatasetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Import(viewModel, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task Delete_ValidId_DeletesDatasetAndRedirectsToIndex()
    {
        // Arrange
        var dataset = new DatasetModel { Id = 1, Name = "TestDataset", ImportDate = DateTime.Now };
        _context.Datasets.Add(dataset);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(dataset.Id, CancellationToken.None);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.False(_context.Datasets.Any(d => d.Id == dataset.Id));
    }

    private IFormFile GetTestFormFile(string fileName, string content)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "UploadFile", fileName);
    }

    public void Dispose() => _context.Dispose();
}