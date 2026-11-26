using System.Text;
using System.Text.Json.Serialization;
using Messenger2.Api.Endpoints;
using Messenger2.Api.Infrastructure;
using Messenger2.Application;
using Messenger2.Infrastructure;
using Messenger2.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddSecurity(builder.Configuration);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;
            bearer.MapInboundClaims = false;
            bearer.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });
    builder.Services.AddAuthorization();

    builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<AppExceptionHandler>();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
        app.MapOpenApi();

    app.MapGet("/", () => "Messenger2 API").AllowAnonymous();
    app.MapGet("/health", () => Results.Ok()).AllowAnonymous();

    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.MapChatEndpoints();

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
