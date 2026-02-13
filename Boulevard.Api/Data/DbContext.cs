using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options): base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Store> Stores { get; set; }
    public DbSet<UserStore> UserStores { get; set; }
    public DbSet<Profile> Profiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("uuid-ossp");
        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .HasDefaultValueSql("uuid_generate_v4()");
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Store>()
            .Property(s => s.Id)
            .HasDefaultValueSql("uuid_generate_v4()");        
        
        modelBuilder.Entity<UserStore>()
            .HasKey(us => new { us.UserId, us.StoreId });
        
        modelBuilder.Entity<UserStore>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserStores)
            .HasForeignKey(us => us.UserId);

        modelBuilder.Entity<UserStore>()
            .HasOne(us => us.Store)
            .WithMany(s => s.UserStores)
            .HasForeignKey(us => us.StoreId);
        
        modelBuilder.Entity<Profile>()
            .Property(p => p.Id)
            .HasDefaultValueSql("uuid_generate_v4()");
        
        modelBuilder.Entity<Profile>()
            .HasOne(p => p.User)
            .WithMany(u => u.Profiles)
            .HasForeignKey(p => p.UserId);
    }
}