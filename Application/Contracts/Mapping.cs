using Messenger2.Application.Models;
using Messenger2.Domain.Entities;

namespace Messenger2.Application.Contracts;

internal static class Mapping
{
    public static UserDto ToDto(this User u) => new(u.Id, u.FirstName, u.LastName, u.AvatarUrl);

    public static MeDto ToMeDto(this User u) => new(u.Id, u.Email, u.FirstName, u.LastName, u.AvatarUrl, u.CreatedAt);

    public static ChatDto ToDto(this Chat c) => new(c.Id, c.Name, c.AvatarUrl, c.Type, c.CreatedBy, c.CreatedAt);

    public static MemberDto ToDto(this Member m, User u) =>
        new(u.Id, u.FirstName, u.LastName, u.AvatarUrl, m.Role, m.CreatedAt);

    public static MessageDto ToDto(this Message m) =>
        new(m.Id, m.ChatId, m.SenderId, m.Text, m.CreatedAt, m.EditedAt);

    public static MessageDto ToDto(this MessageByChat m) =>
        new(m.Id, m.ChatId, m.SenderId, m.Text, m.CreatedAt, m.EditedAt);
}
