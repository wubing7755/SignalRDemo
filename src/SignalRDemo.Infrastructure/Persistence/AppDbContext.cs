using Microsoft.EntityFrameworkCore;
using SignalRDemo.Domain.Aggregates;
using SignalRDemo.Domain.Entities;

namespace SignalRDemo.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ChatRoom> Rooms => Set<ChatRoom>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User 配置
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Id).HasConversion(
                id => id.Value,
                value => SignalRDemo.Domain.ValueObjects.UserId.Create(value));
            entity.Property(u => u.UserName).HasConversion(
                un => un.Value,
                value => SignalRDemo.Domain.ValueObjects.UserName.Create(value))
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(u => u.DisplayName).HasConversion(
                dn => dn.Value,
                value => SignalRDemo.Domain.ValueObjects.DisplayName.Create(value))
                .HasMaxLength(30);
            entity.Property(u => u.PasswordHash).HasConversion(
                ph => ph.Value,
                value => SignalRDemo.Domain.ValueObjects.HashedPassword.From(value))
                .IsRequired();
            entity.HasIndex(u => u.UserName).IsUnique();
        });

        // ChatRoom 配置
        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasConversion(
                id => id.Value,
                value => SignalRDemo.Domain.ValueObjects.RoomId.Create(value));
            entity.Property(r => r.Name).HasConversion(
                n => n.Value,
                value => SignalRDemo.Domain.ValueObjects.RoomName.Create(value))
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(r => r.OwnerId).HasConversion(
                id => id.Value,
                value => SignalRDemo.Domain.ValueObjects.UserId.Create(value));
            entity.HasIndex(r => r.Name).IsUnique();
        });

        // ChatMessage 配置
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasConversion(
                id => id.Value,
                value => SignalRDemo.Domain.ValueObjects.MessageId.Create(value));
            entity.Property(m => m.UserId).HasConversion(
                id => id.Value,
                value => SignalRDemo.Domain.ValueObjects.UserId.Create(value));
            entity.Property(m => m.UserName).HasConversion(
                un => un.Value,
                value => SignalRDemo.Domain.ValueObjects.UserName.Create(value));
            entity.Property(m => m.DisplayName).HasConversion(
                dn => dn.Value,
                value => SignalRDemo.Domain.ValueObjects.DisplayName.Create(value));
            entity.Property(m => m.RoomId).HasConversion(
                id => id.Value,
                value => SignalRDemo.Domain.ValueObjects.RoomId.Create(value));
            entity.HasIndex(m => m.RoomId);
            entity.HasIndex(m => m.Timestamp);
        });
    }
}
