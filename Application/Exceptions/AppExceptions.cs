namespace Messenger2.Application.Exceptions;

/// <summary>Базовое прикладное исключение: безопасно показывать пользователю его сообщение.</summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>Некорректные входные данные (400).</summary>
public sealed class ValidationException(string message) : AppException(message);

/// <summary>Неверные учётные данные (401).</summary>
public sealed class UnauthorizedException(string message) : AppException(message);

/// <summary>Нет прав на операцию (403).</summary>
public sealed class ForbiddenException(string message) : AppException(message);

/// <summary>Объект не найден (404).</summary>
public sealed class NotFoundException(string message) : AppException(message);

/// <summary>Конфликт состояния, например дубликат (409).</summary>
public sealed class ConflictException(string message) : AppException(message);
