namespace Messenger2;

public enum ChatType { Private, Group }

public enum MemberRole { Owner, Admin, Member }

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

public class Member
{
    public int UserId { get; set; }
    public int ChatId { get; set; }
    public MemberRole Role { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Chat Chat { get; set; } = null!;
}

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

public sealed class UserChange
{
    public int Id { get; set; }
    public bool RowExists { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AvatarUrl { get; set; }
}

public sealed class ChatChange
{
    public int Id { get; set; }
    public bool RowExists { get; set; }
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Type { get; set; }
    public int? CreatedBy { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

public sealed class MemberChange
{
    public int UserId { get; set; }
    public int ChatId { get; set; }
    public bool RowExists { get; set; }
    public string? Role { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

public sealed class MessageChange
{
    public long Id { get; set; }
    public bool RowExists { get; set; }
    public int? ChatId { get; set; }
    public int? SenderId { get; set; }
    public string? Text { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? EditedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}