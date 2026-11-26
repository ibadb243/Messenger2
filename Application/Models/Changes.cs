namespace Messenger2.Application.Models;

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