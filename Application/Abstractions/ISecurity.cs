using Messenger2.Domain.Entities;

namespace Messenger2.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
