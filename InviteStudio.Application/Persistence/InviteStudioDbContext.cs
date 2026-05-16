using InviteStudio.Application.Entities;
using Microsoft.EntityFrameworkCore;

namespace InviteStudio.Application.Persistence;

public class InviteStudioDbContext : DbContext
{
    public InviteStudioDbContext(DbContextOptions<InviteStudioDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Name).HasMaxLength(200).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(320).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });
    }
}
