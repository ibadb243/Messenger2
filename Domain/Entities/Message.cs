namespace Messenger2.Domain.Entities;

public class Message
{
    public long Id { get; set; }
    public int SenderId { get; set; }
    public int ChatId { get; set; }
    public string Text { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User Sender { get; set; } = null!;
    public Chat Chat { get; set; } = null!;
}
