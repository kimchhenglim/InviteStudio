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
    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<GuestTag> GuestTags => Set<GuestTag>();

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
            entity.Property(@event => @event.TemplateName).HasMaxLength(100);
            entity.Property(@event => @event.AccentColor).HasMaxLength(32).IsRequired();
            entity.Property(@event => @event.BackgroundColor).HasMaxLength(32).IsRequired();
            entity.Property(@event => @event.FontFamily).HasMaxLength(120).IsRequired();
            entity.Property(@event => @event.LayoutStyle).HasMaxLength(40).IsRequired();
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
            entity.Property(@event => @event.TimelineJson).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<GuestTag>(entity =>
        {
            entity.HasKey(tag => tag.Id);
            entity.Property(tag => tag.Name).HasMaxLength(120).IsRequired();
        });

        modelBuilder.Entity<Guest>(entity =>
        {
            entity.HasKey(guest => guest.Id);
            entity.Property(guest => guest.Name).HasMaxLength(200).IsRequired();
            entity.Property(guest => guest.PhoneNumber).HasMaxLength(40);
            entity.Property(guest => guest.Notes).HasMaxLength(500);
            entity.HasOne(guest => guest.GuestTag)
                .WithMany(tag => tag.Guests)
                .HasForeignKey(guest => guest.GuestTagId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });
    }
}
