using Duckov.Modding;
using Newtonsoft.Json;
using Steamworks;
using UnityEngine;

namespace Ducky.ModArk;

/// <summary>
/// 负责具体的备份与还原实现
/// </summary>
public static class ModArkBackupService
{
    private const string BackupRootFolderName = "DuckyModArk";

    /// <summary>
    /// 备份过程中产生的进度、提示与错误信息。
    /// 外部可以订阅该事件，并在回调中调用 Contract.ModTerminalClient.ShowToTerminal(message)。
    /// </summary>
    public static event Action<string>? OnBackupMessage;

    /// <summary>
    /// 还原过程中产生的进度、提示与错误信息。
    /// 外部可以订阅该事件，并在回调中调用 Contract.ModTerminalClient.ShowToTerminal(message)。
    /// </summary>
    public static event Action<string>? OnRestoreMessage;

    private static string GetTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), BackupRootFolderName);
        Directory.CreateDirectory(root);
        return root;
    }

    private static string SanitizeName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name.Trim();
    }

    public static async UniTask<string> BackupAsync(string backupName)
    {
        if (!SteamManager.Initialized)
        {
            const string msg = "[ModArk] Steam is not initialized, cannot backup subscriptions.";
            PublishBackupMessage(msg);
            throw new InvalidOperationException(msg);
        }

        var root = GetTempRoot();
        var folderName = $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{SanitizeName(backupName)}";
        var backupDir = Path.Combine(root, folderName);
        Directory.CreateDirectory(backupDir);

        PublishBackupMessage($"[ModArk] Creating backup at: {backupDir}");

        // 1. 当前订阅的 steam 模组
        var subscribedMods = GetCurrentSubscribedMods();

        // 2. 本地排序与启用状态
        var modStates = await ModStateRepository.GetCurrentStatesAsync();

        // 3. 组装快照
        var snapshot = new BackupSnapshot
        {
            BackupName = backupName,
            CreatedAtUtc = DateTime.UtcNow,
            Collection = new BackupCollectionInfo
            {
                CollectionId = string.Empty,
                Name = "Mod Ark Backup Collection",
                Visibility = "private"
            },
            SubscribedMods = subscribedMods.Select(m => new SubscribedModInfo
            {
                PublishedFileId = m.PublishedFileId,
                Name = m.Name,
                DisplayName = m.DisplayName
            }).ToList(),
            ModOrder = modStates
                .OrderBy(s => s.OrderIndex)
                .Select(s => new ModOrderEntry
                {
                    Name = s.Name,
                    OrderIndex = s.OrderIndex
                })
                .ToList(),
            ModEnabledStates = modStates
                .Select(s => new ModEnabledEntry
                {
                    Name = s.Name,
                    IsEnabled = s.IsEnabled
                })
                .ToList()
        };

        // 4. TODO: 创建或更新 Steam 合集，将所有订阅模组加入其中
        // 目前仅记录合集信息到快照中，实际 WebAPI 调用需后续接入。

        // 5. 写入 JSON
        var jsonPath = Path.Combine(backupDir, "backup.json");
        var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
        await File.WriteAllTextAsync(jsonPath, json);

        PublishBackupMessage($"[ModArk] Backup snapshot written to: {jsonPath}");

        // 6. 打开备份文件夹，方便用户进一步操作
        try
        {
            Application.OpenURL("file://" + backupDir);
        }
        catch (Exception e)
        {
            var msg = "[ModArk] Failed to open backup directory in file explorer.";
            Log.Debug(e, msg);
            PublishBackupMessage(msg);
        }

        PublishBackupMessage("[ModArk] Backup completed.");
        return jsonPath;
    }

    public static async UniTask<string> RestoreAsync(bool overwrite)
    {
        var root = GetTempRoot();
        var sessionDir = Path.Combine(root, $"restore_{DateTime.UtcNow:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(sessionDir);

        PublishRestoreMessage($"[ModArk] Restore session directory: {sessionDir}");
        PublishRestoreMessage(L.Terminal.RestoreFolderCountdownLog);
        await UniTask.Delay(TimeSpan.FromSeconds(5));

        try
        {
            Application.OpenURL("file://" + sessionDir);
        }
        catch (Exception e)
        {
            var msg = "[ModArk] Failed to open restore session directory in file explorer.";
            Log.Debug(e, msg);
            PublishRestoreMessage(msg);
        }

        // 等待用户将备份文件放入该目录
        string? jsonPath = null;
        for (var i = 0; i < 60; i++)
        {
            var files = Directory.GetFiles(sessionDir, "*.json");
            if (files.Length > 0)
            {
                jsonPath = files[0];
                break;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(1));
        }

        if (jsonPath == null)
        {
            var msg = L.Terminal.RestoreTimeoutError;
            PublishRestoreMessage(msg);
            return msg;
        }

        PublishRestoreMessage($"[ModArk] Found backup file: {jsonPath}");

        // 读取并反序列化
        BackupSnapshot? snapshot;
        try
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            snapshot = JsonConvert.DeserializeObject<BackupSnapshot>(json);
        }
        catch (Exception e)
        {
            var msg = L.Terminal.RestoreReadFileError;
            Log.Error(e, "[ModArk] Failed to read or deserialize backup file.");
            PublishRestoreMessage(msg);
            return msg;
        }

        if (snapshot == null)
        {
            var msg = L.Terminal.RestoreInvalidFileError;
            PublishRestoreMessage(msg);
            return msg;
        }

        if (string.IsNullOrWhiteSpace(snapshot.BackupName) ||
            snapshot.SubscribedMods == null ||
            snapshot.ModOrder == null ||
            snapshot.ModEnabledStates == null)
        {
            var msg = L.Terminal.RestoreMissingFieldsError;
            PublishRestoreMessage(msg);
            return msg;
        }

        // 未指定 --yes 时，直接给出错误，不做任何修改
        // 生成订阅与排序/状态的动作计划
        var desiredIds = new HashSet<ulong>(snapshot.SubscribedMods.Where(m => m.PublishedFileId != 0)
            .Select(m => m.PublishedFileId));
        var currentSubscribed = GetCurrentSubscribedIds();
        var toUnsubscribe = currentSubscribed.Where(id => !desiredIds.Contains(id)).ToList();
        var toSubscribe = snapshot.SubscribedMods
            .Where(m => m.PublishedFileId != 0 && !currentSubscribed.Contains(m.PublishedFileId))
            .ToList();

        var stateByName = snapshot.ModEnabledStates
            .GroupBy(s => s.Name)
            .ToDictionary(g => g.Key, g => g.First().IsEnabled);

        var orderByName = snapshot.ModOrder
            .GroupBy(o => o.Name)
            .ToDictionary(g => g.Key, g => g.First().OrderIndex);

        var currentInfos = ModManager.modInfos.Where(info => !string.IsNullOrWhiteSpace(info.name)).ToList();
        var combinedStates = new List<ModState>();
        foreach (var info in currentInfos)
        {
            stateByName.TryGetValue(info.name, out var enabled);
            orderByName.TryGetValue(info.name, out var orderIndex);

            var state = new ModState
            {
                Name = info.name,
                PublishedFileId = info.publishedFileId,
                IsEnabled = enabled,
                OrderIndex = orderIndex
            };
            combinedStates.Add(state);
        }

        if (!overwrite)
        {
            PublishRestorePlan(toSubscribe, toUnsubscribe, combinedStates);
            return L.Terminal.RestoreNeedYesError;
        }

        // 1. 确保订阅集合完全一致
        var subscribeResults = new List<string>();
        if (!SteamManager.Initialized)
        {
            var msg = L.Terminal.RestoreSteamNotInitializedError;
            PublishRestoreMessage(msg);
            return msg;
        }

        foreach (var extraId in toUnsubscribe)
        {
            var ok = await UnsubscribeFromModAsync(extraId);
            var label = $"Unsubscribe {extraId}";
            subscribeResults.Add($"{label}: {(ok ? "OK" : "FAILED")}");
            if (ok)
            {
                PublishRestoreMessage(string.Format(L.Terminal.RestoreUnsubscribeSuccessLog, extraId));
            }
        }

        foreach (var mod in toSubscribe)
        {
            var ok = await SubscribeToModAsync(mod.PublishedFileId);
            subscribeResults.Add($"{mod.DisplayName} ({mod.PublishedFileId}): {(ok ? "OK" : "FAILED")}");
            if (ok)
            {
                PublishRestoreMessage(string.Format(L.Terminal.RestoreSubscribeSuccessLog, mod.DisplayName, mod.PublishedFileId));
            }
        }

        foreach (var mod in snapshot.SubscribedMods.Where(m => m.PublishedFileId != 0 &&
                                                               currentSubscribed.Contains(m.PublishedFileId)))
        {
            subscribeResults.Add($"{mod.DisplayName} ({mod.PublishedFileId}): already subscribed");
        }

        // 2. 应用排序与启用状态
        await ModStateRepository.ApplyStatesAsync(combinedStates, overwrite: true);

        var summaryHeader = string.Format(L.Terminal.RestoreCompletedLog, subscribeResults.Count);
        _ = CloseGameAfterDelayAsync();
        return summaryHeader;

        async UniTask CloseGameAfterDelayAsync()
        {
            PublishRestoreMessage(L.Terminal.RestoreShutdownWarningLog);
            await UniTask.Delay(TimeSpan.FromSeconds(10));
            try
            {
                PublishRestoreMessage("[ModArk] Closing game...");
                Application.Quit();
            }
            catch (Exception e)
            {
                Log.Debug(e, "[ModArk] Failed to quit automatically after restore.");
            }
        }
    }

    private static async UniTask<bool> SubscribeToModAsync(ulong publishedFileId, int maxRetry = 3)
    {
        for (var attempt = 1; attempt <= maxRetry; attempt++)
        {
            try
            {
                if (!SteamManager.Initialized)
                {
                    Log.Debug("[ModArk] Steam not initialized, cannot subscribe. Attempt {attempt}.", attempt);
                    await UniTask.Delay(500);
                    continue;
                }

                var pfid = new PublishedFileId_t(publishedFileId);
                var callDone = false;
                var callResult = default(RemoteStorageSubscribePublishedFileResult_t);
                using var handler = CallResult<RemoteStorageSubscribePublishedFileResult_t>.Create((result, failure) =>
                {
                    callDone = true;
                    callResult = result;
                });
                var hApiCall = SteamUGC.SubscribeItem(pfid);
                handler.Set(hApiCall);

                var timeout = 0f;
                while (!callDone && timeout < 10f)
                {
                    await UniTask.Delay(100);
                    timeout += 0.1f;
                }

                if (callDone && callResult.m_eResult == EResult.k_EResultOK)
                {
                    Log.Debug("[ModArk] Successfully subscribed to mod {id} on attempt {attempt}.", publishedFileId,
                        attempt);
                    return true;
                }

                Log.Debug(
                    "[ModArk] SubscribeItem failed for {id} on attempt {attempt}, result: {result}.",
                    publishedFileId, attempt, callResult.m_eResult);
            }
            catch (Exception ex)
            {
                Log.Debug(
                    "[ModArk] Exception subscribing to mod {id} on attempt {attempt}: {message}.",
                    publishedFileId, attempt, ex.Message);
            }

            await UniTask.Delay(1000);
        }

        Log.Debug("[ModArk] Failed to subscribe to mod {id} after {maxRetry} attempts.", publishedFileId, maxRetry);
        return false;
    }

    private static async UniTask<bool> UnsubscribeFromModAsync(ulong publishedFileId, int maxRetry = 3)
    {
        for (var attempt = 1; attempt <= maxRetry; attempt++)
        {
            try
            {
                if (!SteamManager.Initialized)
                {
                    PublishRestoreMessage($"[ModArk] Steam not initialized, cannot unsubscribe {publishedFileId}. Attempt {attempt}.");
                    await UniTask.Delay(500);
                    continue;
                }

                var pfid = new PublishedFileId_t(publishedFileId);
                var callDone = false;
                var callResult = default(RemoteStorageUnsubscribePublishedFileResult_t);
                using var handler =
                    CallResult<RemoteStorageUnsubscribePublishedFileResult_t>.Create((result, failure) =>
                    {
                        callDone = true;
                        callResult = result;
                    });
                var hApiCall = SteamUGC.UnsubscribeItem(pfid);
                handler.Set(hApiCall);

                var timeout = 0f;
                while (!callDone && timeout < 10f)
                {
                    await UniTask.Delay(100);
                    timeout += 0.1f;
                }

                if (callDone && callResult.m_eResult == EResult.k_EResultOK)
                {
                    return true;
                }

                PublishRestoreMessage(
                    $"[ModArk] UnsubscribeItem failed for {publishedFileId} on attempt {attempt}, result: {callResult.m_eResult}.");
            }
            catch (Exception ex)
            {
                PublishRestoreMessage(
                    $"[ModArk] Exception unsubscribing {publishedFileId} on attempt {attempt}: {ex.Message}");
            }

            await UniTask.Delay(1000);
        }

        PublishRestoreMessage($"[ModArk] Failed to unsubscribe {publishedFileId} after {maxRetry} attempts.");
        return false;
    }

    private static HashSet<ulong> GetCurrentSubscribedIds()
    {
        var result = new HashSet<ulong>();
        var numSubscribedItems = SteamUGC.GetNumSubscribedItems();
        if (numSubscribedItems <= 0)
        {
            return result;
        }

        var subscribedItems = new PublishedFileId_t[numSubscribedItems];
        SteamUGC.GetSubscribedItems(subscribedItems, numSubscribedItems);
        foreach (var item in subscribedItems)
        {
            result.Add(item.m_PublishedFileId);
        }

        return result;
    }

    private static void PublishBackupMessage(string message)
    {
        Log.Info(message);
        OnBackupMessage?.Invoke(message);
    }

    private static void PublishRestoreMessage(string message)
    {
        Log.Info(message);
        OnRestoreMessage?.Invoke(message);
    }

    private static List<SubscribedModInfo> GetCurrentSubscribedMods()
    {
        var result = new List<SubscribedModInfo>();

        var numSubscribedItems = SteamUGC.GetNumSubscribedItems();
        if (numSubscribedItems <= 0)
        {
            return result;
        }

        var subscribedItems = new PublishedFileId_t[numSubscribedItems];
        SteamUGC.GetSubscribedItems(subscribedItems, numSubscribedItems);

        // 将 game 内已识别的 ModInfo 映射到 publishedFileId
        var modInfoByPfid = new Dictionary<ulong, Duckov.Modding.ModInfo>();
        foreach (var info in ModManager.modInfos)
        {
            if (info.publishedFileId != 0 && !modInfoByPfid.ContainsKey(info.publishedFileId))
            {
                modInfoByPfid[info.publishedFileId] = info;
            }
        }

        foreach (var item in subscribedItems)
        {
            var pfid = item.m_PublishedFileId;
            var name = $"WorkshopItem_{pfid}";
            var displayName = name;

            if (modInfoByPfid.TryGetValue(pfid, out var info))
            {
                name = info.name ?? name;
                displayName = info.displayName ?? name;
            }

            result.Add(new SubscribedModInfo
            {
                PublishedFileId = pfid,
                Name = name,
                DisplayName = displayName
            });
        }

        return result;
    }

    private static void PublishRestorePlan(
        IEnumerable<SubscribedModInfo> toSubscribe,
        IEnumerable<ulong> toUnsubscribe,
        IEnumerable<ModState> combinedStates)
    {
        PublishRestoreMessage("[ModArk] Planned subscription changes:");
        foreach (var mod in toSubscribe)
        {
            PublishRestoreMessage($"  + {mod.DisplayName} ({mod.PublishedFileId})");
        }

        foreach (var id in toUnsubscribe)
        {
            PublishRestoreMessage($"  - {id}");
        }

        PublishRestoreMessage("[ModArk] Planned mod order & states:");
        foreach (var state in combinedStates.OrderBy(s => s.OrderIndex))
        {
            var status = state.IsEnabled ? "Enabled" : "Disabled";
            PublishRestoreMessage($"  {state.OrderIndex,3}: {state.Name} [{status}]");
        }
    }
}


