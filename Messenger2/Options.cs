using System.ComponentModel.DataAnnotations;

namespace Messenger2;

public class SyncOptions
{
    public const string SectionName = "Sync";

    [Range(1, int.MaxValue)]
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>Сколько записей параллельно пишется в Cassandra.</summary>
    [Range(1, 256)]
    public int ParallelDegree { get; set; } = 5;
}

public class CassandraOptions
{
    public const string SectionName = "Cassandra";

    [Required, MinLength(1)]
    public string[] Hosts { get; set; } = [];

    [Range(1, 65535)]
    public int Port { get; set; } = 9042;

    [Required]
    public string Keyspace { get; set; } = "";

    /// <summary>Таймаут чтения для TRUNCATE (он может быть долгим).</summary>
    [Range(1, int.MaxValue)]
    public int TruncateTimeoutMs { get; set; } = 60_000;
}
