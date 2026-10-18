# Unity 开发与验证工程

这是由 Unity **7000.0.0a10（4b5f18082ea8）** 实际创建的开发与验证工程。在 Unity Hub 中添加并打开本目录。

初始化时实际读取的 Standalone 配置为 Windows 64 位、.NET Standard 2.1、CoreCLR；序列化模式为 Force Text。开发工程使用 Unity 官方 URP 17.7.0，Graphics 已指向 `Assets/Settings/LxNetcode_RenderPipeline.asset`。打开 `Assets/Scenes/Development.unity` 可得到只有默认相机与方向光的基础场景。完整证据与检查命令见[测试记录](../docs/TESTING.md)。

包源码直接位于本工程的 `Packages/com.counull.lxnetcode/`，Unity 自动将其识别为 Embedded Package，`Packages/manifest.json` 无需为它声明 `file:` 依赖。编辑这个包目录后刷新 Unity 即可导入；不需要复制包或重新构建 DLL。独立游戏项目以后通过 Git URL 的 `?path=/Unity/Packages/com.counull.lxnetcode` 安装该 UPM 包。

`Assets/Editor/LxNetcodeProjectCheck.cs` 检查包路径、Runtime 的程序集引用、两次显式刷新及渲染管线是否配置。`Assets/Editor/LxNetcodeSocketLoopback.cs` 通过真实 loopback 验证原始 UDP 收发和生命周期；菜单入口为 `LxNetcode/Run Socket Loopback Checks`，批处理命令与实际结果见[测试记录](../docs/TESTING.md)。这些验证脚本不随 UPM 包分发；自有源码包仍无包依赖。

本机旧布局已备份到 `UserSettings/InitializationRepair-20261018T143742Z/`，恢复后的 Scene、Game、Hierarchy、Inspector、Project、Console 均已通过真实编辑器实例的停靠检查。其他机器若继承旧布局，可在 `Layout` 菜单选择 `Default`。布局与备份都只保留在本机。

工程源码、Unity `.meta`、项目配置、包 manifest 与锁文件纳入 Git；Library、Temp、Logs 等生成数据由根目录 `.gitignore` 排除。

开发工程已接入官方 Unity CLI/Pipeline MCP 和 Rider Editor；Rider 的 solution 与 `.csproj` 由 Unity 生成并被 Git 忽略。安装、连接检查和实际验证见[开发工具记录](../docs/UNITY_TOOLS.md)。

组织方式见[组织决策](../docs/ARCHITECTURE.md)，当前阶段见[项目状态](../docs/STATUS.md)。
