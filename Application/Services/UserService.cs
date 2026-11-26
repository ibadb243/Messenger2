using Messenger2.Application.Abstractions;
using Messenger2.Application.Contracts;
using Messenger2.Application.Exceptions;

namespace Messenger2.Application.Services;

public class UserService(IUserRepository users)
{
    public async Task<MeDto> GetMeAsync(int userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Пользователь не найден.");
        return user.ToMeDto();
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(id, ct) ?? throw new NotFoundException("Пользователь не найден.");
        return user.ToDto();
    }

    /// <summary>Поиск пользователей для начала чата или добавления в группу; текущий пользователь исключается.</summary>
    public async Task<IReadOnlyList<UserDto>> SearchAsync(string? query, int currentUserId, CancellationToken ct = default)
    {
        var text = query?.Trim() ?? "";
        if (text.Length < Limits.SearchMinLength)
            throw new ValidationException($"Запрос поиска: минимум {Limits.SearchMinLength} символа.");

        var found = await users.SearchAsync(text, Limits.SearchMaxResults + 1, ct);
        return found.Where(u => u.Id != currentUserId).Take(Limits.SearchMaxResults).Select(u => u.ToDto()).ToList();
    }

    public async Task<MeDto> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        Validate.Name(request.FirstName, "Имя");
        Validate.Name(request.LastName, "Фамилия");
        Validate.AvatarUrl(request.AvatarUrl);

        var user = await users.GetByIdAsync(userId, ct) ?? throw new NotFoundException("Пользователь не найден.");
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();

        await users.UpdateAsync(user, ct);
        return user.ToMeDto();
    }
}
