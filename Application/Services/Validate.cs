using Messenger2.Application.Exceptions;

namespace Messenger2.Application.Services;

internal static class Validate
{
    public static void Name(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > Limits.PersonNameMaxLength)
            throw new ValidationException($"{field}: обязательное поле, не длиннее {Limits.PersonNameMaxLength} символов.");
    }

    public static void Password(string? value)
    {
        if (value is null || value.Length < Limits.PasswordMinLength || value.Length > Limits.PasswordMaxLength)
            throw new ValidationException(
                $"Пароль должен содержать от {Limits.PasswordMinLength} до {Limits.PasswordMaxLength} символов.");
    }

    public static void AvatarUrl(string? value)
    {
        if (value is { Length: > Limits.AvatarUrlMaxLength })
            throw new ValidationException($"Ссылка на аватар не длиннее {Limits.AvatarUrlMaxLength} символов.");
    }

    public static string MessageText(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text) || text.Length > Limits.MessageTextMaxLength)
            throw new ValidationException($"Текст сообщения: от 1 до {Limits.MessageTextMaxLength} символов.");
        return text;
    }
}
