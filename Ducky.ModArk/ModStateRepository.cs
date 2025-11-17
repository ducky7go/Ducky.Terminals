using Duckov.Modding;

namespace Ducky.ModArk;

public class ModState
{
    public string Name { get; set; } = string.Empty;

    public ulong PublishedFileId { get; set; }

    public int OrderIndex { get; set; }

    public bool IsEnabled { get; set; }
}

/// <summary>
/// 通过 ModManager 读写本地模组排序与启用状态
/// </summary>
public static class ModStateRepository
{
    public static UniTask<List<ModState>> GetCurrentStatesAsync()
    {
        var states = new List<ModState>();

        // 当前排序顺序即为 modInfos 的顺序
        var infos = ModManager.modInfos.ToList();

        for (var index = 0; index < infos.Count; index++)
        {
            var info = infos[index];
            var state = new ModState
            {
                Name = info.name ?? string.Empty,
                PublishedFileId = info.publishedFileId,
                OrderIndex = index,
                IsEnabled = IsModEnabled(info.name)
            };
            states.Add(state);
        }

        return UniTask.FromResult(states);
    }

    /// <summary>
    /// 应用备份中的排序与启用状态
    /// </summary>
    public static UniTask ApplyStatesAsync(IEnumerable<ModState> states, bool overwrite)
    {
        if (!overwrite)
        {
            throw new InvalidOperationException("ApplyStatesAsync called with overwrite = false.");
        }

        var stateList = states.ToList();

        // 先按 OrderIndex 排序，重新设置优先级
        foreach (var (state, index) in stateList.OrderBy(s => s.OrderIndex).Select((s, idx) => (s, idx)))
        {
            if (string.IsNullOrWhiteSpace(state.Name))
            {
                continue;
            }

            ModManager.SetModPriority(state.Name, index);
        }

        // 再设置启用状态，通过 SavesSystem 修改全局标志
        foreach (var state in stateList)
        {
            if (string.IsNullOrWhiteSpace(state.Name))
            {
                continue;
            }

            // 与原版游戏保持一致的启用标志键
            Saves.SavesSystem.SaveGlobal("ModActive_" + state.Name, state.IsEnabled);
        }

        return UniTask.CompletedTask;
    }

    private static bool IsModEnabled(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        // 读取与游戏一致的启用标志
        return Saves.SavesSystem.LoadGlobal("ModActive_" + name, defaultValue: false);
    }
}


