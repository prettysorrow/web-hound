namespace EntityFramework;

using Microsoft.EntityFrameworkCore;

public class BackDbContext : DbContext
{
    public BackDbContext(DbContextOptions<BackDbContext> options) : base(options) { }
    public DbSet<DbModels.VerboseUser> VerboseUsers => Set<DbModels.VerboseUser>();
    public DbSet<DbModels.FollowerUser> Followers => Set<DbModels.FollowerUser>();
    public DbSet<DbModels.FollowingUser> Followings => Set<DbModels.FollowingUser>();
    public DbSet<DbModels.Request> Requests => Set<DbModels.Request>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbModels.FollowerUser>().HasKey(e => e.UID);
        modelBuilder.Entity<DbModels.FollowingUser>().HasKey(e => e.UID);
        modelBuilder.Entity<DbModels.Request>().HasKey(e => e.UID);
        modelBuilder.Entity<DbModels.VerboseUser>().HasKey(e => e.UID);

        modelBuilder.Entity<DbModels.VerboseUser>()
            .HasMany(e => e.UserFollowers)
            .WithOne()
            .HasForeignKey("FollowerOfUID");

        modelBuilder.Entity<DbModels.VerboseUser>()
            .HasMany(e => e.UserFollowing)
            .WithOne()
            .HasForeignKey("FollowingOfUID");


        modelBuilder.Entity<DbModels.Request>()
            .HasOne(r => r.VerboseUser)
            .WithMany()
            .HasForeignKey(r => r.VerboseUserUID);
    }
}
