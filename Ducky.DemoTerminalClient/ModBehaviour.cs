using Cysharp.Threading.Tasks;
using Ducky.Sdk.ModBehaviours;

namespace Ducky.DemoTerminalClient;

public class ModBehaviour : ModBehaviourBase
{
    protected override void ModEnabled()
    {
        ModCommandLineEntry.Instance.StartAsync().Forget();
    }

    protected override void ModDisabled()
    {
    }
}
