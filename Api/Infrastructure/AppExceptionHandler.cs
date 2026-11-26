using Messenger2.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Messenger2.Api.Infrastructure;

/// <summary>Превращает прикладные исключения в ответы ProblemDetails с нужным HTTP-статусом.</summary>
public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Некорректный запрос"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Не авторизован"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Доступ запрещён"),
            NotFoundException => (StatusCodes.Status404NotFound, "Не найдено"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфликт"),
            _ => (0, ""),
        };

        // Всё остальное — настоящая ошибка сервера: отдаём стандартному обработчику (500).
        if (status == 0) return false;

        logger.LogInformation("{Title}: {Message}", title, exception.Message);
        await Results.Problem(statusCode: status, title: title, detail: exception.Message).ExecuteAsync(context);
        return true;
    }
}
