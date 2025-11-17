using Ducky.Sdk.ModBehaviours;

namespace Ducky.ModArk;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        Log.Info("[ModArk] Mod enabled.");
        ModArkBackupService.OnBackupMessage += async message =>
        {
            await Contract.ModTerminalClient.ShowToTerminal(message);
        };
        ModArkBackupService.OnRestoreMessage += async message =>
        {
            await Contract.ModTerminalClient.ShowToTerminal(message);
        };
        TerminalEntry.Instance.RunAsync().Forget();
    }

    protected override void ModDisabled()
    {
        Log.Info("[ModArk] Mod disabled.");
    }
}


