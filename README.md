# Ducky.Terminals

[English](./README_en.md) | 简体中文

## 📖 项目简介

**Ducky.Terminals** 是一个为 Unity 游戏《逃离鸭科夫》(Escape from Duckov) 开发的终端控制台框架。它提供了一个可视化的终端界面和 mod 之间的通信系统，允许 mod 开发者为他们的 mod 创建命令行界面。

### 核心特性

- 🎮 **游戏内终端界面** - 可视化的命令行界面，支持滑动显示/隐藏
- 🔌 **Mod 通信系统** - 基于消息总线的 mod 间通信协议
- 🛠️ **命令解析框架** - 集成 `System.CommandLine` 库，支持参数、选项等标准命令行特性
- 🎯 **Provider 过滤系统** - 通过 `#` 前缀快速选择目标 mod
- 📦 **两种集成方式** - 支持使用 Ducky.Sdk 或手动集成

### 项目组件

| 组件 | 说明 | Steam Workshop |
|------|------|----------------|
| **Ducky.TerminalUI** | 终端 UI 主程序，负责显示界面和路由命令 | [3606793704](https://steamcommunity.com/sharedfiles/filedetails/?id=3606793704) |
| **Ducky.DemoTerminalClient** | 使用 Ducky.Sdk 的示例客户端（推荐方式） | [3606789816](https://steamcommunity.com/sharedfiles/filedetails/?id=3606789816) |
| **Ducky.DemoTerminalClientWithoutSdk** | 不使用 SDK 的手动集成示例 | [3606789962](https://steamcommunity.com/sharedfiles/filedetails/?id=3606789962) |

## 🚀 快速开始

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
2. 触发终端界面（在游戏界面左侧边缘区域）
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

参考 `Ducky.DemoTerminalClientWithoutSdk` 项目：

1. 实现 `ModHttpV1Proxy` - 通过反射访问游戏的 ModHttpV1 组件
2. 实现 `ModHttpV1ClientContract` - 封装底层通信
3. 实现 `ModTerminalClientContract` - 封装终端协议
4. 手动引用游戏 DLL 文件

详细示例代码请查看 [Ducky.DemoTerminalClientWithoutSdk](./Ducky.DemoTerminalClientWithoutSdk) 文件夹。

## 📚 关键概念

### ModId 格式

- **Steam Mod**: `steam.{publishedFileId}` （例如：`steam.3606789816`）
- **本地 Mod**: `local.{modName}` （例如：`local.Ducky.TerminalUI`）

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
TerminalUI 接收 (TerminalViewModel)
    ↓
通过 ModHttpV1 发送到目标 ModId (TerminalUIProtocol)
    ↓
目标 Mod 接收消息 (ModTerminalClientContract)
    ↓
System.CommandLine 解析命令 (ModCommandLineEntry)
    ↓
执行命令逻辑
    ↓
返回响应 (ModAsynchronousCommandLineAction)
    ↓
TerminalUI 显示结果 (TerminalMainView)
```

### MVVM 模式（TerminalUI）

TerminalUI 使用 **R3** (Reactive Extensions) 实现响应式编程：

- `ReactiveProperty<T>` - 可观察属性，带变更通知
- `ReactiveCommand<T>` - 命令模式实现
- `Subject<T>` - 单向事件流
- `.AddTo(disposables)` - 自动管理生命周期

**线程安全**: UI 更新必须在主线程。使用 `PostMessage()` 进行跨线程消息传递：
```csharp
// 线程安全：消息入队，通过协程在主线程处理
TerminalMainView.Instance.PostMessage("Response", MessageType.System);
```

## 🐛 调试技巧

- 使用 `Log.Info()` 输出日志（Ducky.Sdk）
- 检查 Unity 场景中是否存在 `ModHttpV1` GameObject
- 验证 ModId 格式：`steam.{id}` 或 `local.{name}`
- 先测试 `ping`/`pong` 命令验证连接
- 使用 `/?` 或 `help` 测试命令解析

## 📝 License

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 🤝 社区支持

- **QQ 群**: 755123281
- **群链接**: [逃离鸭科夫 鸭神降临 mod 技术交流](https://qm.qq.com/q/TjBZSgMOqK)
- **GitHub Issues**: [提交问题](https://github.com/ducky7go/Ducky.Terminals/issues)

## 🙏 致谢

- 《逃离鸭科夫》游戏开发团队
- Ducky.Sdk 开发者
- R3 (Reactive Extensions)
- System.CommandLine

## 📦 相关链接

- [Ducky.Sdk NuGet Package](https://www.nuget.org/packages/Ducky.Sdk)
- [System.CommandLine Documentation](https://learn.microsoft.com/en-us/dotnet/standard/commandline/)
- [UniTask](https://github.com/Cysharp/UniTask)
- [R3 (Reactive Extensions)](https://github.com/Cysharp/R3)
