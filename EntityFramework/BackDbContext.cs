namespace EntityFramework;

using Microsoft.EntityFrameworkCore;

public class BackDbContext : DbContext
{
    public BackDbContext(DbContextOptions<BackDbContext> options) : base(options) { }
    public DbSet<Entities.VerboseUser> VerboseUsers => Set<Entities.VerboseUser>();
    public DbSet<Entities.FollowerUser> Followers => Set<Entities.FollowerUser>();
    public DbSet<Entities.FollowingUser> Followings => Set<Entities.FollowingUser>();
    public DbSet<Entities.Request> Requests => Set<Entities.Request>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.FollowerUser>().HasKey(e => e.UID);
        modelBuilder.Entity<Entities.FollowingUser>().HasKey(e => e.UID);
        modelBuilder.Entity<Entities.Request>().HasKey(e => e.UID);
        modelBuilder.Entity<Entities.VerboseUser>().HasKey(e => e.UID);

        modelBuilder.Entity<Entities.VerboseUser>()
            .HasMany(e => e.UserFollowers)
            .WithOne()
            .HasForeignKey("FollowerOfUID");

        modelBuilder.Entity<Entities.VerboseUser>()
            .HasMany(e => e.UserFollowing)
            .WithOne()
            .HasForeignKey("FollowingOfUID");


        modelBuilder.Entity<Entities.Request>()
            .HasOne(r => r.VerboseUser)
            .WithMany()
            .HasForeignKey(r => r.VerboseUserUID);
    }
}
