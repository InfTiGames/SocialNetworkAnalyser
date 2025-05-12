using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Interfaces;

public interface IAnalysisService
{
    Task<AnalysisResultModel> GetAnalysisAsync(int datasetId, CancellationToken cancellationToken);
}