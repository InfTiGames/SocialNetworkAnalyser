using Microsoft.EntityFrameworkCore;
using SocialNetworkAnalyser.Data;
using SocialNetworkAnalyser.Interfaces;
using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Services;

public class DataImportService : IDataImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(ApplicationDbContext context, ILogger<DataImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> ImportDatasetAsync(string datasetName, string filePath, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting dataset import for '{DatasetName}' using file {FilePath}.", datasetName, filePath);
        cancellationToken.ThrowIfCancellationRequested();

        if (await _context.Datasets.AsNoTracking()
                                    .AnyAsync(d => d.Name == datasetName, cancellationToken))
        {
            _logger.LogWarning("Dataset with the same name has already been imported: {DatasetName}", datasetName);
            return false;
        }

        try
        {
            _logger.LogInformation("Creating new dataset record for '{DatasetName}'.", datasetName);
            var dataset = new DatasetModel { Name = datasetName, ImportDate = DateTime.Now };
            _context.Datasets.Add(dataset);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Dataset record created with ID {DatasetId}.", dataset.Id);

            var friendships = new List<FriendshipModel>();
            _logger.LogInformation("Opening file {FilePath} for reading.", filePath);
            using var stream = new StreamReader(filePath);
            string? line;
            while ((line = await stream.ReadLineAsync()) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    friendships.Add(new FriendshipModel { UserA = parts[0], UserB = parts[1], DatasetId = dataset.Id });
                }
                else
                {
                    _logger.LogWarning("Skipping invalid line in file for dataset '{DatasetName}': {Line}", datasetName, line);
                }
            }

            _logger.LogInformation("Adding {Count} friendships for dataset ID {DatasetId}.", friendships.Count, dataset.Id);
            _context.Friendships.AddRange(friendships);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Dataset '{DatasetName}' imported successfully with {Count} friendships.", datasetName, friendships.Count);

            return true;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Operation canceled while importing dataset '{DatasetName}'.", datasetName);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while importing dataset '{DatasetName}'.", datasetName);
            return false;
        }
    }
}