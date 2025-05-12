using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Interfaces;

public interface IDatasetRepository
{
    Task<List<DatasetModel>> GetAllAsync(CancellationToken cancellationToken);
    Task<DatasetModel?> GetByIdAsync(int id, CancellationToken cancellationToken);
}