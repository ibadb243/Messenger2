using Messenger2.Application.Abstractions;
using Messenger2.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Messenger2.Infrastructure;

public static class SecurityServiceCollectionExtensions
{
    /// <summary>Хэширование паролей и выпуск JWT. Нужно только сервисам, которые аутентифицируют пользователей.</summary>
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        return services;
    }
}
