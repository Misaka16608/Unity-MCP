# CLAUDE.md

此文件为 Claude Code (claude.ai/code) 在处理本仓库代码时提供指导。

## 项目概述

Unity-MCP 是一个通过 [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) 将 LLM（Claude、Cursor、Copilot、Gemini 等）与 Unity Editor 及 Runtime 连接起来的桥梁。项目包含以下子项目：

| 子项目 | 语言/框架 | 用途 |
|--------|-----------|------|
| `Unity-MCP-Plugin/` | C# (Unity UPM 包) | Unity Editor/Runtime 插件，注册并执行 MCP 工具、提示词和资源 |
| `cli/` | TypeScript (Node.js, Commander) | 跨平台命令行工具 `unity-mcp-cli`，用于安装插件、配置 MCP、管理连接 |
| `Installer/` | Unity 项目 | Unity 安装器包（`.unitypackage`），用于一键安装插件 |
| `Unity-Tests/` | Unity 项目 | 按 Unity 版本划分的测试项目目录 |

**关键依赖**: MCP Server **不在**本仓库中。插件消费的是共享仓库 [GameDev-MCP-Server](https://github.com/IvanMurzak/GameDev-MCP-Server)（ASP.NET Core + SignalR hub，二进制文件名为 `gamedev-mcp-server`），该 Server 被 Unity-MCP、Godot-MCP 和 Unreal-MCP 共用。插件通过 `McpServerManager.cs` 中的 `ServerVersion` 常量自动下载对应版本的 Server 二进制文件，下载到 `Library/mcp-server/{platform}/`。Docker 镜像发布为 `aigamedeveloper/mcp-server`。

- **语言**: C# (插件), TypeScript (CLI)
- **许可**: Apache-2.0 (Copyright 2025 Ivan Murzak)
- **最低 Unity 版本**: 2022.3+
- **最低 Node 版本**: ^20.19.0 或 >=22.12.0

## 构建与运行

### CLI

```bash
cd cli
npm install
npm run build        # TypeScript 编译 (tsc)
npm test             # 运行 vitest 测试套件
```

### Unity 插件

- 在 Unity Editor 中打开 `Unity-MCP-Plugin/` 文件夹（Unity 自动编译）
- 测试通过 Unity Test Runner GUI（`Window > General > Test Runner`）:
  - EditMode 测试位于 `Packages/com.ivanmurzak.unity.mcp/Tests/Editor`
  - PlayMode 测试位于 `Packages/com.ivanmurzak.unity.mcp/Tests/Runtime`
- MCP Inspector（调试协议消息）: `Unity-MCP-Plugin/Commands/start_mcp_inspector.bat`

### CLI 驱动的 Unity 测试

```powershell
.\commands\run-unity-tests.ps1 -UnityPath "<path-to-Unity.exe>" -TestMode editmode|playmode|standalone|all
```

### 版本号更新

```powershell
.\commands\bump-version.ps1 <version>
```
更新 `package.json`、`cli/package.json`、`Installer.cs`、`UnityMcpPlugin.cs` 和 `cli/package-lock.json`。

### CI/CD

工作流位于 `.github/workflows/`。CI 矩阵为 18 种组合（3 个 Unity 版本 x 3 种测试模式 x 2 个操作系统）。不受信任贡献者的 PR 需要维护者添加 `ci-ok` 标签后才触发 CI。PR 中不得修改 workflow 文件。

## 架构

```
MCP Client (Claude/Cursor/Copilot 等)
      ↕ stdio 或 streamableHttp
GameDev-MCP-Server  (ASP.NET Core + SignalR hub) — 外部共享仓库
      ↕ SignalR
Unity-MCP-Plugin  (Unity Editor/Runtime)
      ↕ Unity API (主线程)
Unity Engine
```

### 关键架构模式

- **确定性端口哈希**: 对项目路径做 SHA256 哈希，映射到 20000-29999 范围。插件和 CLI 独立计算相同端口，无需配置文件。
- **主线程调度**: 所有 Unity API 调用必须通过 `MainThread.Instance.Run(() => ...)`（同步）或 `RunAsync()`（异步）。调度器在 Update 循环中通过队列执行，将 SignalR 后台线程的调用封送到 Unity 主线程。
- **Server 二进制文件生命周期**: `[InitializeOnLoad]` 触发 `Startup.cs` -> `McpServerManager.cs` 下载 Server 二进制文件到 `Library/mcp-server/{platform}/` 并启动进程。Editor 退出时自动终止 Server。二进制下载是原子的（临时文件 + 重命名），避免文件共享冲突。
- **版本握手**: 插件和 Server 在 SignalR 连接时交换版本号，版本不匹配会触发重新下载。
- **基于属性的 MCP 发现**: 使用 `[AiToolType]` 标记类，`[AiTool]` 标记方法，`[AiPromptType]`/`[AiPrompt]` 标记提示词，启动时通过反射发现。每个文件一个操作，使用 `partial` 类（例如 `Tool_GameObject.Create.cs`）。
- **`UnityMcpPlugin.cs`**: 线程安全的单例，管理插件生命周期。通过反射发现 MCP 功能，使用 R3 库进行响应式状态监控建立 SignalR 连接。
- **序列化**: 为 Unity 类型（GameObject、Component、Vector3 等）提供自定义 JSON 转换器，位于 `Runtime/ReflectionConverters/` 和 `Runtime/JsonConverters/`。Unity 对象转换为引用格式（`GameObjectRef`、`ComponentRef`），并跟踪 instanceID。
- **CLI 库模式**: `cli/src/lib.ts` 无副作用，采用可区分联合类型（`kind: 'success' | 'failure'`），通过 `onProgress` 回调报告进度，不抛出异常。

## 关键路径

| 路径 | 说明 |
|------|------|
| `Unity-MCP-Plugin/Packages/com.ivanmurzak.unity.mcp/` | UPM 包根目录 |
| `.../Editor/Scripts/API/` | Editor 端 MCP 工具实现 |
| `.../Editor/Scripts/Startup.cs` | `[InitializeOnLoad]` 入口点 |
| `.../Editor/Scripts/McpServerManager.cs` | Server 二进制文件下载和生命周期管理 |
| `.../Runtime/UnityMcpPlugin.cs` | 核心插件生命周期 + SignalR |
| `.../Runtime/Utils/MainThreadDispatcher.cs` | 主线程封送调度 |
| `cli/src/` | CLI 源码（TypeScript，基于 Commander） |
| `cli/src/index.ts` | CLI 命令注册入口 |
| `cli/src/lib.ts` | 库入口（无副作用） |
| `Installer/Assets/` | Unity 安装器包 |
| `Unity-Tests/<version>/` | 按 Unity 版本划分的测试项目 |

## 关键规则与规范

- **`#nullable enable`** 必须在每个 C# 文件顶部
- **禁止使用反射访问私有成员**: `System.Reflection` 不得访问 private/internal/non-public 成员。例外: 允许使用 `ReflectorNet` 库。
- **项目路径不能包含空格**: 启动时验证；SignalR/序列化栈在含空格的路径上会出问题。
- **MCP 工具必须返回结构化类型**: 类型化数据模型、`List<T>`、`void` 或 `Task`；避免返回原始字符串。
- **MCP 工具通过抛出异常报告错误**: 使用 `ArgumentException` 或 `Exception`，绝不返回错误字符串。
- **工具名称使用 kebab-case** 并带有类别前缀（例如 `gameobject-create`、`assets-find`）。
- **命名空间模式**: `com.IvanMurzak.Unity.MCP.[Tier].[Component]`
- **每个文件必须包含版权声明头部**。
- **编辑器编译期 MCP 静默**: 对 `.cs` 文件的编辑在重新编译期间会导致 MCP 无响应——直接从磁盘读取 `Editor.log` 来查看编译错误。

## 详细参考文档

- [docs/claude/architecture.md](docs/claude/architecture.md) — 系统架构：SignalR 桥接、主线程执行、确定性端口哈希
- [docs/claude/style.md](docs/claude/style.md) — 编码规范
- [docs/claude/release.md](docs/claude/release.md) — 发布、版本管理、CI/CD
- [docs/claude/documentation-sync.md](docs/claude/documentation-sync.md) — README 翻译/复制同步要求
- [docs/dev/Development.md](docs/dev/Development.md) — 完整开发指南（本地搭建、架构、CI/CD 详情）
- [docs/default-mcp-tools.md](docs/default-mcp-tools.md) — 所有内置 MCP 工具的完整参考
- [docs/mcp-server.md](docs/mcp-server.md) — Server 配置、环境变量、远程托管
- [Unity-MCP-Plugin/CLAUDE.md](Unity-MCP-Plugin/CLAUDE.md) — 子项目详情（Editor/Runtime 划分、插件专属文档）
- [cli/README.md](cli/README.md) — CLI 命令参考
