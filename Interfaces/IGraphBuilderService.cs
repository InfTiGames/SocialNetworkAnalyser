namespace SocialNetworkAnalyser.Interfaces;

public interface IGraphBuilderService
{
    Task<string> BuildGraphDataAsync(int datasetId, CancellationToken cancellationToken);
}
