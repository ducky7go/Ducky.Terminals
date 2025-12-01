
# Mod Ark – Mod Configuration Guardian
## 🧰 Main Features
Mod Ark keeps your Escape from Duckov mod list safe by snapshotting subscriptions, load order, and enabled states. Restore them later after a profile reset or when experimenting with new builds.
- **Full-State Backups**: Capture Steam subscriptions, local order, and enable toggles with one command.
- **Safe Restores**: Validate backup JSON before applying and warn before overwriting current setups.
- **Progress Messages**: Real-time log lines forwarded to the terminal during both backup and restore phases.
**Note: This is a vanilla (original) mod manager and does not operate on BML data.**
## 🎮 Terminal Commands
### How to Enable Terminal
1. Install required mods and enable them.
2. Move the cursor to the bottom-left of the screen and click.
3. When the terminal pops up, select **Mod Ark**.
4. Type **help** to list all commands.
### Common Commands
- **backup \<name\>**: Create a snapshot with the provided name.
- **restore --yes**: Restore the latest backup JSON placed inside the prompted folder. **--yes** confirms overwriting current settings.
## ⚠️ Important Notes
### Backup Naming
Use descriptive backup names (e.g., **pre_update_run**). Invalid filename characters are automatically sanitized.
### Restore Confirmation
Restore replaces your current configuration. Review the backup JSON before running **restore --yes**.
## 🔗 Technical Support
Need technical support, bug reports, or suggestions?
Please visit **[Contact Me Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3613778329)** for contact information