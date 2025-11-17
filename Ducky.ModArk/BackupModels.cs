namespace Ducky.ModArk;

/// <summary>
/// 备份快照的根对象
/// </summary>
public class BackupSnapshot
{
    public string Version { get; set; } = "1";

    public string BackupName { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public BackupCollectionInfo Collection { get; set; } = new();

    public List<SubscribedModInfo> SubscribedMods { get; set; } = new();

    public List<ModOrderEntry> ModOrder { get; set; } = new();

    public List<ModEnabledEntry> ModEnabledStates { get; set; } = new();
}

/// <summary>
/// “合集”的元信息（当前实现为工具内部概念）
/// </summary>
public class BackupCollectionInfo
{
    public string CollectionId { get; set; } = string.Empty;

    public string Name { get; set; } = "Mod Ark Backup Collection";

    public string Visibility { get; set; } = "private";
}

public class SubscribedModInfo
{
    public ulong PublishedFileId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public class ModOrderEntry
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 排序位置，从 0 开始
    /// </summary>
    public int OrderIndex { get; set; }
}

public class ModEnabledEntry
{
    public string Name { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }
}


