## OpenSpec

This repo has no `openspec/` directory. OpenSpec for all `ducky7go-mono` sub-repos lives in the parent repo: `../../openspec/` (workflow rules `../../openspec/AGENTS.md`, specs `../../openspec/specs/`, changes `../../openspec/changes/`).


# Ducky.Terminals - AI Coding Assistant Instructions

## Project Overview

**Ducky.Terminals** is a terminal/command console framework for the Unity-based game "Escape from Duckov" (逃离鸭科夫). It provides a mod-to-mod communication protocol that lets developers expose command-line interfaces from their game mods to a terminal host. The visual terminal UI mod (`Ducky.TerminalUI`) has been removed from this repo, so a compatible terminal host must be installed separately.

**Key Components:** none at present. The example client projects have been removed, and `Ducky.Terminals.slnx` lists no projects. A project added later targets `netstandard2.1`, is listed in `Ducky.Terminals.slnx` and is deployed to the game as a mod; an SDK-based client consumes the `Ducky.Sdk` NuGet package. The repo has no test projects and no `.github/` workflows.

**Repo-level build facts (verified):**
- `Directory.Packages.props` centrally pins `Ducky.Sdk` (currently `0.1.7-dev.2`, a pre-release); csproj files use `PackageReference` without versions. `nuget.config` maps `Ducky.Sdk` to the MyGet feed `https://www.myget.org/F/ducky/api/v3/index.json` and everything else to nuget.org (package source mapping), so pre-release SDK builds restore from MyGet.
- `Directory.Build.props` sets `Nullable=enable`, `LangVersion=preview`, `ImplicitUsings=true` for every project.
- `global.json` pins .NET SDK `10.0.0` with `rollForward: latestMajor` and `allowPrerelease: true`.
- `.editorconfig` and `Ducky.Terminals.sln.DotSettings` (ReSharper/Rider) are present.
- Each SDK-based mod keeps `assets/info.ini`, `assets/title/*.md`, `assets/description/*.md`.

## Architecture & Communication Flow

### Mod Communication Protocol
The system uses a message bus pattern (`ModHttpV1`) for mod-to-mod communication:

1. **Registration**: Mods register with `ModTerminalClientContract` using unique ModIds (format: `steam.{id}` or `local.{name}`)
2. **Discovery**: the terminal host receives `online`/`offline` commands when clients connect/disconnect
3. **Command Routing**: User types command → the terminal host routes to selected provider ModId → provider processes and responds
4. **Response Display**: Provider sends response via `ShowToTerminal()` → displayed in the terminal host console

**Critical Pattern**: All CLI messages use `contentType: "cli"` - other content types are ignored by terminal clients.

## Development Workflows

### Building Mods
```bash
# The solution lists no projects yet: add a project to Ducky.Terminals.slnx before building
dotnet build Ducky.Terminals.slnx
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
4. Open the terminal in-game via the terminal host mod
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

**Key Pattern**: Use `ModAsynchronousCommandLineAction` wrapper - it automatically routes return strings to the terminal host. Avoid manual `Contract.ModTerminalClient.ShowToTerminal()` calls unless necessary.

### Provider ID Generation Logic
ProviderIds are short identifiers for mod selection (e.g., `#816` for steam mod, `#dem` for local):

1. **Steam mods**: Last 3 digits of publishedFileId (preprocessed)
2. **Local mods**: First 3 chars of mod name (preprocessed)
3. **Preprocessing**: Strip non-alphanumeric, lowercase
4. **Conflict Resolution**: Incrementally increase length (4, 5, 6...) until unique

Users filter with `#` prefix: typing `#816` filters to matching providers.

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

Uses Ducky.Sdk's built-in CSV localization. A mod's `LK.cs` declares keys as `const string` (snake_case) in nested static class `LK.Terminal` under `[LanguageSupport("en", "zh-hant", "zh", "fr", "de", "es", "ru", "ja", "ko", "pt")]`; the SDK generates the `L.{Category}.{Key}` accessors that return text for the current game language.

Adding a key:
1. Add the `const string` to `LK.cs` (do not hand-edit `lkeys.json` / `keys.hash.txt`).
2. Build: the SDK creates or updates `assets/Locales/{lang}.csv` for all 10 languages (new keys get empty values, existing translations are preserved).
3. Fill in the translation in every CSV (`Key,Value`; quote values containing special characters or commas; an empty value falls back to the key name).
4. Rebuild and use `L.{Category}.{Key}` in code.

## Common Patterns & Anti-Patterns

### ✅ Correct Patterns
- Use singleton pattern for protocol managers (`GetOrCreate(modId)`)
- Normalize line endings: `.Replace("\r\n", "\n").Replace("\r", "\n")`
- JSON-serialize multiline strings for `show` command: `TerminalUICommand.Show(content)`

### ❌ Anti-Patterns
- Don't block UI thread with synchronous I/O
- Avoid calling `ShowToTerminal()` in tight loops (queue messages instead)
- Don't parse commands manually - use `System.CommandLine.Parsing.CommandLineParser`
- Don't create multiple instances of singleton contracts (use `GetOrCreate()`)
- Don't hardcode user-facing strings - use localization keys instead
- Don't forget to update all 10 CSV files when adding new translation keys

## Debugging Tips
- Enable Ducky.Sdk logging via `Log.Info()` - outputs to game console
- Check `ModHttpV1` GameObject exists in Unity scene (ModHttpV1Proxy dependency)
- Verify ModId format: `steam.{publishedFileId}` or `local.{modName}`
- Test `ping`/`pong` command first to verify connection
- Use `/? ` or `help` commands to test System.CommandLine parsing

## Naming Conventions
- **ModId**: Unique identifier (e.g., `steam.1234567890`, `local.MyMod`)
- **ProviderId**: Short display ID for terminal selection (e.g., `816`, `dem`)
- **ContentType**: Fixed as `"cli"` for terminal commands
- **Commands**: Lowercase verbs (e.g., `heal`, `time`, `god`, `online`, `offline`, `show`)
