using Messenger2.Application.Abstractions;
using Messenger2.Application.Contracts;
using Messenger2.Application.Exceptions;
using Messenger2.Domain.Entities;

namespace Messenger2.Application.Services;

public class ChatService(IChatRepository chats, IMemberRepository members, IUserRepository users)
{
    public async Task<IReadOnlyList<ChatDto>> GetMyChatsAsync(int userId, CancellationToken ct = default) =>
        (await chats.GetByUserAsync(userId, ct)).Select(c => c.ToDto()).ToList();

    public async Task<ChatDetailsDto> GetAsync(int chatId, int userId, CancellationToken ct = default)
    {
        await members.RequireMemberAsync(chatId, userId, ct);
        return await LoadDetailsAsync(chatId, ct);
    }

    /// <summary>Личный чат; если он уже существует, возвращается существующий.</summary>
    public async Task<ChatDto> CreatePrivateAsync(int userId, CreatePrivateChatRequest request, CancellationToken ct = default)
    {
        if (request.UserId == userId)
            throw new ValidationException("Нельзя создать чат с самим собой.");
        if (!await users.ExistsAsync(request.UserId, ct))
            throw new NotFoundException("Пользователь не найден.");

        var existing = await chats.FindPrivateChatAsync(userId, request.UserId, ct);
        if (existing is not null) return existing.ToDto();

        // Чат и участники сохраняются одной транзакцией.
        var chat = await chats.AddAsync(new Chat
        {
            Type = ChatType.Private,
            CreatedBy = userId,
            Members =
            [
                new Member { UserId = userId, Role = MemberRole.Member },
                new Member { UserId = request.UserId, Role = MemberRole.Member },
            ],
        }, ct);

        return chat.ToDto();
    }

    public async Task<ChatDto> CreateGroupAsync(int userId, CreateGroupChatRequest request, CancellationToken ct = default)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > Limits.ChatNameMaxLength)
            throw new ValidationException($"Название группы: от 1 до {Limits.ChatNameMaxLength} символов.");
        Validate.AvatarUrl(request.AvatarUrl);

        var memberIds = (request.MemberIds ?? []).Where(id => id != userId).Distinct().ToList();
        if (memberIds.Count + 1 > Limits.GroupMembersMax)
            throw new ValidationException($"В группе не может быть больше {Limits.GroupMembersMax} участников.");

        if (memberIds.Count > 0)
        {
            var found = await users.GetByIdsAsync(memberIds, ct);
            var missing = memberIds.Except(found.Select(u => u.Id)).ToList();
            if (missing.Count > 0)
                throw new NotFoundException($"Пользователи не найдены: {string.Join(", ", missing)}.");
        }

        var chat = await chats.AddAsync(new Chat
        {
            Type = ChatType.Group,
            Name = name,
            AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim(),
            CreatedBy = userId,
            Members = memberIds
                .Select(id => new Member { UserId = id, Role = MemberRole.Member })
                .Prepend(new Member { UserId = userId, Role = MemberRole.Owner })
                .ToList(),
        }, ct);

        return chat.ToDto();
    }

    public async Task<MemberDto> AddMemberAsync(int chatId, int userId, AddMemberRequest request, CancellationToken ct = default)
    {
        var requester = await members.RequireMemberAsync(chatId, userId, ct);
        await RequireGroupAsync(chatId, ct);

        if (requester.Role == MemberRole.Member)
            throw new ForbiddenException("Добавлять участников могут только владелец и администраторы.");
        if (request.Role == MemberRole.Owner)
            throw new ValidationException("Нельзя назначить второго владельца.");
        if (request.Role == MemberRole.Admin && requester.Role != MemberRole.Owner)
            throw new ForbiddenException("Администраторов назначает только владелец.");

        var user = await users.GetByIdAsync(request.UserId, ct) ?? throw new NotFoundException("Пользователь не найден.");
        if (await members.ExistsAsync((user.Id, chatId), ct))
            throw new ConflictException("Пользователь уже состоит в чате.");

        var member = await members.AddAsync(new Member { ChatId = chatId, UserId = user.Id, Role = request.Role }, ct);
        return member.ToDto(user);
    }

    /// <summary>Удаляет участника; если <paramref name="targetUserId"/> совпадает с текущим — это выход из чата.</summary>
    public async Task RemoveMemberAsync(int chatId, int userId, int targetUserId, CancellationToken ct = default)
    {
        var requester = await members.RequireMemberAsync(chatId, userId, ct);
        await RequireGroupAsync(chatId, ct);

        var target = await members.GetByIdAsync((targetUserId, chatId), ct)
            ?? throw new NotFoundException("Участник не найден.");

        if (targetUserId == userId)
        {
            if (requester.Role == MemberRole.Owner)
                throw new ValidationException("Владелец не может покинуть чат.");
        }
        else
        {
            var allowed = requester.Role == MemberRole.Owner
                || (requester.Role == MemberRole.Admin && target.Role == MemberRole.Member);
            if (!allowed)
                throw new ForbiddenException("Недостаточно прав, чтобы удалить этого участника.");
        }

        await members.DeleteAsync((targetUserId, chatId), ct);
    }

    private async Task RequireGroupAsync(int chatId, CancellationToken ct)
    {
        var chat = await chats.GetByIdAsync(chatId, ct) ?? throw new NotFoundException("Чат не найден.");
        if (chat.Type != ChatType.Group)
            throw new ValidationException("Операция доступна только для групповых чатов.");
    }

    private async Task<ChatDetailsDto> LoadDetailsAsync(int chatId, CancellationToken ct)
    {
        var chat = await chats.GetByIdAsync(chatId, ct) ?? throw new NotFoundException("Чат не найден.");
        var chatMembers = await members.GetByChatAsync(chatId, ct);
        var memberUsers = (await users.GetByIdsAsync(chatMembers.Select(m => m.UserId).ToList(), ct))
            .ToDictionary(u => u.Id);

        var dtos = chatMembers
            .Where(m => memberUsers.ContainsKey(m.UserId))
            .Select(m => m.ToDto(memberUsers[m.UserId]))
            .ToList();

        return new ChatDetailsDto(chat.Id, chat.Name, chat.AvatarUrl, chat.Type, chat.CreatedBy, chat.CreatedAt, dtos);
    }
}

internal static class MemberRepositoryExtensions
{
    /// <summary>Возвращает участника чата или бросает 403 (не раскрывая, существует ли чат).</summary>
    public static async Task<Member> RequireMemberAsync(
        this IMemberRepository members, int chatId, int userId, CancellationToken ct) =>
        await members.GetByIdAsync((userId, chatId), ct)
            ?? throw new ForbiddenException("Вы не являетесь участником этого чата.");
}
