using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Interfaces;

public interface IFriendshipRepository
{
    Task<List<FriendshipModel>> GetByDatasetIdAsync(int datasetId, CancellationToken cancellationToken);
}
