using Microsoft.EntityFrameworkCore;
using SocialNetworkAnalyser.Data;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Repositories;

public class FriendshipRepository : IFriendshipRepository
{
    private readonly ApplicationDbContext _context;

    public FriendshipRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<FriendshipModel>> GetByDatasetIdAsync(int datasetId, CancellationToken cancellationToken) =>
        await _context.Friendships
                      .AsNoTracking()
                      .Where(f => f.DatasetId == datasetId)
                      .ToListAsync(cancellationToken);
}