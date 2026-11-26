namespace Messenger2.Application;

/// <summary>Ограничения предметной области, общие для валидации и API.</summary>
public static class Limits
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const int EmailMaxLength = 255;
    public const int PersonNameMaxLength = 100;
    public const int AvatarUrlMaxLength = 500;
    public const int ChatNameMaxLength = 100;
    public const int GroupMembersMax = 500;
    public const int MessageTextMaxLength = 4000;

    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public const int SearchMinLength = 2;
    public const int SearchMaxResults = 20;
}
