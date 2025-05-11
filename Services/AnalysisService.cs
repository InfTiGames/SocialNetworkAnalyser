using Microsoft.Extensions.Caching.Memory;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;
using System.Collections.Concurrent;

namespace SocialNetworkAnalyser.Services;

public class AnalysisService : IAnalysisService
{
    private readonly IFriendshipRepository _friendshipRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalysisService> _logger;

    public AnalysisService(IFriendshipRepository friendshipRepository, IMemoryCache cache, ILogger<AnalysisService> logger) =>
        (_friendshipRepository, _cache, _logger) = (friendshipRepository, cache, logger);

    public async Task<AnalysisResultModel> GetAnalysisAsync(int datasetId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting analysis for dataset ID {DatasetId}.", datasetId);

        if (_cache.TryGetValue($"AnalysisResult_{datasetId}", out AnalysisResultModel cachedResult))
        {
            _logger.LogInformation("Returning cached analysis result for dataset ID {DatasetId}.", datasetId);
            return cachedResult;
        }

        var friendships = await _friendshipRepository.GetByDatasetIdAsync(datasetId, cancellationToken);
        _logger.LogInformation("Retrieved {Count} friendships for dataset ID {DatasetId}.", friendships.Count, datasetId);

        var graph = new Dictionary<string, HashSet<string>>(friendships.Count * 2);

        foreach (var f in friendships)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!graph.TryGetValue(f.UserA, out var setA))
            {
                setA = new HashSet<string>();
                graph[f.UserA] = setA;
            }
            if (!graph.TryGetValue(f.UserB, out var setB))
            {
                setB = new HashSet<string>();
                graph[f.UserB] = setB;
            }
            setA.Add(f.UserB);
            setB.Add(f.UserA);
        }

        int totalUsers = graph.Count;
        double avgFriends = totalUsers > 0
            ? graph.Values.Sum(neighbors => neighbors.Count) / (double)totalUsers
            : 0;
        _logger.LogInformation("Graph constructed: Total users = {TotalUsers}, Average friends per user = {AvgFriends}.", totalUsers, avgFriends);

        var averageCounts = ComputeAverageReachableCounts(graph, cancellationToken);
        _logger.LogInformation("Computed average reachable counts for {LevelCount} levels.", averageCounts.Count);

        var cliques = BronKerboschMaximalCliques(graph, cancellationToken);
        double avgCliqueSize = cliques.Count > 0 ? cliques.Average(c => c.Count) : 0;
        _logger.LogInformation("Found {CliqueCount} cliques, average clique size = {AvgCliqueSize}.", cliques.Count, avgCliqueSize);

        var result = new AnalysisResultModel
        {
            TotalUsers = totalUsers,
            AverageFriendsPerUser = avgFriends,
            AverageCountsPerDistance = averageCounts,
            AverageMaximalCliqueSize = avgCliqueSize
        };

        _cache.Set($"AnalysisResult_{datasetId}", result, TimeSpan.FromMinutes(30));
        _logger.LogInformation("Analysis result cached for dataset ID {DatasetId}.", datasetId);
        return result;
    }

    private Dictionary<int, double> ComputeAverageReachableCounts(Dictionary<string, HashSet<string>> graph, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting computation of average reachable counts.");
        var distanceSums = new ConcurrentDictionary<int, int>();
        int totalUsers = graph.Count;
        var nodes = graph.Keys.ToList();
        int sampleSize = totalUsers > 1000 ? 100 : totalUsers;
        _logger.LogInformation("Using sample size of {SampleSize} out of {TotalUsers} total users.", sampleSize, totalUsers);
        var rand = new Random(12345);
        var sample = nodes.OrderBy(x => rand.Next()).Take(sampleSize).ToList();

        Parallel.ForEach(sample,
            new ParallelOptions { CancellationToken = cancellationToken, MaxDegreeOfParallelism = Environment.ProcessorCount },
            user =>
            {
                var distances = BFS(graph, user, cancellationToken);
                foreach (var d in distances.Values)
                {
                    if (d > 0)
                        distanceSums.AddOrUpdate(d, 1, (_, count) => count + 1);
                }
            });

        int maxDistance = distanceSums.Keys.Any() ? distanceSums.Keys.Max() : 0;
        var result = new Dictionary<int, double>();
        for (int d = 1; d <= maxDistance; d++)
            result[d] = distanceSums.TryGetValue(d, out var count) ? count / (double)sampleSize : 0;
        _logger.LogInformation("Completed computing average reachable counts up to distance {MaxDistance}.", maxDistance);
        return result;
    }

    private Dictionary<string, int> BFS(Dictionary<string, HashSet<string>> graph, string start, CancellationToken cancellationToken)
    {
        var distances = new Dictionary<string, int> { [start] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = queue.Dequeue();
            int currDist = distances[current];
            foreach (var neighbor in graph[current])
            {
                if (!distances.ContainsKey(neighbor))
                {
                    distances[neighbor] = currDist + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }
        return distances;
    }

    private List<HashSet<string>> BronKerboschMaximalCliques(Dictionary<string, HashSet<string>> graph, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Bron-Kerbosch maximal clique search.");
        if (graph.Count > 500)
        {
            _logger.LogWarning("Graph size {GraphSize} exceeds limit, sampling first 500 nodes.", graph.Count);
            graph = graph.Take(500).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        var cliques = new List<HashSet<string>>();
        BronKerboschPivot(new HashSet<string>(), new HashSet<string>(graph.Keys), new HashSet<string>(), graph, cliques, cancellationToken);
        _logger.LogInformation("Bron-Kerbosch found {CliqueCount} cliques.", cliques.Count);
        return cliques;
    }

    private void BronKerboschPivot(
        HashSet<string> R,
        HashSet<string> P,
        HashSet<string> X,
        Dictionary<string, HashSet<string>> graph,
        List<HashSet<string>> cliques,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (P.Count == 0 && X.Count == 0)
        {
            lock (cliques)
                cliques.Add(new HashSet<string>(R));
            _logger.LogDebug("Found a clique with {CliqueSize} members.", R.Count);
            return;
        }

        var union = new HashSet<string>(P);
        union.UnionWith(X);
        var pivot = union.OrderByDescending(u => graph[u].Count).FirstOrDefault() ?? P.First();
        var candidates = P.Except(graph[pivot]).ToList();

        foreach (var v in candidates)
        {
            var newR = new HashSet<string>(R) { v };
            var newP = new HashSet<string>(P.Intersect(graph[v]));
            var newX = new HashSet<string>(X.Intersect(graph[v]));
            BronKerboschPivot(newR, newP, newX, graph, cliques, cancellationToken);
            P.Remove(v);
            X.Add(v);
        }
    }
}