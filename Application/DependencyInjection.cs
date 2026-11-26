using Messenger2.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Messenger2.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<ChatService>();
        services.AddScoped<MessageService>();
        return services;
    }
}
