# LxNetcode UPM 源码包

这是未来 UPM 包的预留目录，当前没有 `package.json`、Runtime 程序集或网络实现，因此尚不可通过 Package Manager 安装。

未来实际游戏项目通过 Git URL 引用此目录。具体 URL 形式与兼容性前置条件见[组织决策](../../docs/ARCHITECTURE.md)。

Socket 运行时保持纯 C#，不依赖 UnityEngine。AI 只在设计获批后实现原始 UDP Socket I/O；上层包格式、协议、连接状态和游戏网络逻辑由项目作者手写，完整范围见[项目指令](../../AGENTS.md)。
