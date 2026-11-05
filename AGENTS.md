# Ducky.Terminals - AI Coding Assistant Instructions

## Project Overview

**Ducky.Terminals** is a command-console protocol for the Unity game "Escape from Duckov" (逃离鸭科夫). Mods expose commands through `ModTerminalClientContract`; the SDK's `ModHttpV1` message hub carries them to a terminal host mod, which routes user input to the selected mod and displays the replies. The terminal UI mod `Ducky.TerminalUI` has been removed from this repo, so a compatible host must be installed separately. Host-side behavior is not in this repo.

**Current state (verified):**
- `Ducky.Terminals.slnx` lists no projects; the example clients and the terminal UI were removed. Nothing builds until a project is added.
- No test projects and no `.github/` workflows.
- A new project should target `netstandard2.1`, be listed in `Ducky.Terminals.slnx`, and consume the `Ducky.Sdk` NuGet package.

## Build Settings

- `Directory.Packages.props` pins `Ducky.Sdk` centrally at `0.1.7-dev.2` (pre-release). Project files use `PackageReference` without versions.
- `nuget.config` maps `Ducky.Sdk` to the MyGet feed `https://www.myget.org/F/ducky/api/v3/index.json` and all other packages to nuget.org, so pre-release SDK builds restore from MyGet.
- `Directory.Build.props` sets `Nullable=enable`, `LangVersion=preview`, `ImplicitUsings=true` and `ManagePackageVersionsCentrally=true`.
- `global.json` pins the .NET SDK to `10.0.0` with `rollForward: latestMajor` and `allowPrerelease: true`.
- `Local.props` is git-ignored and holds machine-specific paths (`SteamFolder`, `DuckovFolder`). No current file imports it; a project that needs game paths must import it and create it locally:
  ```xml
  <Project>
    <PropertyGroup>
      <SteamFolder>/path/to/Steam/</SteamFolder>
    </PropertyGroup>
  </Project>
  ```
- Each SDK-based mod deploys to `Duckov_Data/Mods/<ModName>/` (the SDK's deploy step).

## Protocol (verified in `Ducky.Sdk`, `Sdk/SDKlibs/Ducky.Sdk.Lib/Contracts/`)

- `ModTerminalClientContract.GetOrCreate(modId)` returns a per-mod singleton; `Contract.ModTerminalClient` resolves it for the current mod. `Connect(handler)` registers the input handler; `ShowToTerminal(body)` sends output to the host.
- The host mod id is `ModTerminalClientContract.TerminalUIModId` = `"TerminalUIMod"`. Commands use content type `ModTerminalClientContract.ContentTypeCli` = `"cli"`.
- Clients send `TerminalUICommand.Offline` to the host on disconnect.
- `ModAsynchronousCommandLineAction` returns its result to the host automatically; call `ShowToTerminal()` by hand only when needed.
- ModId format (README_en): `steam.{publishedFileId}` or `local.{modName}`.
- The SDK hub is a `MonoBehaviour` named `ModHttpV1` (`ModHttpV1.HubGameObjectName`). `MessageHubManager` finds it by `GetType().Name == "ModHttpV1"`, so do not rename it.

Not verified here (host side): provider selection with `#` filters, provider ID generation and the host's command routing.

## Mod Lifecycle and Commands

- SDK mods subclass `ModBehaviourBase` and implement `ModEnabled()` / `ModDisabled()`. Start async work with `.Forget()`. Without the SDK, see README_en "Method 2: Manual Integration".
- Commands use `System.CommandLine` (embedded in Ducky.Sdk): build a `Command` with a description, add options, set `Action = new ModAsynchronousCommandLineAction(...)`, and add it to the root command.
- Do not parse command text by hand; use `System.CommandLine`.

## Localization

Ducky.Sdk builds the localization from a mod's `LK.cs`:
1. Declare keys as `const string` (snake_case) in a nested static class such as `LK.Terminal`, under `[LanguageSupport(...)]`.
2. Build. The SDK generates `L.{Category}.{Key}` accessors and creates or updates `assets/Locales/{lang}.csv` for every listed language. Existing translations are kept; new keys get empty values.
3. Fill in each CSV (`Key,Value`; quote values with commas or special characters). An empty value falls back to the key name.
4. Rebuild and use `L.{Category}.{Key}` in code.

Do not hand-edit `lkeys.json` or `keys.hash.txt`; the SDK generates them.

## Gotchas

- Unity APIs (`GameObject`, `AddComponent`, `Destroy`, `Application.*`) must run on the main thread. Do not call them inside `UniTask.RunOnThreadPool` or `Task.Run`; `await UniTask.SwitchToMainThread()` first.
- Do not block the UI thread with synchronous I/O.
- Do not call `ShowToTerminal()` in tight loops; queue the messages instead.
- Do not create extra singleton contracts; use `GetOrCreate(modId)`.
- Normalize line endings in multiline output: `.Replace("\r\n", "\n").Replace("\r", "\n")`.
- Do not hard-code user-facing strings; use localization keys, and update all CSV files when adding one.

## Debugging Tips

- Log with `Log.Info()` (Ducky.Sdk); the output goes to the game console.
- Check that the `ModHttpV1` GameObject exists in the Unity scene (`ModHttpV1Proxy` depends on it).
- Check the ModId format: `steam.{publishedFileId}` or `local.{modName}`.
- The README_en example defines `ping`, which returns `pong`; use it to check the connection first.
