# Unity 与 Rider 开发工具

## 官方 Unity MCP

本工程使用 Unity 官方 CLI 的 `unity mcp`，由开发工程中的 `com.unity.pipeline` 连接运行中的编辑器。Unity 已弃用 `com.unity.ai.assistant` 中旧的 MCP Server，当前推荐 CLI 提供的 MCP；本工程不使用 `com.coplaydev.unity-mcp`。参考 [Unity 官方迁移说明](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli)和 [Pipeline 安装说明](https://docs.unity.com/en-us/unity-cli/unity-pipeline/unity-pipeline-package)。

2026-10-18 实际安装与验证基线：

| 项目 | 实际值 |
| --- | --- |
| Unity Editor | 7000.0.0a10，Windows、CoreCLR |
| 官方 Unity CLI | `1.0.0-beta.13` |
| Unity Pipeline | `com.unity.pipeline` `0.8.0-exp.1`，Unity Registry |
| Rider Editor 集成 | `com.unity.ide.rider` `3.1.0`，Unity Registry |
| Rider IDE | 2026.2.3.1 |

CLI 通过 [Unity 官方安装器](https://unity.com/install.ps1)安装，安装器已校验下载二进制的 SHA-256。CLI、Pipeline 仍为预发布版本，以上只代表本机实际验证结果。安装及后续升级参考[官方 CLI 文档](https://docs.unity.com/en-us/unity-cli/use-unity-cli)。

Pipeline 与 Rider 集成只声明在 `Unity/Packages/manifest.json`，实际解析的锁文件一并纳入 Git。Pipeline 带入 Unity Test Framework 1.9.0、NUnit 扩展 2.1.2、Newtonsoft Json 3.2.2 和 Mono.Cecil 1.11.6 等开发依赖。可分发包 `com.counull.lxnetcode` 的 `dependencies` 仍为空，Runtime 程序集没有引用 UnityEngine、UnityEditor、Pipeline 或 Rider。

## 日常连接

先打开 `E:\LxNetcode\Unity`，等待包导入及脚本编译完成，再检查连接：

```powershell
unity pipeline list --json
unity command editor_status --project-path 'E:\LxNetcode\Unity' --json
```

这些命令已实际运行。Pipeline 列表确认包已安装且本地服务可达；`editor_status` 返回 `ready`、`compiling: false`、工程路径和 `7000.0.0a10`。服务端口会在重载后变化，应由 CLI 自动发现，不将本次端口写死到客户端。

MCP 客户端使用官方 CLI 的 stdio 服务：

```powershell
unity mcp
# 同时打开多个工程时，为该服务明确指定目标：
unity mcp --project-path 'E:\LxNetcode\Unity'
```

本机 Codex 的 `C:\Users\Lxxr\.codex\config.toml` 已新增 `mcp_servers.unity`，使用实际安装的 CLI 绝对路径和 `args = ["mcp"]`；旧 `unityMCP` 入口设为 `enabled = false`，保留以便恢复。修改前已在同目录备份，其他配置逐项比较未改变。本机路径、备份和凭据不提交到仓库。新终端可使用安装器加入 PATH 的 `unity`；现有会话若尚未显示新 MCP 工具，需刷新连接或重新进入会话。配置格式见 [OpenAI 官方 MCP 文档](https://learn.chatgpt.com/docs/extend/mcp?surface=cli)。

实际 MCP 验证：stdio `initialize` 返回 `unity-mcp` / `1.0.0-beta.13`；编辑器就绪后 `tools/list` 返回 178 项；真实 `tools/call editor_status` 成功返回正确工程和 Unity 版本。还通过官方 `eval` 调用了现有 `LxNetcodeProjectCheck.Run`，返回 `LXNETCODE_PROJECT_CHECK_PASS`，再次确认 Embedded 包、Runtime 无 Unity 引用、两次显式刷新及 URP 配置。编辑器未运行时，不能把 CLI 握手成功或配置存在当作工程工具已就绪。

## Rider 与 C# 工程

本机 Rider 从 `E:\Cache\jetbrains\Rider\bin\rider64.exe` 启动，打开 `E:\LxNetcode\Unity`。已按 [JetBrains 官方 Unity 接入方式](https://www.jetbrains.com/help/rider/Unity.html)安装 Rider Editor 集成，并将它选为 External Script Editor。

最初的 `Unity.sln` 只有空的 Global 段，没有 C# Project，Rider 因此无法做项目语义分析。通过官方 Unity MCP 的 `eval` 实际执行：

```csharp
Unity.CodeEditor.CodeEditor.SetExternalScriptEditor(
    @"E:\Cache\jetbrains\Rider\bin\rider64.exe");
Unity.CodeEditor.CodeEditor.CurrentEditor.SyncAll();
```

结果确认脚本编辑器为 `Packages.Rider.Editor.RiderScriptEditor`，并生成 `Unity.sln`、`LxNetcode.Sockets.csproj`、`Assembly-CSharp.csproj`、`Assembly-CSharp-Editor.csproj`。Runtime 工程包含迁移后包内两份 UDP 源文件，语言版本为 9.0。这些文件由 Unity 生成并被 Git 忽略，不将它们当作独立 DLL 发布工程；实际 API 兼容级别仍以 Unity 配置和真实编译响应文件为准。

Rider MCP 继续使用既有本机配置。已真实完成 `initialize`、`tools/list` 与 `list_directory_tree` 调用，服务报告 `JetBrains Rider MCP Server` / `2026.2.3.1`，成功读取 `Packages/com.counull.lxnetcode`。生成工程后，Socket Goal 对话已确认 Rider 识别三个 C# Project，并实际完成源码语义检查；具体 Socket 审查与运行结果由其测试记录说明。

本机验证脚本、协议返回与日志保留在被忽略的 `Unity/Logs/`。本节不代表已验证其他机器、其他 Unity 版本、Player 平台或远程 Git URL 安装。
