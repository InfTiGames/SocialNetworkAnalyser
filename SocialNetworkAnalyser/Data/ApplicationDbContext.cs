using Microsoft.EntityFrameworkCore;
using SocialNetworkAnalyser.Models;

namespace SocialNetworkAnalyser.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DatasetModel> Datasets { get; set; } = null!;
    public DbSet<FriendshipModel> Friendships { get; set; } = null!;
}