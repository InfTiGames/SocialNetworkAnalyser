using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;
using SocialNetworkAnalyser.Services;

namespace SocialNetworkAnalyser.Tests.ServicesTests;

public class GraphBuilderServiceTests
{
    private readonly Mock<IFriendshipRepository> _friendshipRepositoryMock;
    private readonly Mock<ILogger<GraphBuilderService>> _loggerMock;
    private readonly IGraphBuilderService _service;

    public GraphBuilderServiceTests()
    {
        _friendshipRepositoryMock = new Mock<IFriendshipRepository>();
        _loggerMock = new Mock<ILogger<GraphBuilderService>>();

        _service = new GraphBuilderService(_friendshipRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task BuildGraphDataAsync_ReturnsEmptyGraph_WhenNoFriendships()
    {
        // Arrange
        int datasetId = 1;
        _friendshipRepositoryMock.Setup(repo => repo.GetByDatasetIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FriendshipModel>());

        // Act
        string jsonResult = await _service.BuildGraphDataAsync(datasetId, CancellationToken.None);

        // Assert
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("nodes", out var nodes));
        Assert.True(root.TryGetProperty("edges", out var edges));
        Assert.Equal(0, nodes.GetArrayLength());
        Assert.Equal(0, edges.GetArrayLength());
    }

    [Fact]
    public async Task BuildGraphDataAsync_ReturnsValidGraph_WhenFriendshipsExist()
    {
        // Arrange
        int datasetId = 2;
        var friendships = new List<FriendshipModel>
        {
            new FriendshipModel { UserA = "Alice", UserB = "Bob" },
            new FriendshipModel { UserA = "Bob", UserB = "Charlie" },
            new FriendshipModel { UserA = "Charlie", UserB = "Alice" }
        };

        _friendshipRepositoryMock.Setup(repo => repo.GetByDatasetIdAsync(datasetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(friendships);

        // Act
        string jsonResult = await _service.BuildGraphDataAsync(datasetId, CancellationToken.None);

        // Assert 
        using var doc = JsonDocument.Parse(jsonResult);
        var root = doc.RootElement;


        Assert.True(root.TryGetProperty("nodes", out var nodesElement));
        var nodeIds = nodesElement.EnumerateArray()
            .Select(node => node.GetProperty("data").GetProperty("id").GetString())
            .ToHashSet();
        Assert.Equal(3, nodeIds.Count);  // Alice, Bob, Charlie


        Assert.True(root.TryGetProperty("edges", out var edgesElement));
        var edgesArray = edgesElement.EnumerateArray().ToList();
        Assert.Equal(3, edgesArray.Count);
        foreach (var edge in edgesArray)
        {
            var dataElement = edge.GetProperty("data");
            var source = dataElement.GetProperty("source").GetString();
            var target = dataElement.GetProperty("target").GetString();
            string expectedId = $"{source}-{target}";
            Assert.Equal(expectedId, dataElement.GetProperty("id").GetString());
        }
    }

    [Fact]
    public async Task BuildGraphDataAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        // Arrange
        int datasetId = 3;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _friendshipRepositoryMock.Setup(repo => repo.GetByDatasetIdAsync(datasetId, cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.BuildGraphDataAsync(datasetId, cts.Token));
    }
}