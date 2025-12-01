# Ducky TerminalUI Command Demo Plugin: Practical Command Examples for Developers
### Ducky TerminalUI Command Demo Plugin: Practical Command Examples for Developers
This is a demo project built for **Ducky SDK TerminalUI**, focusing on showing practical command examples. It helps developers understand how to implement TerminalUI-interactive commands through specific, usable commands—each command is a complete demo of TerminalUI interaction, ready for reference and reuse.
#### Core Value: 4 Practical TerminalUI Command Examples
Every command in the project is designed to demonstrate TerminalUI interaction logic. You can directly copy the command code structure to build your own TerminalUI commands.
1. **ping** – Basic Connection Test Command
* **Function**: Verify if the TerminalUI connection is normal.
* **Usage**: Enter **ping** in TerminalUI.
* **TerminalUI Feedback**: Immediately returns **pong** to confirm the connection is successful.
* **Why It Matters**: The simplest TerminalUI two-way interaction demo—shows how to receive commands and send feedback.
1. **time** – System Time Query Command
* **Function**: Get the current system time (with clear format).
* **Usage**: Enter **time** in TerminalUI.
* **TerminalUI Feedback**: Returns **Current system time: 2025-11-16 14:30:00** (adapts to real-time).
* **Key Demo Point**: Shows how to link system data to TerminalUI feedback (no extra parameters needed).
1. **heal** – Player Health Restoration Command
* **Function**: Restore the main character's health (supports custom or full restoration).
* **Usage**:
* Full health: Enter **heal** directly.
* Custom health: Enter **heal --amount 50** or the shorthand **heal -a 50**.
* **TerminalUI Feedback**:
* Full restoration: **Healed 100 health points. Current health: 100/100**.
* Custom restoration: **Healed 50 health points. Current health: 50/100**.
* **Key Demo Point**: Demonstrates how to add optional parameters to commands and link in-game character data to TerminalUI.
1. **god** – Player Invincibility Toggle Command
* **Function**: Turn the main character's god mode (invincibility) on or off (requires mandatory parameters).
* **Usage**:
* Enable: Enter **god true**.
* Disable: Enter **god false**.
* **TerminalUI Feedback**:
* Enable: **God mode enabled.**
* Disable: **God mode disabled.**
* **Key Demo Point**: Shows how to set mandatory parameters for commands and control in-game function switches via TerminalUI.
#### For Developers: Directly Reusable Command Logic
Each command includes complete TerminalUI interaction code (from input to feedback). You can:
* Copy the **time**/**date** structure for simple data query commands.
* Reuse the **heal** parameter logic for commands that need optional inputs.
* Reference the **god** structure for commands that require mandatory switches.
If you are a mod author and want to integrate your mod into the console, please visit [https://github.com/ducky7go/Ducky.Terminals](https://github.com/ducky7go/Ducky.Terminals) for integration help.
## 🔗 Technical Support
Need technical support, bug reports, or suggestions?
Please visit **[Contact Me Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=3613778329)** for contact information