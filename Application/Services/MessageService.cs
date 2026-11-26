using Messenger2.Application.Abstractions;
using Messenger2.Application.Contracts;
using Messenger2.Application.Exceptions;
using Messenger2.Domain.Entities;

namespace Messenger2.Application.Services;

public class MessageService(
    IMessageRepository messages,
    IMemberRepository members,
    IMessageReadRepository history)
{
    public async Task<MessageDto> SendAsync(int chatId, int userId, SendMessageRequest request, CancellationToken ct = default)
    {
        var text = Validate.MessageText(request.Text);
        await members.RequireMemberAsync(chatId, userId, ct);

        var message = await messages.AddAsync(new Message { ChatId = chatId, SenderId = userId, Text = text }, ct);
        return message.ToDto();
    }

    public async Task<MessageDto> EditAsync(
        int chatId, long messageId, int userId, EditMessageRequest request, CancellationToken ct = default)
    {
        var text = Validate.MessageText(request.Text);
        await members.RequireMemberAsync(chatId, userId, ct);

        var message = await FindAsync(chatId, messageId, ct);
        if (message.SenderId != userId)
            throw new ForbiddenException("Редактировать можно только свои сообщения.");

        message.Text = text;
        message.EditedAt = DateTimeOffset.UtcNow;
        await messages.UpdateAsync(message, ct);
        return message.ToDto();
    }

    /// <summary>Автор удаляет своё сообщение; владелец и администраторы — любое в чате.</summary>
    public async Task DeleteAsync(int chatId, long messageId, int userId, CancellationToken ct = default)
    {
        var member = await members.RequireMemberAsync(chatId, userId, ct);
        var message = await FindAsync(chatId, messageId, ct);

        if (message.SenderId != userId && member.Role == MemberRole.Member)
            throw new ForbiddenException("Удалить чужое сообщение могут только владелец и администраторы.");

        await messages.DeleteAsync(messageId, ct);
    }

    /// <summary>
    /// История читается из Cassandra (хранилище чтения), поэтому новые сообщения появляются в ней
    /// с задержкой, равной интервалу синхронизации.
    /// </summary>
    public async Task<IReadOnlyList<MessageDto>> GetHistoryAsync(
        int chatId, int userId, long? beforeId, int? limit, CancellationToken ct = default)
    {
        await members.RequireMemberAsync(chatId, userId, ct);

        var size = Math.Clamp(limit ?? Limits.DefaultPageSize, 1, Limits.MaxPageSize);
        var rows = await history.GetByChatAsync(chatId, beforeId, size);
        return rows.Select(m => m.ToDto()).ToList();
    }

    private async Task<Message> FindAsync(int chatId, long messageId, CancellationToken ct)
    {
        var message = await messages.GetByIdAsync(messageId, ct);
        return message is not null && message.ChatId == chatId
            ? message
            : throw new NotFoundException("Сообщение не найдено.");
    }
}
