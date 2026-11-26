namespace Messenger2.Domain.Entities;

public class Chat
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public ChatType Type { get; set; }
    public int CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User Creator { get; set; } = null!;
    public ICollection<Member> Members { get; set; } = new List<Member>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
