# Ducky.Terminals - AI Coding Assistant Instructions

## Project Overview

**Ducky.Terminals** is a terminal/command console framework for the Unity-based game "Escape from Duckov" (逃离鸭科夫). It provides a mod-to-mod communication system with a visual terminal UI that allows developers to create command-line interfaces for their game mods.

**Key Components:**
- `Ducky.TerminalUI` - The main terminal UI mod that displays console and routes commands
- `Ducky.DemoTerminalClient` - Example client mod using Ducky.Sdk for integration (recommended)
- `Ducky.DemoTerminalClientWithoutSdk` - Example client without SDK (manual integration pattern)

## Architecture & Communication Flow

### Mod Communication Protocol
The system uses a message bus pattern (`ModHttpV1`) for mod-to-mod communication:

1. **Registration**: Mods register with `ModTerminalClientContract` using unique ModIds (format: `steam.{id}` or `local.{name}`)
2. **Discovery**: TerminalUI receives `online`/`offline` commands when clients connect/disconnect
3. **Command Routing**: User types command → TerminalUI routes to selected provider ModId → provider processes and responds
4. **Response Display**: Provider sends response via `ShowToTerminal()` → displayed in TerminalUI console

**Critical Pattern**: All CLI messages use `contentType: "cli"` - other content types are ignored by terminal clients.

### Project Structure
```
Ducky.DemoTerminalClient/          # SDK-based integration (preferred)
  └─ ModBehaviour.cs               # Entry point, calls StartAsync()
  └─ ModCommandLineEntry.cs        # Command parsing with System.CommandLine
Ducky.DemoTerminalClientWithoutSdk/ # Manual integration reference
  └─ Contracts/                    # Hand-rolled protocol implementations
     └─ ModTerminalClientContract  # Terminal client wrapper
     └─ ModHttpV1ClientContract    # Low-level HTTP communication
     └─ ModHttpV1Proxy            # Reflection-based proxy (no direct dependency)
Ducky.TerminalUI/                  # Terminal UI mod
  └─ TerminalUIProtocol.cs         # Protocol handler for incoming commands
  └─ TerminalMainView.cs           # UI view (MVVM pattern with R3)
  └─ TerminalViewModel.cs          # ViewModel with reactive properties
  └─ ProviderFilterPanel.cs        # Provider selection UI
```

## Development Workflows

### Building Mods
```bash
# Build all projects
dotnet build Ducky.Terminals.slnx

# Build with SDK (auto-deploys to game folder via Ducky.Sdk targets)
dotnet build Ducky.DemoTerminalClient/Ducky.DemoTerminalClient.csproj

# Build without SDK (manual deploy via custom MSBuild targets)
dotnet build Ducky.DemoTerminalClientWithoutSdk/Ducky.DemoTerminalClientWithoutSdk.csproj
```

**Important**: Projects reference `Local.props` for environment-specific paths (SteamFolder, DuckovFolder). This file is git-ignored and must be created locally:
```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/Steam/</SteamFolder>
  </PropertyGroup>
</Project>
```

### Testing Workflow
1. Build the mod → outputs to `bin/Debug/netstandard2.1/`
2. SDK projects auto-copy to `$(DuckovFolder)Duckov_Data/Mods/$(ModName)/`
3. Launch game → mods load automatically
4. Open TerminalUI in-game (via handler view trigger)
5. Select provider (e.g., `#demo` filter) → test commands

## Project-Specific Conventions

### ModBehaviour Lifecycle
- **SDK Version**: Extend `Ducky.Sdk.ModBehaviours.ModBehaviourBase`, override `ModEnabled()`/`ModDisabled()`
- **Manual Version**: Extend `Duckov.Modding.ModBehaviour`, use `OnEnable()` method
- **Pattern**: Always call async initialization with `.Forget()` (UniTask pattern for fire-and-forget)

```csharp
// SDK Pattern
protected override void ModEnabled()
{
    ModCommandLineEntry.Instance.StartAsync().Forget();
}

// Manual Pattern (no SDK)
public void OnEnable()
{
    ModCommandLineEntry.Instance.StartAsync().Forget();
}
```

### Command Implementation Pattern (System.CommandLine)
Commands use Microsoft's `System.CommandLine` library (embedded in Ducky.Sdk):

```csharp
// 1. Create command with description
var healCommand = new Command("heal", "Heal the player");

// 2. Add optional/required arguments
var amountOption = new Option<int?>("--amount", ["a"]) 
{ 
    Description = "Amount to restore" 
};
healCommand.Add(amountOption);

// 3. Use ModAsynchronousCommandLineAction for auto-response routing
healCommand.Action = new ModAsynchronousCommandLineAction(async context =>
{
    var amount = context.GetValue(amountOption);
    return await UniTask.FromResult($"Healed {amount} HP");
});

// 4. Add to root command
rootCommand.Add(healCommand);
```

**Key Pattern**: Use `ModAsynchronousCommandLineAction` wrapper - it automatically routes return strings to TerminalUI. Avoid manual `Contract.ModTerminalClient.ShowToTerminal()` calls unless necessary.

### MVVM Pattern (TerminalUI)
TerminalUI uses **R3** (Reactive Extensions) for reactive properties:

- `ReactiveProperty<T>` - Observable value with change notifications
- `ReactiveCommand<T>` - Command pattern implementation
- `Subject<T>` - Event stream for one-way notifications
- `.AddTo(disposables)` - Automatic disposal management

**Thread Safety**: UI updates must occur on main thread. Use `PostMessage()` for cross-thread message posting:
```csharp
// Thread-safe: queues message, processes on main thread via coroutine
TerminalMainView.Instance.PostMessage("Response", MessageType.System);
```

### Provider ID Generation Logic
ProviderIds are short identifiers for mod selection (e.g., `#816` for steam mod, `#dem` for local):

1. **Steam mods**: Last 3 digits of publishedFileId (preprocessed)
2. **Local mods**: First 3 chars of mod name (preprocessed)
3. **Preprocessing**: Strip non-alphanumeric, lowercase
4. **Conflict Resolution**: Incrementally increase length (4, 5, 6...) until unique

Users filter with `#` prefix: typing `#816` filters to matching providers.

### ModArk Backup/Restore Flow
`Ducky.ModArk` (in `Ducky.ModArk/`) extends the terminal ecosystem with a stateful backup tool:

- **Command surface**: `TerminalEntry.cs` registers `backup <name>` and `restore [--yes|-y]` via `System.CommandLine`. Each action is wrapped in `ModAsynchronousCommandLineAction`, so return strings are automatically routed back to the terminal.
- **Snapshot creation**: `ModArkBackupService.BackupAsync()` gathers Steam Workshop subscriptions (`SteamUGC`), local ordering/enabled flags from `ModStateRepository.GetCurrentStatesAsync()`, and serializes a `BackupSnapshot` model (`BackupModels.cs`) to `<Temp>/DuckyModArk/backup_*.json`. The service publishes progress lines through `OnBackupMessage`, and `ModBehaviour` wires those events to `Contract.ModTerminalClient.ShowToTerminal()`.
- **Restore safeguards**: `RestoreAsync()` waits for the player to drop a JSON file into a temp folder opened via `Application.OpenURL`. Without `--yes`, it only prints a diff plan (`PublishRestorePlan`) so players can review required subscribe/unsubscribe operations. When confirmed, it synchronizes subscriptions (retrying Steam API calls), reapplies ordering/enabled state through `ModStateRepository.ApplyStatesAsync(overwrite: true)`, then warns that the game will close shortly.
- **Localization**: All user-facing strings come from `L.Terminal.*`, backed by keys in `Ducky.ModArk/LK.cs` and CSV values under `Ducky.ModArk/assets/Locales/`. When adding new prompts or error messages, update the keys first so auto-generated translations stay aligned.

## Integration Points

### Adding a New Terminal Client Mod
1. **Reference Ducky.Sdk** (recommended) or manually implement contracts
2. **Create ModBehaviour** with `StartAsync()` initialization
3. **Connect to terminal**:
   ```csharp
   var client = Contract.ModTerminalClient; // or GetOrCreate(ModId)
   await client.Connect(async (isFromTerminal, fromModId, message, respond) => {
       // Parse message, execute command
       await respond(resultString);
   });
   ```
4. **Register commands** using `System.CommandLine.Command` hierarchy
5. **Deploy** mod to `Duckov_Data/Mods/{ModName}/` with `assets/info.ini`

### External Dependencies
- **UniTask**: Used for async/await (Unity-compatible)
- **Newtonsoft.Json**: JSON serialization (for multiline content in `show` command)
- **R3** (TerminalUI only): Reactive Extensions for MVVM
- **ObservableCollections.R3** (TerminalUI only): Observable collections
- **TMPro**: TextMeshPro for UI text rendering

### Game-Specific Integration
Projects reference Unity/game assemblies from `$(ManagedDirectory)`:
- `TeamSoda*.dll` - Core game assemblies
- `Unity*.dll` - Unity engine
- `ItemStatsSystem.dll`, `Newtonsoft.Json.dll`, `UniTask.dll`

**Without SDK**: Manually reference DLLs with wildcards:
```xml
<Reference Include="$(ManagedDirectory)TeamSoda*.dll" />
```

## Localization & Multi-Language Support

### Localization System
The project uses **Ducky.Sdk's built-in localization system** with CSV-based translation files.

**Supported Languages** (10 total):
- `en` - English
- `zh` - Chinese Simplified (简体中文)
- `zh-hant` - Chinese Traditional (繁體中文)
- `ja` - Japanese (日本語)
- `ko` - Korean (한국어)
- `fr` - French (Français)
- `de` - German (Deutsch)
- `es` - Spanish (Español)
- `ru` - Russian (Русский)
- `pt` - Portuguese (Português)

### File Structure
```
Ducky.TerminalUI/
  └─ assets/
     └─ Locales/
        ├─ en.csv
        ├─ zh.csv
        ├─ zh-hant.csv
        ├─ ja.csv
        ├─ ko.csv
        ├─ fr.csv
        ├─ de.csv
        ├─ es.csv
        ├─ ru.csv
        └─ pt.csv
  └─ LK.cs  # Language Key definitions
```

### How to Use Localization

#### 1. Define Language Keys in `LK.cs`
```csharp
using Ducky.Sdk.Attributes;

[LanguageSupport("en", "zh-Hant", "zh", "fr", "de", "es", "ru", "ja", "ko", "pt")]
public static class LK
{
    public static class UI
    {
        public const string InputPlaceholder = "terminal_input_placeholder";
        public const string TerminalTitle = "terminal_title";
    }
}
```

- Use `[LanguageSupport]` attribute to declare supported languages
- Organize keys in nested static classes (e.g., `LK.UI`, `LK.Commands`, etc.)
- Keys are `const string` with snake_case naming

#### 2. Build and Auto-Generate CSV Files
When you build the project, Ducky.Sdk automatically:
- Generates CSV files for all supported languages in `assets/Locales/`
- Creates missing keys in existing CSV files
- Preserves existing translations

**Example auto-generated CSV (`zh.csv`):**
```csv
Key,Value
terminal_input_placeholder,"请输入 # 可以快速搜索mod"
terminal_title,"终端"
```

**Important**: 
- CSV files are auto-generated - edit `LK.cs` only to add new keys
- After build, manually fill in translations in the generated CSV files
- Always quote values containing special characters or commas
- Empty values will fall back to the key name

#### 3. Fill in Translations
After the first build generates CSV files with empty values:
1. Open each language's CSV file (e.g., `zh.csv`, `en.csv`, etc.)
2. Fill in the translated values for each key
3. Rebuild the project

#### 4. Use Translations in Code
The Ducky.Sdk build system auto-generates the `L` class from `LK` definitions:

```csharp
// In TerminalMainView.cs
titleText.text = L.UI.TerminalTitle;          // Returns translated text based on game language
placeholder.text = L.UI.InputPlaceholder;      // Automatically selects correct CSV file
```

**Pattern**: `L.{Category}.{KeyName}` → returns translated string for current game language

#### 5. Build Process Summary
1. Define keys in `LK.cs` with `[LanguageSupport]` attribute
2. Build project → Ducky.Sdk auto-generates CSV files with empty values
3. Manually fill in translations in all CSV files under `assets/Locales/`
4. Rebuild → `L` class is generated with all translations
5. Use `L.UI.xxx` in code to access translations

### Current Translations
| Key | English | 中文 (简体) |
|-----|---------|------------|
| `terminal_title` | Terminal | 终端 |
| `terminal_input_placeholder` | Enter # to quickly search mods | 请输入 # 可以快速搜索mod |

### Adding New Translations
1. Add constant to `LK.cs` (e.g., `public const string NewKey = "new_key";`)
2. Build project → CSV files are auto-generated/updated with the new key (empty values)
3. Manually fill in translations for the new key in all 10 CSV files
4. Rebuild project to regenerate `L` class with new translations
5. Use `L.UI.NewKey` in code

## Common Patterns & Anti-Patterns

### ✅ Correct Patterns
- Use `ModAsynchronousCommandLineAction` for command handlers (auto-routes responses)
- Call `.Forget()` on fire-and-forget UniTasks
- Use singleton pattern for protocol managers (`GetOrCreate(modId)`)
- Normalize line endings: `.Replace("\r\n", "\n").Replace("\r", "\n")`
- JSON-serialize multiline strings for `show` command: `TerminalUICommand.Show(content)`
- Use `L.{Category}.{Key}` for all user-facing text (enables multi-language support)
- Keep CSV files in sync - all languages should have the same keys

### ❌ Anti-Patterns
- Don't block UI thread with synchronous I/O
- Avoid calling `ShowToTerminal()` in tight loops (queue messages instead)
- Don't parse commands manually - use `System.CommandLine.Parsing.CommandLineParser`
- Don't create multiple instances of singleton contracts (use `GetOrCreate()`)
- Don't use `SetActive()` on ViewModel-controlled UI - use ReactiveProperty bindings
- Don't hardcode user-facing strings - use localization keys instead
- Don't forget to update all 10 CSV files when adding new translation keys

## Debugging Tips
- Enable Ducky.Sdk logging via `Log.Info()` - outputs to game console
- Check `ModHttpV1` GameObject exists in Unity scene (ModHttpV1Proxy dependency)
- Verify ModId format: `steam.{publishedFileId}` or `local.{modName}`
- Test `ping`/`pong` command first to verify connection
- Use `/? ` or `help` commands to test System.CommandLine parsing

## Target Framework & Build Environment
- **Target**: .NET Standard 2.1 (Unity compatibility)
- **Language**: C# with preview features, nullable enabled
- **Build Tool**: .NET SDK (not full .NET Framework)
- **Solution Format**: `.slnx` (new VS solution format)
- **Output**: Mod DLLs deployed to game's `Duckov_Data/Mods/` folder

## Naming Conventions
- **ModId**: Unique identifier (e.g., `steam.3606789816`, `local.Ducky.TerminalUI`)
- **ProviderId**: Short display ID for terminal selection (e.g., `816`, `dem`)
- **ContentType**: Fixed as `"cli"` for terminal commands
- **Commands**: Lowercase verbs (e.g., `heal`, `time`, `god`, `online`, `offline`, `show`)
