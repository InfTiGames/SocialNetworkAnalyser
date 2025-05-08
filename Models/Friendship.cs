namespace SocialNetworkAnalyser.Models;

public class Friendship
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int FriendId { get; set; }
    public User Friend { get; set; } = null!;
}