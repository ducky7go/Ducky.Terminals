using System.CommandLine;
using System.CommandLine.Parsing;
using Ducky.Sdk.Contracts.CommandLine;

namespace Ducky.ModArk;

internal static class ParseResultExtensions
{
    /// <summary>
    /// 检查用户是否明确提供了指定的 Option
    /// </summary>
    public static bool IsOptionProvided(this ParseResult parseResult, Option option)
    {
        var aliases = option.Aliases;
        return parseResult.Tokens.Any(t => aliases.Contains(t.Value));
    }
}

public class TerminalEntry
{
    public static TerminalEntry Instance { get; } = new();

    public async UniTask RunAsync()
    {
        try
        {
            var command = CreateCommand();
            await Contract.ModTerminalClient.Connect(async (terminal, id, body, toTerminal) =>
            {
                var parseResult = CommandLineParser.Parse(command, body);
                await parseResult.InvokeAsync();
            });
        }
        catch (Exception e)
        {
            Log.Error(e, "[ModArk] Failed to connect to ModTerminalClient");
        }
    }

    private ModRootCommand CreateCommand()
    {
        var rootCommand = new ModRootCommand(L.Terminal.TerminalDescription);

        // backup 命令
        {
            var backupCommand = new Command("backup", L.Terminal.BackupCommandDescription);
            var nameArgument = new Argument<string>("name")
            {
                Description = L.Terminal.BackupCommandNameArgumentDescription
            };
            backupCommand.Add(nameArgument);

            backupCommand.Action = new ModAsynchronousCommandLineAction(BackupAsync);
            rootCommand.Add(backupCommand);

            async UniTask<string> BackupAsync(ParseResult result)
            {
                var name = result.GetValue(nameArgument);
                if (string.IsNullOrWhiteSpace(name))
                {
                    var msg = L.Terminal.BackupNameRequiredError;
                    Log.Info(msg);
                    return msg;
                }

                try
                {
                    var snapshotPath = await ModArkBackupService.BackupAsync(name);
                    var msg = string.Format(L.Terminal.BackupCompletedLog, snapshotPath);
                    Log.Info(msg);
                    return msg;
                }
                catch (Exception e)
                {
                    Log.Error(e, "[ModArk] Backup failed.");
                    return string.Format(L.Terminal.BackupFailedLog, e.Message);
                }
            }
        }

        // restore 命令
        {
            var yesOption = new Option<bool>("--yes", "-y")
            {
                Description = L.Terminal.RestoreYesOptionDescription
            };
            var restoreCommand = new Command("restore", L.Terminal.RestoreCommandDescription);
            restoreCommand.Add(yesOption);

            restoreCommand.Action = new ModAsynchronousCommandLineAction(RestoreAsync);
            rootCommand.Add(restoreCommand);

            async UniTask<string> RestoreAsync(ParseResult result)
            {
                var overwrite = result.GetValue(yesOption);
                try
                {
                    var message = await ModArkBackupService.RestoreAsync(overwrite);
                    Log.Info(message);
                    return message;
                }
                catch (Exception e)
                {
                    Log.Error(e, "[ModArk] Restore failed.");
                    return string.Format(L.Terminal.RestoreFailedLog, e.Message);
                }
            }
        }

        return rootCommand;
    }
}


