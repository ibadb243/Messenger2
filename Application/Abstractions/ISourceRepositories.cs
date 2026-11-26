using Messenger2.Domain.Entities;
using Messenger2.Application.Models;

namespace Messenger2.Application.Abstractions;

/// <summary>Транзакция чтения из источника (MS SQL). Без CommitAsync откатывается при Dispose.</summary>
public interface ISourceTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}

/// <summary>Репозиторий MS SQL: версии и изменения Change Tracking.</summary>
public interface IChangeTrackingRepository
{
    /// <summary>Открывает snapshot-транзакцию, в рамках которой версия и изменения читаются согласованно.</summary>
    Task<ISourceTransaction> BeginSnapshotAsync(CancellationToken ct);

    Task<long> CurrentVersionAsync(CancellationToken ct);
    Task<long> MinValidVersionAsync(string table, CancellationToken ct);

    Task<List<UserChange>> UserChangesAsync(long since, CancellationToken ct);
    Task<List<ChatChange>> ChatChangesAsync(long since, CancellationToken ct);
    Task<List<MemberChange>> MemberChangesAsync(long since, CancellationToken ct);
    Task<List<MessageChange>> MessageChangesAsync(long since, CancellationToken ct);
}

/// <summary>Репозиторий MS SQL: полное чтение доменных сущностей (для полной перезагрузки).</summary>
public interface ISourceDataRepository
{
    IAsyncEnumerable<User> AllUsers();
    IAsyncEnumerable<Chat> AllChats();
    IAsyncEnumerable<Member> AllMembers();
    IAsyncEnumerable<Message> AllLiveMessages();
}
