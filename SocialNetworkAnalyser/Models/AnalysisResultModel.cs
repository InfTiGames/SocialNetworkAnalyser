namespace SocialNetworkAnalyser.Models;

public class AnalysisResultModel
{
    public int TotalUsers { get; set; }
    public double AverageFriendsPerUser { get; set; }
    public Dictionary<int, double> AverageCountsPerDistance { get; set; } = [];
    public double AverageMaximalCliqueSize { get; set; }
}