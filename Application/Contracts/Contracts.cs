using Messenger2.Domain.Entities;

namespace Messenger2.Application.Contracts;

// ---- Запросы ----
public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);
public sealed record LoginRequest(string Email, string Password);
public sealed record UpdateProfileRequest(string FirstName, string LastName, string? AvatarUrl);

public sealed record CreatePrivateChatRequest(int UserId);
public sealed record CreateGroupChatRequest(string Name, string? AvatarUrl, IReadOnlyList<int>? MemberIds);
public sealed record AddMemberRequest(int UserId, MemberRole Role = MemberRole.Member);

public sealed record SendMessageRequest(string Text);
public sealed record EditMessageRequest(string Text);

// ---- Ответы ----
public sealed record UserDto(int Id, string FirstName, string LastName, string? AvatarUrl);

public sealed record MeDto(int Id, string Email, string FirstName, string LastName, string? AvatarUrl, DateTimeOffset CreatedAt);

public sealed record AuthResponse(string Token, DateTimeOffset ExpiresAt, MeDto User);

public sealed record ChatDto(int Id, string? Name, string? AvatarUrl, ChatType Type, int CreatedBy, DateTimeOffset CreatedAt);

public sealed record MemberDto(int UserId, string FirstName, string LastName, string? AvatarUrl, MemberRole Role, DateTimeOffset JoinedAt);

public sealed record ChatDetailsDto(
    int Id, string? Name, string? AvatarUrl, ChatType Type, int CreatedBy, DateTimeOffset CreatedAt,
    IReadOnlyList<MemberDto> Members);

public sealed record MessageDto(long Id, int ChatId, int SenderId, string Text, DateTimeOffset CreatedAt, DateTimeOffset? EditedAt);
