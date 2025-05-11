using Microsoft.EntityFrameworkCore;
using SocialNetworkAnalyser.Data;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Repositories;

public class DatasetRepository : IDatasetRepository
{
    private readonly ApplicationDbContext _context;

    public DatasetRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<DatasetModel>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.Datasets.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<DatasetModel?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        await _context.Datasets.FindAsync([id], cancellationToken);
}