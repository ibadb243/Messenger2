using System.ComponentModel.DataAnnotations;

namespace Messenger2.Infrastructure.Persistence.CassandraDb;

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
