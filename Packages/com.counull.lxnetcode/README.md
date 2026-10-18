# LxNetcode UPM 源码包

这是可由本地开发工程导入的 UPM 源码包骨架，manifest 版本为 `0.0.1-preview.1`。`Runtime/LxNetcode.Sockets.asmdef` 以 `noEngineReferences: true` 建立纯 C# 程序集，目前只有程序集版本元数据，没有公开 Socket API 或网络实现。

当前兼容基线为实际编译过的 Unity 7000.0.0a10、.NET Standard 2.1。包没有外部包依赖，没有 Native 插件或预编译 DLL；其他 Unity 版本与 Player 平台尚未验证。此版本号只标识骨架，不代表已有可用的网络功能或发布 tag。

开发工程通过本地 `file:` 路径引用此目录；未来实际游戏项目通过 Git URL 安装。具体 URL 形式与刷新行为见[组织决策](../../docs/ARCHITECTURE.md)。远程 Git URL 安装尚未运行。

Socket 运行时保持纯 C#，不依赖 UnityEngine。AI 只在设计获批后实现原始 UDP Socket I/O；上层包格式、协议、连接状态和游戏网络逻辑由项目作者手写，完整范围见[项目指令](../../AGENTS.md)。
