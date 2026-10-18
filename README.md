# LxNetcode

C# 游戏网络学习项目。由项目作者从零研究并手写包格式、协议、连接状态、可靠性及游戏网络功能；AI 的实现职责仅限于必要且可靠的原始 UDP Socket I/O 封装。

## 当前阶段

正在初始化仓库目录、维护文档和项目技能。Socket 设计尚未批准，没有网络实现、可运行测试或可安装的 UPM 包。

## 仓库结构

| 路径 | 用途 |
| --- | --- |
| `docs/` | 项目状态、组织决策和测试计划，随代码提交到 Git |
| `Unity/` | Unity 开发与验证工程的预留目录 |
| `Packages/com.counull.lxnetcode/` | 未来通过 Git URL 分发的 UPM 源码包目录 |
| `.agents/skills/` | 随仓库分发的项目技能 |
| `AGENTS.md` | AI 职责边界与协作规则 |

开发与验证工程将消费同一份包源码。实际游戏项目以后通过 Unity Package Manager 的 Git URL 引用包，不需要先分发 DLL。包布局与接入前置条件见[组织决策](docs/ARCHITECTURE.md)。

## 维护入口

从[文档索引](docs/README.md)进入维护文档；[项目状态](docs/STATUS.md)记录已完成事项和待确认项，[测试计划](docs/TESTING.md)区分计划与实际执行结果。

项目技能包括 `$lxnetcode-socket-workflow` 和 `$lxnetcode-socket-review`。技能位置采用 [OpenAI 官方仓库技能约定](https://learn.chatgpt.com/docs/build-skills#where-codex-loads-local-skills)。

## 学习资料

资料用于理解概念与权衡，不构成授权 AI 实现上层网络栈。

- [Gaffer On Games — Game Networking](https://gafferongames.com/categories/game-networking/)
- [Gabriel Gambetta — Fast-Paced Multiplayer](https://gabrielgambetta.com/)
- [Valve — Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)
