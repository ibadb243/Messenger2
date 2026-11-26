namespace Messenger2.Application;

/// <summary>Таблицы MS SQL, отслеживаемые Change Tracking (они же ключи в sync_state).</summary>
public static class MsSqlTables
{
    public const string Schema = "dbo";

    public const string Users = "Users";
    public const string Chats = "Chats";
    public const string Members = "Members";
    public const string Messages = "Messages";

    /// <summary>Порядок важен: он же порядок применения изменений.</summary>
    public static readonly string[] All = [Users, Chats, Members, Messages];
}
