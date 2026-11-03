### Ducky TerminalUI 命令演示插件：面向开发者的实用命令实例
这是一款为 **Ducky SDK TerminalUI** 打造的演示项目，核心聚焦 "实用命令实例"—— 通过 4 个可直接操作的命令，让开发者直观理解如何实现 TerminalUI 交互命令，每个命令都是一套完整的 TerminalUI 交互示范，可直接参考复用。
#### 核心价值：4 个实用的 TerminalUI 命令实例
项目中的每一条命令都为演示 TerminalUI 交互逻辑设计，复制命令的代码结构，即可快速搭建自己的 TerminalUI 命令。
1. **ping** – 基础连接测试命令
* **功能**：验证 TerminalUI 连接是否正常。
* **使用方式**：在 TerminalUI 中输入 **ping**。
* **TerminalUI 反馈**：立即返回 **pong**，确认连接成功。
* **示范重点**：最简单的 TerminalUI 双向交互 —— 展示 "接收命令→发送反馈" 的基础流程。
1. **time** – 系统时间查询命令
* **功能**：获取当前系统时间（格式清晰）。
* **使用方式**：在 TerminalUI 中输入 **time**。
* **TerminalUI 反馈**：返回 **Current system time: 2025-11-16 14:30:00**（随实时时间变化）。
* **示范重点**：展示 "系统数据→TerminalUI 反馈" 的联动（无需额外参数）。
1. **heal** – 玩家回血控制命令
* **功能**：恢复主角生命值（支持自定义血量或回满）。
* **使用方式**：
* 回满血量：直接输入 **heal**。
* 自定义血量：输入 **heal --amount 50** 或简写 **heal -a 50**。
* **TerminalUI 反馈**：
* 回满时：**Healed 100 health points. Current health: 100/100**。
* 自定义时：**Healed 50 health points. Current health: 50/100**。
* **示范重点**：演示命令如何添加 "可选参数"，以及 "游戏内角色数据→TerminalUI 反馈" 的联动。
1. **god** – 玩家无敌模式切换命令
* **功能**：开启 / 关闭主角无敌模式（需传入必填参数）。
* **使用方式**：
* 开启无敌：输入 **god true**。
* 关闭无敌：输入 **god false**。
* **TerminalUI 反馈**：
* 开启时：**God mode enabled.**
* 关闭时：**God mode disabled.**
* **示范重点**：展示命令如何设置 "必填参数"，以及通过 TerminalUI 控制游戏内功能开关。
#### 面向开发者：可直接复用的命令逻辑
每个命令都包含完整的 TerminalUI 交互代码（从输入到反馈），可直接参考：
* 做简单数据查询命令，复制 **time**/**date** 的结构。
* 做需要可选输入的命令，复用 **heal** 的参数逻辑。
* 做需要必填开关的命令，参考 **god** 的结构。
作为 mod 作者，如果你要想为你的 mod 集成进入控制台，可以查看 [https://github.com/ducky7go/Ducky.Terminals](https://github.com/ducky7go/Ducky.Terminals) 来获取对接帮助。
