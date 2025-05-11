namespace SocialNetworkAnalyser.Models;

public class FriendshipModel
{
    public int Id { get; set; }
    public string UserA { get; set; } = null!;
    public string UserB { get; set; } = null!;
    public int DatasetId { get; set; }
    public DatasetModel Dataset { get; set; } = null!;
}