namespace Messenger2.Domain.Entities;

public class Member
{
    public int UserId { get; set; }
    public int ChatId { get; set; }
    public MemberRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Chat Chat { get; set; } = null!;
}
