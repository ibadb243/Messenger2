using Cassandra;
using Cassandra.Mapping;
using Messenger2.Application.Abstractions;
using Messenger2.Infrastructure.Persistence.CassandraDb;
using Messenger2.Infrastructure.Persistence.MsSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Messenger2.Infrastructure;

public static class DependencyInjection
{
    public const string MsSqlConnectionName = "MsSqlServer";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddMsSql(configuration)
            .AddCassandra(configuration);
    }

    private static IServiceCollection AddMsSql(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString(MsSqlConnectionName);
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                $"ConnectionStrings:{MsSqlConnectionName} не задана. Укажите её через user-secrets, " +
                $"переменную окружения ConnectionStrings__{MsSqlConnectionName} или appsettings.");

        services.AddDbContext<MsSqlDbContext>(o => o.UseSqlServer(connection));

        services.AddScoped<IChangeTrackingRepository, MsSqlChangeTrackingRepository>();
        services.AddScoped<ISourceDataRepository, MsSqlSourceDataRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<IMemberRepository, MemberRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();

        return services;
    }

    private static IServiceCollection AddCassandra(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CassandraOptions>()
            .Bind(configuration.GetSection(CassandraOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ICluster>(sp =>
        {
            var o = sp.GetRequiredService<IOptions<CassandraOptions>>().Value;
            MappingConfiguration.Global.Define(new CassandraMappings(o.Keyspace));

            sp.GetRequiredService<ILoggerFactory>().CreateLogger("Messenger2.Infrastructure.Cassandra")
                .LogInformation("Connecting to Cassandra at {Hosts}:{Port}", o.Hosts, o.Port);
            return Cluster.Builder()
                .AddContactPoints(o.Hosts)
                .WithPort(o.Port)
                .Build();
        });

        services.AddSingleton<Cassandra.ISession>(sp =>
        {
            var keyspace = sp.GetRequiredService<IOptions<CassandraOptions>>().Value.Keyspace;
            var session = sp.GetRequiredService<ICluster>().Connect(keyspace);
            sp.GetRequiredService<ILoggerFactory>().CreateLogger("Messenger2.Infrastructure.Cassandra")
                .LogInformation("Connected to Cassandra, keyspace {Keyspace}", keyspace);
            return session;
        });

        services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<Cassandra.ISession>()));
        services.AddSingleton<IUserReadRepository, CassandraUserReadRepository>();
        services.AddSingleton<IChatReadRepository, CassandraChatReadRepository>();
        services.AddSingleton<IMemberReadRepository, CassandraMemberReadRepository>();
        services.AddSingleton<IMessageReadRepository, CassandraMessageReadRepository>();
        services.AddSingleton<ISyncStateRepository, CassandraSyncStateRepository>();
        services.AddSingleton<IReadStoreMaintenance, CassandraReadStoreMaintenance>();

        return services;
    }
}
