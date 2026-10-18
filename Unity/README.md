# Unity 开发与验证工程

这是由 Unity **7000.0.0a10（4b5f18082ea8）** 实际创建的开发与验证工程。在 Unity Hub 中添加并打开本目录。

初始化时实际读取的 Standalone 配置为 Windows 64 位、.NET Standard 2.1、CoreCLR；序列化模式为 Force Text。完整证据与批处理检查命令见[测试记录](../docs/TESTING.md)。

`Packages/manifest.json` 通过 `file:../../Packages/com.counull.lxnetcode` 引用仓库中的包源码。编辑包目录后刷新 Unity 即可导入；不需要复制包或重新构建 DLL。独立游戏项目以后通过 Git URL 安装该 UPM 包。

`Assets/Editor/LxNetcodeProjectCheck.cs` 仅在开发工程中检查包路径、Runtime 的程序集引用及两次显式刷新。它不随 UPM 包分发，不包含 Socket 实现。当前没有场景、网络测试运行器或额外注册表包依赖。

工程源码、Unity `.meta`、项目配置、包 manifest 与锁文件纳入 Git；Library、Temp、Logs 等生成数据由根目录 `.gitignore` 排除。

组织方式见[组织决策](../docs/ARCHITECTURE.md)，当前阶段见[项目状态](../docs/STATUS.md)。
