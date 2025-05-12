using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;
using SocialNetworkAnalyser.Services;

namespace SocialNetworkAnalyser.Tests.ServicesTests;

public class AnalysisServiceTests
{
    private readonly Mock<IFriendshipRepository> _friendshipRepoMock;
    private readonly Mock<ILogger<AnalysisService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly IAnalysisService _service;

    public AnalysisServiceTests()
    {
        _friendshipRepoMock = new Mock<IFriendshipRepository>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<AnalysisService>>();
        _service = new AnalysisService(_friendshipRepoMock.Object, _cache, _loggerMock.Object);
    }

    [Fact]
    public async Task GetAnalysisAsync_ReturnsCachedResult_WhenResultAlreadyCached()
    {
        // Arrange
        int datasetId = 1;
        var cachedResult = new AnalysisResultModel
        {
            TotalUsers = 42,
            AverageFriendsPerUser = 4.2,
            AverageMaximalCliqueSize = 3.0,
            AverageCountsPerDistance = new Dictionary<int, double> { { 1, 5.0 } }
        };
        _cache.Set($"AnalysisResult_{datasetId}", cachedResult, TimeSpan.FromMinutes(30));

        // Act
        var result = await _service.GetAnalysisAsync(datasetId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.TotalUsers);
        Assert.Equal(4.2, result.AverageFriendsPerUser);
        Assert.Equal(3.0, result.AverageMaximalCliqueSize);
        Assert.True(result.AverageCountsPerDistance.ContainsKey(1));
        Assert.Equal(5.0, result.AverageCountsPerDistance[1]);

        _friendshipRepoMock.Verify(repo => repo.GetByDatasetIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAnalysisAsync_ComputesCorrectAnalysis_ForCompleteGraph()
    {
        int datasetId = 2;
        var friendships = new List<FriendshipModel>
        {
            new FriendshipModel { UserA = "Alice", UserB = "Bob" },
            new FriendshipModel { UserA = "Alice", UserB = "Charlie" },
            new FriendshipModel { UserA = "Bob", UserB = "Charlie" }
        };

        _friendshipRepoMock
            .Setup(repo => repo.GetByDatasetIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(friendships);

        // Act
        var result = await _service.GetAnalysisAsync(datasetId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalUsers);
        Assert.Equal(2.0, result.AverageFriendsPerUser, 2);
        Assert.True(result.AverageCountsPerDistance.ContainsKey(1));
        Assert.Equal(2.0, result.AverageCountsPerDistance[1], 1);
        Assert.Equal(3.0, result.AverageMaximalCliqueSize, 1);

        var cached = _cache.Get<AnalysisResultModel>($"AnalysisResult_{datasetId}");
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task GetAnalysisAsync_ThrowsTaskCanceledException_WhenCancellationRequested()
    {
        // Arrange
        int datasetId = 1;
        _friendshipRepoMock
            .Setup(repo => repo.GetByDatasetIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.GetAnalysisAsync(datasetId, CancellationToken.None));
    }
}