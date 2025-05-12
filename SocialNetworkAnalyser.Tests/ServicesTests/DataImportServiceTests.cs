using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SocialNetworkAnalyser.Data;
using SocialNetworkAnalyser.Models;
using SocialNetworkAnalyser.Services;

namespace SocialNetworkAnalyser.Tests.ServicesTests;

public class DataImportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly DataImportService _service;
    private readonly Mock<ILogger<DataImportService>> _loggerMock;

    public DataImportServiceTests()
    {
        _loggerMock = new Mock<ILogger<DataImportService>>();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new DataImportService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task ImportDatasetAsync_ReturnsFalse_WhenDatasetAlreadyExists()
    {
        // Arrange
        string datasetName = "DuplicateDataset";
        _context.Datasets.Add(new DatasetModel { Name = datasetName, ImportDate = DateTime.Now });
        await _context.SaveChangesAsync(CancellationToken.None);

        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Alice Bob\nCharlie David");

        // Act
        bool result = await _service.ImportDatasetAsync(datasetName, tempFile, CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.Equal(1, _context.Datasets.Count());

        File.Delete(tempFile);
    }

    [Fact]
    public async Task ImportDatasetAsync_ReturnsTrue_WhenFileIsValid()
    {
        // Arrange
        string datasetName = "ValidDataset";
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Alice Bob\nBob Charlie");

        // Act
        bool result = await _service.ImportDatasetAsync(datasetName, tempFile, CancellationToken.None);

        // Assert
        Assert.True(result);
        var dataset = _context.Datasets.FirstOrDefault(d => d.Name == datasetName);
        Assert.NotNull(dataset);
        var friendships = _context.Friendships.Where(f => f.DatasetId == dataset.Id).ToList();
        Assert.Equal(2, friendships.Count);

        File.Delete(tempFile);
    }

    [Fact]
    public async Task ImportDatasetAsync_ThrowsOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        string datasetName = "CancellableDataset";
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Alice Bob");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.ImportDatasetAsync(datasetName, tempFile, cts.Token));

        File.Delete(tempFile);
    }

    [Fact]
    public async Task ImportDatasetAsync_ImportsOnlyValidLines_IgnoresInvalidLines()
    {
        // Arrange
        string datasetName = "InvalidLineTest";
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "Alice Bob\nInvalidLineWithoutSpace");

        // Act
        bool result = await _service.ImportDatasetAsync(datasetName, tempFile, CancellationToken.None);

        // Assert
        Assert.True(result);
        var dataset = _context.Datasets.FirstOrDefault(d => d.Name == datasetName);
        Assert.NotNull(dataset);
        var friendships = _context.Friendships.Where(f => f.DatasetId == dataset.Id).ToList();
        Assert.Single(friendships);

        File.Delete(tempFile);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}