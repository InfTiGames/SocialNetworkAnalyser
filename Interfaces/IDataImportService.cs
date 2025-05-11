namespace SocialNetworkAnalyser.Interfaces;

public interface IDataImportService
{
    Task<bool> ImportDatasetAsync(string datasetName, string filePath, CancellationToken cancellationToken);
}