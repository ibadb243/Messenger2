namespace Messenger2.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Chat> CreatedChats { get; set; } = new List<Chat>();
    public ICollection<Member> Memberships { get; set; } = new List<Member>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
