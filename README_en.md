# Ducky.Terminals

English | [简体中文](./README.md)

## 📖 Project Overview

**Ducky.Terminals** is a terminal console framework developed for the Unity-based game "Escape from Duckov" (逃离鸭科夫). It provides a visual terminal UI and an inter-mod communication system that allows mod developers to create command-line interfaces for their mods.

### Core Features

- 🎮 **In-Game Terminal UI** - Visual command-line interface with slide-in/out animation
- 🔌 **Mod Communication System** - Message bus-based inter-mod communication protocol
- 🛠️ **Command Parsing Framework** - Integrated `System.CommandLine` library with support for arguments, options, and standard CLI features
- 🎯 **Provider Filter System** - Quick mod selection using `#` prefix
- 📦 **Two Integration Methods** - Support for both Ducky.Sdk and manual integration

### Project Components

| Component | Description | Steam Workshop |
|-----------|-------------|----------------|
| **Ducky.TerminalUI** | Main terminal UI program, handles display and command routing | [3606793704](https://steamcommunity.com/sharedfiles/filedetails/?id=3606793704) |
| **Ducky.DemoTerminalClient** | Example client using Ducky.Sdk (recommended) | [3606789816](https://steamcommunity.com/sharedfiles/filedetails/?id=3606789816) |
| **Ducky.DemoTerminalClientWithoutSdk** | Manual integration example without SDK | [3606789962](https://steamcommunity.com/sharedfiles/filedetails/?id=3606789962) |

## 🚀 Quick Start

### Requirements

- .NET SDK 8.0+
- Unity game "Escape from Duckov" installed
- Visual Studio 2022 / Rider / VS Code

### Building the Project

1. **Clone the Repository**
```bash
git clone https://github.com/ducky7go/Ducky.Terminals.git
cd Ducky.Terminals
```

2. **Configure Local Environment**

Create a `Local.props` file (git-ignored):
```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/Steam/</SteamFolder>
  </PropertyGroup>
</Project>
```

3. **Build All Projects**
```bash
dotnet build Ducky.Terminals.slnx
```

Build artifacts will be automatically deployed to the game's `Duckov_Data/Mods/` folder.

### In-Game Usage

1. Launch the game, mods will load automatically
2. Trigger the terminal UI (left edge area of game screen)
3. Type `#` to view available mod providers
4. Select a provider and enter commands, e.g., `time`, `heal --amount 50`

## 🔌 Mod Developer Integration Guide

### Method 1: Using Ducky.Sdk (Recommended)

#### 1. Create Project and Reference SDK

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.1</TargetFramework>
    <ModName>YourModName</ModName>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Ducky.Sdk" Version="0.1.5-dev.3">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

#### 2. Create ModBehaviour

```csharp
using Cysharp.Threading.Tasks;
using Ducky.Sdk.ModBehaviours;

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
```

#### 3. Implement Command Parsing

```csharp
using System.CommandLine;
using Cysharp.Threading.Tasks;
using Ducky.Sdk.Contracts;
using Ducky.Sdk.Contracts.CommandLine;

public class ModCommandLineEntry
{
    public static ModCommandLineEntry Instance { get; } = new();

    public async UniTask StartAsync()
    {
        var command = CreateModRootCommand();
        var client = Contract.ModTerminalClient;
        
        await client.Connect(async (fromTerminal, fromModId, message, respond) =>
        {
            var parseResult = CommandLineParser.Parse(command, message);
            await parseResult.InvokeAsync();
        });
    }

    private ModRootCommand CreateModRootCommand()
    {
        var rootCommand = new ModRootCommand("Your Mod Description");

        // Example: Simple command
        var pingCommand = new Command("ping", "Test connection");
        pingCommand.Action = new ModAsynchronousCommandLineAction(async context =>
        {
            return await UniTask.FromResult("pong");
        });
        rootCommand.Add(pingCommand);

        // Example: Command with arguments
        var healCommand = new Command("heal", "Heal the player");
        var amountOption = new Option<int?>("--amount", ["a"]) 
        { 
            Description = "Amount to restore" 
        };
        healCommand.Add(amountOption);
        healCommand.Action = new ModAsynchronousCommandLineAction(async context =>
        {
            var amount = context.GetValue(amountOption) ?? 100;
            return await UniTask.FromResult($"Healed {amount} HP");
        });
        rootCommand.Add(healCommand);

        return rootCommand;
    }
}
```

### Method 2: Manual Integration (Without SDK)

Refer to the `Ducky.DemoTerminalClientWithoutSdk` project:

1. Implement `ModHttpV1Proxy` - Access game's ModHttpV1 component via reflection
2. Implement `ModHttpV1ClientContract` - Encapsulate low-level communication
3. Implement `ModTerminalClientContract` - Encapsulate terminal protocol
4. Manually reference game DLL files

For detailed example code, check the [Ducky.DemoTerminalClientWithoutSdk](./Ducky.DemoTerminalClientWithoutSdk) folder.

## 📚 Key Concepts

### ModId Format

- **Steam Mod**: `steam.{publishedFileId}` (e.g., `steam.3606789816`)
- **Local Mod**: `local.{modName}` (e.g., `local.Ducky.TerminalUI`)

### ProviderId

ProviderId is a short identifier for terminal selection:

- **Steam Mod**: Last 3 digits of publishedFileId (preprocessed)
- **Local Mod**: First 3 characters of mod name (preprocessed)
- **Preprocessing Rules**: Strip non-alphanumeric characters, lowercase
- **Conflict Resolution**: Automatically increase length (4, 5, 6... digits) until unique

Users filter using `#` prefix, e.g., typing `#816` filters to matching providers.

### Communication Protocol

All terminal commands must use `contentType: "cli"`:

```csharp
// Send command
await client.SendTo(targetModId, "cli", "your command");

// Respond to terminal
await client.ShowToTerminal("response message");
```

### Command Implementation Best Practices

✅ **Recommended Practices:**
- Use `ModAsynchronousCommandLineAction` wrapper for command handlers (auto-routes responses)
- Use `.Forget()` for fire-and-forget UniTask scenarios
- Use singleton pattern for protocol managers (`GetOrCreate(modId)`)
- Normalize line endings: `.Replace("\r\n", "\n").Replace("\r", "\n")`
- JSON-serialize multiline content: `TerminalUICommand.Show(JsonConvert.SerializeObject(content))`

❌ **Avoid:**
- Don't perform synchronous I/O on UI thread
- Avoid calling `ShowToTerminal()` in tight loops (batch messages instead)
- Don't parse commands manually - use `System.CommandLine` parser
- Don't create multiple singleton instances (use `GetOrCreate()`)

## 🏗️ Architecture

### Communication Flow

```
User Input Command
    ↓
TerminalUI Receives (TerminalViewModel)
    ↓
Send to Target ModId via ModHttpV1 (TerminalUIProtocol)
    ↓
Target Mod Receives Message (ModTerminalClientContract)
    ↓
System.CommandLine Parses Command (ModCommandLineEntry)
    ↓
Execute Command Logic
    ↓
Return Response (ModAsynchronousCommandLineAction)
    ↓
TerminalUI Displays Result (TerminalMainView)
```

### MVVM Pattern (TerminalUI)

TerminalUI uses **R3** (Reactive Extensions) for reactive programming:

- `ReactiveProperty<T>` - Observable property with change notifications
- `ReactiveCommand<T>` - Command pattern implementation
- `Subject<T>` - One-way event stream
- `.AddTo(disposables)` - Automatic lifecycle management

**Thread Safety**: UI updates must occur on main thread. Use `PostMessage()` for cross-thread messaging:
```csharp
// Thread-safe: queues message, processes on main thread via coroutine
TerminalMainView.Instance.PostMessage("Response", MessageType.System);
```

## 🐛 Debugging Tips

- Use `Log.Info()` for logging (Ducky.Sdk)
- Check if `ModHttpV1` GameObject exists in Unity scene
- Verify ModId format: `steam.{id}` or `local.{name}`
- Test `ping`/`pong` command first to verify connection
- Use `/?` or `help` to test command parsing

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

## 🤝 Community Support

- **QQ Group**: 755123281
- **Group Link**: [Escape from Duckov Mod Technical Discussion](https://qm.qq.com/q/TjBZSgMOqK)
- **GitHub Issues**: [Submit Issues](https://github.com/ducky7go/Ducky.Terminals/issues)

## 🙏 Acknowledgments

- "Escape from Duckov" game development team
- Ducky.Sdk developers
- R3 (Reactive Extensions)
- System.CommandLine

## 📦 Related Links

- [Ducky.Sdk NuGet Package](https://www.nuget.org/packages/Ducky.Sdk)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [UniTask](https://github.com/Cysharp/UniTask)
- [R3 (Reactive Extensions)](https://github.com/Cysharp/R3)
