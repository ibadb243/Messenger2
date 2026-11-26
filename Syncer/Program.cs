using Microsoft.Extensions.Hosting;
using Messenger2.Infrastructure;
using Messenger2.Syncer;
using Serilog;

// Минимальный логгер на время старта: поймает ошибки конфигурации до того, как поднимется основной.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog((services, cfg) => cfg
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddOptions<SyncOptions>()
        .Bind(builder.Configuration.GetSection(SyncOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<Synchronizer>();
    builder.Services.AddHostedService<SyncWorker>();

    await builder.Build().RunAsync();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Sync service terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
