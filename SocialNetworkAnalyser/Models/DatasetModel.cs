namespace SocialNetworkAnalyser.Models;

public class DatasetModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public DateTime ImportDate { get; set; }
    public ICollection<FriendshipModel> Friendships { get; set; } = new List<FriendshipModel>();
}