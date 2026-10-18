# LxNetcode

![LxNetcode](docs/images/lxnetcode-cover.png)

C# 游戏网络学习项目。由项目作者从零研究并手写包格式、协议、连接状态、可靠性及游戏网络功能；AI 的实现职责仅限于必要且可靠的原始 UDP Socket I/O 封装。

## 当前阶段

Unity 7000.0.0a10 开发工程与内嵌 UPM 源码包已建立。原始 UDP Socket 封装采用同步非阻塞方案，公开接口与边界见 [Socket 契约](docs/SOCKET.md)，实际执行结果与未验证范围见[测试记录](docs/TESTING.md)。尚未创建发布 tag。

## 仓库结构

| 路径 | 用途 |
| --- | --- |
| `docs/` | 项目状态、组织决策和测试计划，随代码提交到 Git |
| `Unity/` | Unity 7000.0.0a10 开发与验证工程 |
| `Unity/Packages/com.counull.lxnetcode/` | 工程内嵌的 UPM 源码包与纯 C# UDP Socket 程序集 |
| `.agents/skills/` | 随仓库分发的项目技能 |
| `AGENTS.md` | AI 职责边界与协作规则 |

包源码直接放在开发工程的 `Unity/Packages/` 下，由 Unity 识别为 Embedded Package；在编辑器的 Packages 中即可维护同一份源码。实际游戏项目以后通过 Unity Package Manager 的 Git URL（`?path=/Unity/Packages/com.counull.lxnetcode`）引用包，不需要先分发 DLL。包布局、同步方式和兼容基线见[组织决策](docs/ARCHITECTURE.md)。

## 打开工程

安装 Git LFS 后，在仓库根目录执行：

```powershell
git lfs install --local
git lfs pull
```

使用 Unity Hub 添加并打开 `Unity/`，选择 **7000.0.0a10**，打开 `Assets/Scenes/Development.unity`。开发工程已配置 Unity 官方 URP 17.7.0；当前 Standalone 配置为 .NET Standard 2.1、CoreCLR，Runtime asmdef 禁止 UnityEngine 引用。初始化检查命令、实际 GPU 渲染与布局检查结果见[测试记录](docs/TESTING.md)。

若编辑器继承了旧版本的最大化/浮动布局，在右上角 `Layout` 菜单选择 `Default`。本机已恢复并验证六个常用面板的停靠状态；布局属于 `UserSettings`，不纳入 Git。

图片、模型、音频等二进制资源由 `.gitattributes` 配置 LFS；源码、Unity YAML 与 `.meta` 保留为普通文本。现有封面已转入 LFS，未改写历史提交。

## 维护入口

从[文档索引](docs/README.md)进入维护文档；[项目状态](docs/STATUS.md)记录已完成事项和待确认项，[测试计划](docs/TESTING.md)区分计划与实际执行结果。

项目技能包括 `$lxnetcode-socket-workflow` 和 `$lxnetcode-socket-review`。技能位置采用 [OpenAI 官方仓库技能约定](https://learn.chatgpt.com/docs/build-skills#where-codex-loads-local-skills)。

Matt Pocock 工程技能已配置使用 GitHub Issues、默认 triage 标签和单上下文领域文档布局；配置文件位于 `docs/agents/`，由 `AGENTS.md` 引用。选择 GitHub 跟踪方式不代表已创建远程 Issue 或标签。

## 学习资料

资料用于理解概念与权衡，不构成授权 AI 实现上层网络栈。

- [Gaffer On Games — Game Networking](https://gafferongames.com/categories/game-networking/)
- [Gabriel Gambetta — Fast-Paced Multiplayer](https://gabrielgambetta.com/)
- [Valve — Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)
