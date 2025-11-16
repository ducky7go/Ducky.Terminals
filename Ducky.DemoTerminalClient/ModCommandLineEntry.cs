using System.CommandLine;
using System.CommandLine.Parsing;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.CommandLine;
using Ducky.Sdk.Logging;

namespace Ducky.DemoTerminalClient;

public class ModCommandLineEntry
{
    public static ModCommandLineEntry Instance { get; } = new();

    public async UniTask StartAsync()
    {
        var command = CreateModRootCommand();
        var client = Contract.ModTerminalClient;
        await client.Connect(async (terminal, id, message, toTerminal) =>
        {
            Log.Info($"Received message from {id}: {message}");

            // 这是一个简单的测试，当控制台发送 "ping" 时，回复 "pong"
            // 这也是对接的一种最简单方式，如果不需要复杂的命令行解析，可以直接使用这种方式
            if (message == "ping")
            {
                await toTerminal("pong");
            }
            else
            {
                // 下面是使用命令行解析器处理更复杂的命令
                // 这是推荐的做法，因为我们内部完成了一系列必要的集成
                // 你可以使用 Sdk 中已经内置的 System.CommandLine 解析器
                // https://learn.microsoft.com/en-us/dotnet/standard/commandline/
                // 注意 Sdk 已经内置了 System.CommandLine 的源代码，所以你不需要额外引用该包
                var parseResult = CommandLineParser.Parse(command, message);
                await parseResult.InvokeAsync();
            }
        });
    }

    private ModRootCommand CreateModRootCommand()
    {
        var rootCommand = new ModRootCommand("Ducky Demo Terminal Client");

        {
            // 添加子命令 time
            var timeCommand = new Command("time", "Get the current system time")
            {
                // 注意使用 Sdk 内置好的 ModAsynchronousCommandLineAction
                // 这样可以在完成时自动会您将内容返回给 TerminalUI
                Action = new ModAsynchronousCommandLineAction(ShowTime)
            };
            rootCommand.Add(timeCommand);

            UniTask<string> ShowTime(ParseResult context)
            {
                var currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                return UniTask.FromResult($"Current system time: {currentTime}");
            }
        }
        {
            // 添加子命令 date
            var dateCommand = new Command("date", "Get the current system date");
            dateCommand.SetAction(ShowDate);
            rootCommand.Add(dateCommand);

            async Task<int> ShowDate(ParseResult p)
            {
                // 虽然这不是我们推荐的做法，但你也可以直接使用 Contract 来发送消息
                var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                await Contract.ModTerminalClient.ShowToTerminal($"Current system date: {currentDate}");
                return 0;
            }
        }
        {
            // 添加回血命令，并且允许传递可选的生命值参数，如果没有指定，就回满
            // 这是一个关于游戏内操作的选项
            // example: heal --amount 50
            // Option 也支持简写，例如 -a 50
            // Option 是一种可选参数
            var healCommand = new Command("heal", "Heal the player");
            var amountOption = new Option<int?>(
                "--amount",
                ["a"])
            {
                Description = "Amount of health to restore (default: full health)"
            };
            healCommand.Add(amountOption);
            healCommand.Action = new ModAsynchronousCommandLineAction(x => UniTask.FromResult(Heal(x)));
            rootCommand.Add(healCommand);

            string Heal(ParseResult context)
            {
                var main = LevelManager.Instance.MainCharacter;
                if (main == null)
                {
                    return "No main character found to heal.";
                }

                var health = main.Health;
                if (health == null)
                {
                    return "Main character has no health component.";
                }

                var emptyHealth = health.MaxHealth - health.CurrentHealth;
                var valueToHeal = Math.Min(context.GetValue(amountOption) ?? emptyHealth, emptyHealth);
                health.CurrentHealth += valueToHeal;
                return
                    $" Healed {valueToHeal} health points. Current health: {health.CurrentHealth}/{health.MaxHealth}";
            }
        }
        {
            // 设置玩家是否无敌
            // example: god true
            // Argument 是一种必选参数
            var godModeCommand = new Command("god", "Set god mode (invincibility) for the player");
            Argument<bool> enableArgs = new("enabled")
            {
                Description = "Set to true to enable god mode, false to disable"
            };
            godModeCommand.Add(enableArgs);
            godModeCommand.Action = new ModAsynchronousCommandLineAction(x => UniTask.FromResult(SetGodMode(x)));
            rootCommand.Add(godModeCommand);

            string SetGodMode(ParseResult context)
            {
                var main = LevelManager.Instance.MainCharacter;
                if (main == null)
                {
                    return "No main character found to set god mode.";
                }

                var health = main.Health;
                if (health == null)
                {
                    return "Main character has no health component.";
                }

                var enable = context.GetValue(enableArgs);
                // 设置无敌状态
                // 这里使用 SDK 已经编写好的方法进行设置
                health.SetInvincible(enable);
                return enable ? "God mode enabled." : "God mode disabled.";
            }
        }

        return rootCommand;
    }
}
