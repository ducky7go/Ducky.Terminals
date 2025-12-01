# Project Context

## Purpose
Ducky.Terminals is a terminal console framework for Unity game "Escape from Duckov". It provides a visual terminal interface and inter-mod communication system that allows mod developers to create command-line interfaces for their mods.

## Tech Stack
- **.NET Standard 2.1** - Target framework for Unity compatibility
- **Ducky.Sdk** (v0.1.7-dev.2) - Core SDK for mod development
- **R3** (v1.3.0) - Reactive Extensions for .NET
- **ObservableCollections.R3** (v3.3.4) - Reactive collections
- **System.CommandLine** - Command line parsing framework
- **Unity** - Game engine
- **UniTask** - Async/await integration for Unity

## Project Conventions

### Code Style
- C# conventions following Microsoft style guidelines
- Async/await patterns with UniTask for Unity integration
- Reactive programming using R3 for UI updates
- Singleton pattern for protocol management (`GetOrCreate(modId)`)
- Normalized line endings: `.Replace("\r\n", "\n").Replace("\r", "\n")`

### Architecture Patterns
- **MVVM Pattern** - TerminalUI uses R3 ReactiveProperty and ReactiveCommand
- **Message Bus** - Inter-mod communication via ModHttpV1
- **Command Pattern** - System.CommandLine for parsing terminal commands
- **Observer Pattern** - Reactive extensions for data streams
- **Provider Pattern** - Filter system using # prefix for mod selection

### Testing Strategy
- Manual testing through in-game terminal
- ping/pong commands for connection verification
- Command help system (`/?` or `help`) for validation

### Git Workflow
- Main branch for stable releases
- Commit messages should reference Steam Workshop IDs where applicable
- GitHub flow with feature branches

## Domain Context

### ModId Formats
- **Steam Mods**: `steam.{publishedFileId}` (e.g., `steam.3606789816`)
- **Local Mods**: `local.{modName}` (e.g., `local.Ducky.TerminalUI`)

### ProviderId System
- Short identifier for terminal selection
- Steam mods: last 3 digits of publishedFileId
- Local mods: first 3 characters of mod name
- Auto-increments length when conflicts occur

### Communication Protocol
- All terminal commands use `contentType: "cli"`
- Thread-safe UI updates via `PostMessage()`
- JSON serialization for multi-line content

## Important Constraints
- **Unity Threading**: UI updates must be on main thread
- **Performance**: Avoid tight loops with `ShowToTerminal()` calls
- **Fire-and-forget**: Use `.Forget()` for UniTask calls when result not needed
- **Single Instance**: Use singleton pattern for protocol clients
- **Dependencies**: Must handle missing dependencies gracefully

## External Dependencies
- **Unity Engine**: Game runtime environment
- **SteamUGC**: Steam Workshop integration for mod subscriptions
- **ModHttpV1**: Internal Unity component for mod communication
- **Steam Client**: Required for backup/restore functionality in Ducky.ModArk
