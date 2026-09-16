using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext
{
    public DbSet<Game> Games { get; set; } = null;
    public DbSet<UserGame> UserGames { get; set; } = null;

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Game>(b =>
        {
            b.HasKey(g => g.Id);
            b.Property(g => g.Title).IsRequired().HasMaxLength(250);
            b.Property(g => g.Description).HasMaxLength(2000);
            b.Property(g => g.Price).HasColumnType("decimal(10,2)");
            b.Property(g => g.Developer).HasMaxLength(200);
        });

        modelBuilder.Entity<UserGame>(b =>
        {
            b.HasKey(ug => new { ug.UserId, ug.GameId });
            b.HasOne(ug => ug.Game).WithMany().HasForeignKey(ug => ug.GameId);
            b.Property(ug => ug.AcquiredAt).IsRequired();
        });
    }
}
