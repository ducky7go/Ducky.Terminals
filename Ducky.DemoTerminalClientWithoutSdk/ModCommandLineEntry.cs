using Cysharp.Threading.Tasks;
using Ducky.DemoTerminalClientWithoutSdk.Contracts;
using UnityEngine;

namespace Ducky.DemoTerminalClientWithoutSdk;

public class ModCommandLineEntry
{
    public static ModCommandLineEntry Instance { get; } = new();

    public async UniTask StartAsync()
    {
        var client = ModTerminalClientContract.GetOrCreate(Helper.GetModId());
        await client.Connect(async (terminal, id, message, toTerminal) =>
        {
            Debug.Log($"Received message from {id}: {message}");

            // 这是一个简单的测试，当控制台发送 "ping" 时，回复 "pong"
            // 这也是对接的一种最简单方式，如果不需要复杂的命令行解析，可以直接使用这种方式
            if (message == "ping")
            {
                await toTerminal("pong");
            }
        });
    }
}
