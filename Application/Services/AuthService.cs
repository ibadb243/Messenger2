using Messenger2.Application.Abstractions;
using Messenger2.Application.Contracts;
using Messenger2.Application.Exceptions;
using Messenger2.Domain.Entities;

namespace Messenger2.Application.Services;

public class AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        Validate.Name(request.FirstName, "Имя");
        Validate.Name(request.LastName, "Фамилия");
        Validate.Password(request.Password);

        if (await users.EmailExistsAsync(email, ct))
            throw new ConflictException("Пользователь с таким email уже зарегистрирован.");

        var user = await users.AddAsync(new User
        {
            Email = email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = hasher.Hash(request.Password),
        }, ct);

        return Issue(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), ct);

        // Одинаковая ошибка для «нет пользователя» и «неверный пароль» — не раскрываем, что именно не так.
        if (user is null || !hasher.Verify(request.Password ?? "", user.PasswordHash))
            throw new UnauthorizedException("Неверный email или пароль.");

        return Issue(user);
    }

    private AuthResponse Issue(User user)
    {
        var token = tokens.CreateToken(user);
        return new AuthResponse(token.Value, token.ExpiresAt, user.ToMeDto());
    }

    private static string NormalizeEmail(string? email)
    {
        var value = (email ?? "").Trim().ToLowerInvariant();
        if (value.Length == 0 || value.Length > Limits.EmailMaxLength || !value.Contains('@') || value.Contains(' '))
            throw new ValidationException("Некорректный email.");
        return value;
    }
}
