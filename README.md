# Ducky.Terminals

[English](./README_en.md) | 简体中文

## 📖 项目简介

**Ducky.Terminals** 是一个为 Unity 游戏《逃离鸭科夫》(Escape from Duckov) 开发的终端控制台框架。它提供 mod 之间的通信协议，允许 mod 开发者为他们的 mod 暴露命令行接口，由兼容的终端界面 mod 展示和路由命令。本仓库已不再包含终端界面 mod `Ducky.TerminalUI`。

### 核心特性

- 🎮 **游戏内终端** - 命令通过兼容的终端界面 mod 在游戏内输入和显示
- 🔌 **Mod 通信系统** - 基于消息总线的 mod 间通信协议
- 🛠️ **命令解析框架** - 集成 `System.CommandLine` 库，支持参数、选项等标准命令行特性
- 🎯 **Provider 过滤系统** - 通过 `#` 前缀快速选择目标 mod
- 📦 **两种集成方式** - 支持使用 Ducky.Sdk 或手动集成

## � 为什么要对接终端控制台？

作为 mod 开发者，对接终端控制台可以为你的 mod 和玩家带来诸多好处：

### 1️⃣ 快速配置，无需 UI 设计

你可以通过简单的文本命令让玩家配置你的 mod，而无需花费大量时间设计复杂的配置界面。

**示例：**
```bash
# 设置自动启动
set autostart false

# 配置刷新间隔
config refresh-interval 5000

# 开启调试模式
debug enable
```

### 2️⃣ 预留复杂操作的入口

为一些高级功能或诊断工具预留命令行接口，方便玩家一键执行复杂操作。

**示例：**
```bash
# 导出诊断数据
dump --output ~/duckov/logs/debug.zip

# 收集环境信息（配置、日志、数据）用于问题反馈
collect-env --include-logs --include-config

# 批量清理缓存
cleanup --cache --temp --logs
```

### 3️⃣ 公开 API 给其他 Mod 调用

**这是最强大的特性！** 终端命令不仅可以由玩家通过终端发送，**其他 mod 也可以向你的 mod 发送命令**。这使得跨 mod 协作成为可能，打造出强大的生态系统。

#### 实战案例：

**🔔 案例 1：统一通知中心**
```bash
# 任何 mod 都可以向通知中心发送通知
notify append --title "任务完成" --message "恭喜你完成了主线任务！" --type success

# 其他 mod 调用示例
await client.SendTo("local.NotificationCenter", "cli", "notify append --title 'HP过低' --type warning");
```

**🎨 案例 2：共享精美弹框组件**
```bash
# 制作一个精美的对话框 mod，其他 mod 无需引用 DLL 就能使用
dialog show --title "确认操作" --message "确定要删除存档吗？" --buttons "确定,取消"

# 其他 mod 调用
await client.SendTo("local.BeautifulDialog", "cli", "dialog show --title '提示' --message '操作成功'");
```

**📖 案例 3：存档的一生（时间线系统）**
```bash
# 制作一个"存档编年史" mod，记录玩家的游戏历程
timeline add --event "击杀 Boss：鸭王" --timestamp 1234567890
timeline add --event "首次通关：困难模式" --timestamp 1234567900

# 其他 mod 向时间线写入数据
await client.SendTo("local.SaveLifeline", "cli", "timeline add --event '解锁成就：不死之身' --category achievement");
await client.SendTo("local.SaveLifeline", "cli", "timeline add --event '累计击杀：1000' --category stats");
```

**📊 案例 4：统计数据中心**
```bash
# 制作一个数据统计 mod，汇总所有 mod 的数据
stats report --mod-id local.CombatMod --kills 150 --deaths 3
stats report --mod-id local.EconomyMod --gold-earned 5000

# 查询统计
stats query --period today
stats export --format json
```

### 🔗 Mod 间通信的优势

- **零依赖耦合**：Mod 之间无需直接引用彼此的 DLL
- **动态发现**：运行时发现可用的 mod 服务
- **版本兼容**：基于文本协议，天然向后兼容
- **易于调试**：玩家和开发者都可以直接通过终端测试命令
- **标准化接口**：使用 `System.CommandLine` 提供清晰的参数和帮助文档

## �🚀 快速开始

### 环境要求

- .NET SDK 8.0+
- Unity 游戏《逃离鸭科夫》已安装
- Visual Studio 2022 / Rider / VS Code

### 构建项目

1. **克隆仓库**
```bash
git clone https://github.com/ducky7go/Ducky.Terminals.git
cd Ducky.Terminals
```

2. **配置本地环境**

创建 `Local.props` 文件（该文件已被 git 忽略）：
```xml
<Project>
  <PropertyGroup>
    <SteamFolder>/path/to/Steam/</SteamFolder>
  </PropertyGroup>
</Project>
```

3. **构建所有项目**
```bash
dotnet build Ducky.Terminals.slnx
```

构建产物会自动部署到游戏的 `Duckov_Data/Mods/` 文件夹。

### 游戏内使用

1. 启动游戏，mod 会自动加载
2. 通过终端界面 mod 打开终端
3. 输入 `#` 查看可用的 mod provider
4. 选择 provider 后输入命令，例如：`time`、`heal --amount 50`

## 🔌 Mod 开发者对接指南

### 方式一：使用 Ducky.Sdk（推荐）

#### 1. 创建项目并引用 SDK

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

#### 2. 创建 ModBehaviour

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

#### 3. 实现命令解析

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

        // 示例：简单命令
        var pingCommand = new Command("ping", "Test connection");
        pingCommand.Action = new ModAsynchronousCommandLineAction(async context =>
        {
            return await UniTask.FromResult("pong");
        });
        rootCommand.Add(pingCommand);

        // 示例：带参数的命令
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

### 方式二：手动集成（不使用 SDK）

手动集成需要完成以下部分：

1. 实现 `ModHttpV1Proxy` - 通过反射访问游戏的 ModHttpV1 组件
2. 实现 `ModHttpV1ClientContract` - 封装底层通信
3. 实现 `ModTerminalClientContract` - 封装终端协议
4. 手动引用游戏 DLL 文件

## 📚 关键概念

### ModId 格式

- **Steam Mod**: `steam.{publishedFileId}` （例如：`steam.1234567890`）
- **本地 Mod**: `local.{modName}` （例如：`local.MyMod`）

### ProviderId

ProviderId 是用于终端选择的短标识符：

- **Steam Mod**: 取 publishedFileId 的后 3 位（经过预处理）
- **本地 Mod**: 取 mod 名称的前 3 个字符（经过预处理）
- **预处理规则**: 去除非字母数字字符，转小写
- **冲突解决**: 当出现重复时，自动增加长度（4、5、6...位）直到唯一

用户通过 `#` 前缀过滤，例如输入 `#816` 会过滤到匹配的 provider。

### 通信协议

所有终端命令必须使用 `contentType: "cli"`：

```csharp
// 发送命令
await client.SendTo(targetModId, "cli", "your command");

// 响应终端
await client.ShowToTerminal("response message");
```

### 命令实现最佳实践

✅ **推荐做法：**
- 使用 `ModAsynchronousCommandLineAction` 包装命令处理器（自动路由响应）
- 对 UniTask 调用使用 `.Forget()` 处理 fire-and-forget 场景
- 使用单例模式管理协议（`GetOrCreate(modId)`）
- 规范化换行符：`.Replace("\r\n", "\n").Replace("\r", "\n")`
- 多行内容使用 JSON 序列化：`TerminalUICommand.Show(JsonConvert.SerializeObject(content))`

❌ **避免做法：**
- 不要在 UI 线程执行同步 I/O 操作
- 避免在紧密循环中调用 `ShowToTerminal()`（应该批量发送）
- 不要手动解析命令 - 使用 `System.CommandLine` 解析器
- 不要创建多个单例实例（使用 `GetOrCreate()`）

## 🏗️ 架构说明

### 通信流程

```
用户输入命令
    ↓
终端界面 mod 接收命令
    ↓
通过 ModHttpV1 发送到目标 ModId
    ↓
目标 Mod 接收消息 (ModTerminalClientContract)
    ↓
System.CommandLine 解析命令 (ModCommandLineEntry)
    ↓
执行命令逻辑
    ↓
返回响应 (ModAsynchronousCommandLineAction)
    ↓
终端界面 mod 显示结果
```

## 🐛 调试技巧

- 使用 `Log.Info()` 输出日志（Ducky.Sdk）
- 检查 Unity 场景中是否存在 `ModHttpV1` GameObject
- 验证 ModId 格式：`steam.{id}` 或 `local.{name}`
- 先测试 `ping`/`pong` 命令验证连接
- 使用 `/?` 或 `help` 测试命令解析

## 📝 License

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🙏 致谢

- 《逃离鸭科夫》游戏开发团队
- Ducky.Sdk 开发者
- System.CommandLine

## 📦 相关链接

- [Ducky.Sdk NuGet Package](https://www.nuget.org/packages/Ducky.Sdk)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [UniTask](https://github.com/Cysharp/UniTask)
