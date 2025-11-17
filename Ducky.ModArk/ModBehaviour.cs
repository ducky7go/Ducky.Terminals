using Ducky.Sdk.ModBehaviours;

namespace Ducky.ModArk;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        Log.Info("[ModArk] Mod enabled.");
        ModArkBackupService.OnBackupMessage += ShowToTerminal;
        ModArkBackupService.OnRestoreMessage += ShowToTerminal;
        TerminalEntry.Instance.RunAsync().Forget();
    }

    private void ShowToTerminal(string message)
    {
        Contract.ModTerminalClient.ShowToTerminal(message).Forget();
    }

    protected override void ModDisabled()
    {
        Log.Info("[ModArk] Mod disabled.");
        ModArkBackupService.OnBackupMessage -= ShowToTerminal;
        ModArkBackupService.OnRestoreMessage -= ShowToTerminal;
    }
}
