using System.ComponentModel.DataAnnotations;

namespace Messenger2.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Ключ подписи HMAC-SHA256. Минимум 32 символа; хранится в секретах, не в репозитории.</summary>
    [Required, MinLength(32)]
    public string Secret { get; set; } = "";

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    [Range(1, 60 * 24 * 30)]
    public int ExpiresMinutes { get; set; } = 60;
}
