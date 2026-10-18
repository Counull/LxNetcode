# 项目状态

更新日期：2026-10-18。

## 当前阶段

Unity 开发工程、UPM 源码包骨架、Git LFS、维护文档与项目技能已初始化。最初的批处理检查遗漏了渲染管线与图形界面；现已补齐 URP，并通过实际 GPU 渲染和编辑器布局检查。Socket 设计未批准；没有 Socket 实现、网络测试或发布 tag。

## 初始化内容

- `docs/`：维护文档及其索引。
- `Unity/`：实际开发与验证工程，通过本地 `file:` 路径消费仓库内的包源码。
- 开发工程 Graphics 使用 URP 17.7.0，基础场景为 `Assets/Scenes/Development.unity`（默认相机、方向光）；本机旧布局已备份并恢复，布局不纳入 Git。
- `Packages/com.counull.lxnetcode/`：版本为 `0.0.1-preview.1` 的包 manifest，以及禁止 UnityEngine 引用的 `LxNetcode.Sockets` asmdef；Runtime 仅含程序集版本元数据。
- `.gitattributes`：二进制资源使用 Git LFS，源码、Unity YAML 和 `.meta` 保持普通文本。已执行 `git lfs install --local`，当前封面转为 LFS 指针；未迁移历史。
- `AGENTS.md`：持久化项目目标、AI 范围与设计确认要求。
- `.agents/skills/`：Socket 设计/实现流程和只读审查技能。
- `docs/agents/`：Matt Pocock 工程技能配置，使用 GitHub Issues（Counull/LxNetcode）、默认 triage 标签及单上下文领域文档布局。
- README 明确作者与 AI 的职责；`.gitignore` 排除 Unity 和 .NET 的生成数据，保留维护文档与项目技能。

## 已确认的方向

- 维护文档与项目技能随仓库提交到 Git。
- `docs/` 与 Unity 工程目录分别组织。
- 未来实际游戏项目通过 Git URL 引用 UPM 包；开发工程消费同一份包源码。
- AI 只实现经批准的原始 UDP Socket I/O；当前初始化没有授权网络实现。

## 实际工程配置

| 项目 | 初始化时的实际值 |
| --- | --- |
| Unity | 7000.0.0a10，revision `4b5f18082ea8` |
| 当前构建目标 | StandaloneWindows64 |
| Standalone API Compatibility Level | .NET Standard 2.1，枚举值 6 |
| Standalone 脚本后端 | CoreCLR |
| Editor 运行时 | 10.0.10；不据此推定可使用 .NET 10 API |
| Runtime 编译器语言版本 | C# 9.0（实际编译响应文件） |
| Unity 序列化 | Force Text |
| 包来源 | Local，解析至仓库 `Packages/com.counull.lxnetcode/` |
| 渲染管线 | Unity 官方 URP 17.7.0，仅用于开发工程 |
| 包依赖 | 自有包无依赖；开发工程含本地包、Unity 内建模块及 URP 的 Unity 官方依赖，Searcher 4.9.5 来自 Unity registry |

API 枚举字符串返回旧别名 `NET_Standard_2_0`；安装版官方 XML 将该别名标为弃用，并说明值对应的 `NET_Standard` 为 .NET Standard 2.1。实际编译也引用 `netstandard2.1` targeting pack，详见[测试记录](TESTING.md)。

## 待设计与验证

Socket 接口、收发模型、线程模型、取消/关闭、缓冲区所有权、错误和截断语义仍待设计批准。测试运行方式与依赖随设计确认，不预装第三方测试包。工程初始化不构成 Socket 实现授权。

## 验证记录

- Unity 批处理创建工程：退出码 0。
- Unity 批处理编译与包检查：退出码 0，输出 `LXNETCODE_PROJECT_CHECK_PASS`；包路径、Runtime 无 Unity 引用、两次显式资源刷新均通过，临时探针已清理。
- 图形初始化修复：新增的管线检查先复现缺失问题（退出码 1），修复后检查通过；Direct3D12 实际渲染 640×360 图像并校验中心测试像素通过，修复进程退出码 0。
- 编辑器布局：真实图形编辑器加载项目本地默认布局，六个常用面板均为 `docked=True`、`maximized=False`，最终校验进程退出码 0。证据和一次校验类型名适配失败见[测试记录](TESTING.md)。
- Unity 的旧 Input Manager 弃用警告仍存在；本次未改变输入系统。
- Git LFS 封面指针的 SHA-256 与工作区 PNG、对应本地 LFS 对象一致；初始化提交 `93c08e1` 推送时已成功上传 1 个 LFS 对象，远程下载仍未验证。
- 独立工程 Git URL 安装、Player 构建、其他 Unity/操作系统及 UDP loopback：未运行。

首次骨架初始化（`32451b6`）的静态检查已执行：

- 在系统临时目录的隔离工具环境中运行 `skill-creator/scripts/quick_validate.py` 的 `validate_skill`：两项项目技能均返回 `Skill is valid!`。PyYAML 只用于临时校验，没有添加项目依赖。
- 10 份 Markdown 文档中的 12 个相对文件链接全部可解析。
- 14 项 Git 忽略规则检查通过：生成数据被排除，维护文档、技能、包源码、Unity `.meta`、工程配置和包锁文件可纳入 Git。

这些历史静态检查只验证文件格式、链接和忽略规则，不代表已验证技能自动选择或网络行为。当前 Unity 初始化结果以上面的实际执行记录为准。

工程技能配置写入后的静态检查：13 份 Markdown 文档中的 15 个相对文件链接全部可解析；新增的三份 `docs/agents/` 配置均可被 Git 跟踪。没有创建 GitHub Issue 或远程标签。
