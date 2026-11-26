namespace Messenger2.Application.Models;

public class UserById
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
}

public class ChatById
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string Type { get; set; } = null!;
    public int CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class MemberByChat
{
    public int ChatId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}

public class ChatByUser
{
    public int UserId { get; set; }
    public int ChatId { get; set; }
}

public class MessageByChat
{
    public int ChatId { get; set; }
    public long Id { get; set; }
    public int SenderId { get; set; }
    public string Text { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
}

public class SyncState
{
    public string TableName { get; set; } = null!;
    public long LastVersion { get; set; }
}
