namespace Messenger2;

public class SyncOptions
{
    public int IntervalSeconds { get; set; } = 5;

    public string[] CassandraHosts { get; set; } = ["localhost"];
    public int CassandraPort { get; set; } = 9042;
}