using Cysharp.Threading.Tasks;

namespace Ducky.DemoTerminalClientWithoutSdk;

public class ModBehaviour : Duckov.Modding.ModBehaviour
{
    public void OnEnable()
    {
        ModCommandLineEntry.Instance.StartAsync().Forget();
    }
}
