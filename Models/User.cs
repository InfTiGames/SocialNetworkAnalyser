namespace SocialNetworkAnalyser.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Friendship> Friendships { get; set; } = [];
}
