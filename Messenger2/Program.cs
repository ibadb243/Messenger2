using Cassandra;
using Cassandra.Mapping;
using Messenger2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

// Минимальный логгер на время старта: поймает ошибки конфигурации до того, как поднимется основной.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var mssqlConnection = builder.Configuration.GetConnectionString("MsSqlServer");
    if (string.IsNullOrWhiteSpace(mssqlConnection))
        throw new InvalidOperationException(
            "ConnectionStrings:MsSqlServer не задана. Укажите её через user-secrets, " +
            "переменную окружения ConnectionStrings__MsSqlServer или appsettings.");

    builder.Services.AddOptions<SyncOptions>()
        .Bind(builder.Configuration.GetSection(SyncOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddOptions<CassandraOptions>()
        .Bind(builder.Configuration.GetSection(CassandraOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddDbContext<MsSqlDbContext>(o => o.UseSqlServer(mssqlConnection));

    builder.Services.AddSingleton<ICluster>(sp =>
    {
        var o = sp.GetRequiredService<IOptions<CassandraOptions>>().Value;
        MappingConfiguration.Global.Define(new CassandraMappings(o.Keyspace));

        Log.Information("Connecting to Cassandra at {Hosts}:{Port}", o.Hosts, o.Port);
        return Cluster.Builder()
            .AddContactPoints(o.Hosts)
            .WithPort(o.Port)
            .Build();
    });

    builder.Services.AddSingleton<Cassandra.ISession>(sp =>
    {
        var keyspace = sp.GetRequiredService<IOptions<CassandraOptions>>().Value.Keyspace;
        var session = sp.GetRequiredService<ICluster>().Connect(keyspace);
        Log.Information("Connected to Cassandra, keyspace {Keyspace}", keyspace);
        return session;
    });

    builder.Services.AddSingleton<CassandraStore>();
    builder.Services.AddScoped<MsSqlChangeTracking>();
    builder.Services.AddScoped<Synchronizer>();
    builder.Services.AddHostedService<SyncWorker>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.MapGet("/", () => "Messenger2 sync service");
    app.MapGet("/health", () => Results.Ok());

    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
