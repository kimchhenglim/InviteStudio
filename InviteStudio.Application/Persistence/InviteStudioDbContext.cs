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
    public DbSet<Event> Events => Set<Event>();

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

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(@event => @event.Id);
            entity.Property(@event => @event.EventType).HasConversion<string>().HasMaxLength(100).IsRequired();
            entity.Property(@event => @event.Person1Name).HasMaxLength(200).IsRequired();
            entity.Property(@event => @event.Person2Name).HasMaxLength(200).IsRequired();
            entity.Property(@event => @event.Person1Phone).HasMaxLength(40);
            entity.Property(@event => @event.Person2Phone).HasMaxLength(40);
            entity.Property(@event => @event.EventDate).IsRequired();
            entity.Property(@event => @event.Venue).HasMaxLength(250).IsRequired();
            entity.Property(@event => @event.VenueMapLink).HasMaxLength(500);
            entity.Property(@event => @event.VideoLink).HasMaxLength(500);
            entity.Property(@event => @event.MusicLink).HasMaxLength(500);
        });
    }
}
