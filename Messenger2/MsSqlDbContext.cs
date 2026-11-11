using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Messenger2;

public class MsSqlDbContext(DbContextOptions<MsSqlDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.FirstName).HasColumnName("firstname");
            e.Property(x => x.LastName).HasColumnName("lastname");
            e.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
            e.Property(x => x.Email).HasColumnName("email");
            e.Property(x => x.PasswordHash).HasColumnName("password_hash");
            e.Property(x => x.CreatedAt).HasColumnName("created_at")
                .HasColumnType("datetimeoffset").HasDefaultValueSql("SYSUTCDATETIME()");
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Chat>(e =>
        {
            e.ToTable("Chats");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.Name).HasColumnName("name");
            e.Property(x => x.AvatarUrl).HasColumnName("avatar_url");
            e.Property(x => x.Type).HasColumnName("type")
                .HasConversion<LowercaseEnumConverter<ChatType>>();
            e.Property(x => x.CreatedBy).HasColumnName("created_by");
            e.Property(x => x.CreatedAt).HasColumnName("created_at")
                .HasColumnType("datetimeoffset").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.Creator).WithMany(u => u.CreatedChats)
                .HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Member>(e =>
        {
            e.ToTable("Members");
            e.HasKey(x => new { x.UserId, x.ChatId });
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.ChatId).HasColumnName("chat_id");
            e.Property(x => x.Role).HasColumnName("role")
                .HasConversion<LowercaseEnumConverter<MemberRole>>();
            e.Property(x => x.CreatedAt).HasColumnName("created_at")
                .HasColumnType("datetimeoffset").HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.User).WithMany(u => u.Memberships)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Chat).WithMany(c => c.Members)
                .HasForeignKey(x => x.ChatId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.ToTable("Messages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(x => x.SenderId).HasColumnName("sender_id");
            e.Property(x => x.ChatId).HasColumnName("chat_id");
            e.Property(x => x.Text).HasColumnName("text");
            e.Property(x => x.CreatedAt).HasColumnName("created_at")
                .HasColumnType("datetimeoffset").HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(x => x.EditedAt).HasColumnName("edited_at").HasColumnType("datetimeoffset");
            e.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("datetimeoffset");

            e.HasOne(x => x.Sender).WithMany(u => u.Messages)
                .HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Chat).WithMany(c => c.Messages)
                .HasForeignKey(x => x.ChatId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

public sealed class LowercaseEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    v => v.ToString().ToLowerInvariant(),
    s => Enum.Parse<TEnum>(s, true))
    where TEnum : struct, Enum
{
}