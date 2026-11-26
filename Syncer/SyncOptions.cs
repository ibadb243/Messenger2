using System.ComponentModel.DataAnnotations;

namespace Messenger2.Syncer;

public class SyncOptions
{
    public const string SectionName = "Sync";

    [Range(1, int.MaxValue)]
    public int IntervalSeconds { get; set; } = 5;

    /// <summary>Сколько записей параллельно пишется в хранилище чтения.</summary>
    [Range(1, 256)]
    public int ParallelDegree { get; set; } = 5;
}
