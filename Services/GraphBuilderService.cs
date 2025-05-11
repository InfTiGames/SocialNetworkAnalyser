using SocialNetworkAnalyser.Interfaces;
using System.Text.Json;

namespace SocialNetworkAnalyser.Services;

public class GraphBuilderService : IGraphBuilderService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly ILogger<GraphBuilderService> _logger;

    public GraphBuilderService(IFriendshipRepository friendshipRepository, ILogger<GraphBuilderService> logger) =>
        (_friendshipRepository, _logger) = (friendshipRepository, logger);

    public async Task<string> BuildGraphDataAsync(int datasetId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting to build graph data for dataset ID {DatasetId}.", datasetId);

        var friendships = await _friendshipRepository.GetByDatasetIdAsync(datasetId, cancellationToken);
        _logger.LogInformation("Retrieved {Count} friendships for dataset ID {DatasetId}.", friendships.Count, datasetId);

        var nodes = friendships
            .SelectMany(f => new[] { f.UserA, f.UserB })
            .Distinct()
            .Select(user => new { data = new { id = user, label = $"User {user}" } })
            .ToList();
        _logger.LogInformation("Constructed {NodeCount} unique nodes.", nodes.Count);

        var edges = friendships
            .Select(f => new { data = new { id = $"{f.UserA}-{f.UserB}", source = f.UserA, target = f.UserB } })
            .ToList();
        _logger.LogInformation("Constructed {EdgeCount} edges.", edges.Count);

        var graphData = new { nodes, edges };
        string serializedGraph = JsonSerializer.Serialize(graphData);

        _logger.LogInformation("Graph data successfully built for dataset ID {DatasetId}.", datasetId);
        return serializedGraph;
    }
}