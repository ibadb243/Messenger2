using Cassandra;
using Cassandra.Mapping;
using Messenger2;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

MappingConfiguration.Global.Define<CassandraMappings>();

var builder = WebApplication.CreateBuilder(args);

var mssqlConnection = builder.Configuration.GetConnectionString("MsSqlServer")
    ?? throw new InvalidOperationException("ConnectionStrings:MsSqlServer not found");

builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection("Sync"));

builder.Services.AddDbContext<MsSqlDbContext>(o => o.UseSqlServer(mssqlConnection));

builder.Services.AddSingleton<ICluster>(sp =>
{
    var o = sp.GetRequiredService<IOptions<SyncOptions>>().Value;
    return Cluster.Builder()
        .AddContactPoints(o.CassandraHosts)
        .WithPort(o.CassandraPort)
        .Build();
});

builder.Services.AddSingleton<Cassandra.ISession>(sp => sp.GetRequiredService<ICluster>().Connect("messenger"));

builder.Services.AddSingleton<CassandraStore>();
builder.Services.AddScoped<MsSqlChangeTracking>();
builder.Services.AddScoped<Synchronizer>();
builder.Services.AddHostedService<SyncWorker>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
