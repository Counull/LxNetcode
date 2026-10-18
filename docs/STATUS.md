# 项目状态

更新日期：2026-10-18。

## 当前阶段

Unity 开发工程、内嵌 UPM 源码包、Git LFS、维护文档与项目技能已建立。原始 UDP Socket 已按同步非阻塞方案实现；截断问题修复后，真实 Unity loopback **30 通过、0 失败、0 跳过，退出码 0**。公开接口与所有权见 [SOCKET.md](SOCKET.md)，执行证据见 [TESTING.md](TESTING.md)。尚无发布 tag。

## 初始化内容

- `docs/`：维护文档及其索引。
- `Unity/`：实际开发与验证工程，包源码直接位于工程内的 `Packages/`，由 Unity 作为 Embedded Package 导入。
- 开发工程 Graphics 使用 URP 17.7.0，基础场景为 `Assets/Scenes/Development.unity`（默认相机、方向光）；本机旧布局已备份并恢复，布局不纳入 Git。
- `Unity/Packages/com.counull.lxnetcode/`：版本为 `0.0.1-preview.1` 的包 manifest，以及禁止 UnityEngine 引用的 `LxNetcode.Sockets` asmdef。
- `.gitattributes`：二进制资源使用 Git LFS，源码、Unity YAML 和 `.meta` 保持普通文本。已执行 `git lfs install --local`，当前封面转为 LFS 指针；未迁移历史。
- `AGENTS.md`：持久化项目目标、AI 范围与设计确认要求。
- `.agents/skills/`：Socket 设计/实现流程和只读审查技能。
- `docs/agents/`：Matt Pocock 工程技能配置，使用 GitHub Issues（Counull/LxNetcode）、默认 triage 标签及单上下文领域文档布局。
- README 明确作者与 AI 的职责；`.gitignore` 排除 Unity 和 .NET 的生成数据，保留维护文档与项目技能。

## 已确认的方向

- 维护文档与项目技能随仓库提交到 Git。
- `docs/` 与 Unity 工程目录分别组织。
- 未来实际游戏项目通过 Git URL 引用 UPM 包；开发工程消费同一份包源码。
- 作者要求按游戏最佳实践选择接收模型，本阶段采用同步非阻塞原始 UDP I/O；不提供上层协议功能。

## 实际工程配置

| 项目 | 当前实际值 |
| --- | --- |
| Unity | 7000.0.0a10，revision `4b5f18082ea8` |
| 当前构建目标 | StandaloneWindows64 |
| Standalone API Compatibility Level | .NET Standard 2.1，枚举值 6 |
| Standalone 脚本后端 | CoreCLR |
| Editor 运行时 | 10.0.10；不据此推定可使用 .NET 10 API |
| Runtime 编译器语言版本 | C# 9.0（实际编译响应文件） |
| Unity 序列化 | Force Text |
| 包来源 | Embedded，实际解析至 `Unity/Packages/com.counull.lxnetcode/` |
| 渲染管线 | Unity 官方 URP 17.7.0，仅用于开发工程 |
| 包依赖 | 自有包无依赖；开发工程含 Unity 内建模块、URP 17.7.0、Pipeline 0.8.0-exp.1、Rider 3.1.0 及官方传递依赖，详见 [开发工具](UNITY_TOOLS.md) |

API 枚举字符串返回旧别名 `NET_Standard_2_0`；安装版官方 XML 将该别名标为弃用，并说明值对应的 `NET_Standard` 为 .NET Standard 2.1。实际编译也引用 `netstandard2.1` targeting pack，详见[测试记录](TESTING.md)。

## Socket 实现与验证边界

`UdpSocket` 提供 `Bind`、`LocalEndPoint`、`SendTo`、`TryReceiveFrom` 和 `Dispose`。一次处理一个 datagram，没有挂起接收、后台线程或托管接收队列。停止调用即停止主动接收；同一把锁协调 I/O 与关闭。调用方拥有数组，endpoint 使用独立快照。每实例固定 64 KiB 暂存，容量不足抛 `MessageSize` 且不写入调用方数组；空 datagram 是成功接收。

开发工程的 30 项 loopback 测试使用真实 Socket，最终全部通过；运行器不依赖测试框架。官方 Pipeline 的传递依赖包含 Unity Test Framework/NUnit，仅属于开发工程。独立审查未发现未解决的 P1/P2 问题，Rider 对三份源码的实际语义检查无错误，仍有已核对的警告与风格建议。Player、其他操作系统/Unity 版本及远程 Git URL 安装尚未验证；IPv6 jumbogram 不支持。

## 验证记录

- Unity 批处理创建工程：退出码 0。
- Unity 批处理编译与包检查：退出码 0，输出 `LXNETCODE_PROJECT_CHECK_PASS`；包路径、Runtime 无 Unity 引用、两次显式资源刷新均通过，临时探针已清理。
- 包已迁入 `Unity/Packages/com.counull.lxnetcode/`，迁移时 13 个文件的 SHA-256 全部一致，保留 `.meta` GUID 和 Socket 任务的在途源码。移除自有包的旧 `file:` 声明，Unity 自动生成 Embedded 锁项；迁移后项目检查实际退出码 0，包路径、Runtime 无 Unity 引用、两次显式刷新及 URP 配置均通过。首次检查因测试脚本类型歧义退出 1，修正后重跑通过，见[测试记录](TESTING.md)。
- 图形初始化修复：新增的管线检查先复现缺失问题（退出码 1），修复后检查通过；Direct3D12 实际渲染 640×360 图像并校验中心测试像素通过，修复进程退出码 0。
- 编辑器布局：真实图形编辑器加载项目本地默认布局，六个常用面板均为 `docked=True`、`maximized=False`，最终校验进程退出码 0。证据和一次校验类型名适配失败见[测试记录](TESTING.md)。
- Unity 的旧 Input Manager 弃用警告仍存在；本次未改变输入系统。
- Git LFS 封面指针的 SHA-256 与工作区 PNG、对应本地 LFS 对象一致；初始化提交 `93c08e1` 推送时已成功上传 1 个 LFS 对象，远程下载仍未验证。
- Socket 首次 loopback：26 通过、3 截断失败；原生探针证明当前 Unity 的小缓冲区 `ReceiveMessageFrom` 返回部分字节但无截断标志。修复为固定单包暂存后检查调用方容量，最终 30 通过、0 失败、0 跳过，退出码 0，IPv4/IPv6 截断与恢复均通过。
- 官方 Unity MCP 实际握手、178 项工具发现、工程状态读取和项目检查通过；Rider 已生成并识别三个 C# 工程，已使用 Rider MCP 修改代码并完成语义检查。详见 [开发工具](UNITY_TOOLS.md) 与 [测试记录](TESTING.md)。
- 独立工程 Git URL 安装、Player 构建、其他 Unity/操作系统：未运行。

首次骨架初始化（`32451b6`）的静态检查已执行：

- 在系统临时目录的隔离工具环境中运行 `skill-creator/scripts/quick_validate.py` 的 `validate_skill`：两项项目技能均返回 `Skill is valid!`。PyYAML 只用于临时校验，没有添加项目依赖。
- 10 份 Markdown 文档中的 12 个相对文件链接全部可解析。
- 14 项 Git 忽略规则检查通过：生成数据被排除，维护文档、技能、包源码、Unity `.meta`、工程配置和包锁文件可纳入 Git。

这些历史静态检查只验证文件格式、链接和忽略规则，不代表已验证技能自动选择或网络行为。当前 Unity 初始化结果以上面的实际执行记录为准。

工程技能配置写入后的静态检查：13 份 Markdown 文档中的 15 个相对文件链接全部可解析；新增的三份 `docs/agents/` 配置均可被 Git 跟踪。没有创建 GitHub Issue 或远程标签。
