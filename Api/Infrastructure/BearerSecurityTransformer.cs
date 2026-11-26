using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Messenger2.Api.Infrastructure;

/// <summary>Добавляет в OpenAPI-документ JWT Bearer, чтобы в Swagger UI работала кнопка Authorize.</summary>
public sealed class BearerSecurityTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Вставьте токен из /api/auth/login или /api/auth/register (без префикса Bearer).",
        };

        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(SchemeName, document)] = [] },
        ];

        return Task.CompletedTask;
    }
}
