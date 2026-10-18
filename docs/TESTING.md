# 测试与证据

## 当前状态

工程初始化检查已实际运行并通过。初次检查遗漏的渲染管线和图形界面已补查并修复。Socket 首次 29 项为 26 通过、3 截断失败；修复并增加大包验证后，最终 **30 通过、0 失败、0 跳过，Unity 退出码 0**。历史失败和当前证据分别记录如下。

## 首次工程初始化检查：2026-10-18

环境：Windows、Unity 7000.0.0a10（`4b5f18082ea8`），开发工程 `E:\LxNetcode\Unity`。通过真实 Unity CLI 创建工程，退出码 0；随后实际运行以下 PowerShell 检查：

```powershell
$unityExe = 'E:\Env\Unity\7000.0.0a10\Editor\Unity.exe'
$unityCheckProcess = Start-Process -FilePath $unityExe -ArgumentList @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', 'E:\LxNetcode\Unity',
    '-executeMethod', 'LxNetcode.Development.LxNetcodeProjectCheck.Run',
    '-logFile', 'E:\LxNetcode\Unity\Logs\project-check.log'
) -WindowStyle Hidden -Wait -PassThru
$unityCheckProcess.ExitCode
```

最终运行退出码 **0**，日志输出 `LXNETCODE_PROJECT_CHECK_PASS`，没有 C# 编译错误或警告。检查代码位于 `Unity/Assets/Editor/LxNetcodeProjectCheck.cs`，报告写入被忽略的 `Unity/Logs/project-check.json`。

| 检查 | 实际结果 |
| --- | --- |
| Unity 注册的包来源/版本 | Local / `0.0.1-preview.1` |
| 包解析路径 | `E:\LxNetcode\Packages\com.counull.lxnetcode` |
| Runtime 编译与加载 | `LxNetcode.Sockets` 已加载；程序集引用中没有 UnityEngine/UnityEditor |
| 本地源码刷新 | 包目录临时文本资源写入两种不同内容，两次显式同步刷新均通过 Unity TextAsset 导入结果校验；探针及 `.meta` 已清理 |
| 当前构建目标 | StandaloneWindows64 |
| Standalone API Compatibility Level | 值 6，返回旧别名 `NET_Standard_2_0`，实际为 .NET Standard 2.1 |
| Standalone 脚本后端 | CoreCLR |
| Editor 运行时 | 10.0.10 |
| Runtime 编译参数 | `-langversion:9.0`，引用 `netstandard2.1` targeting pack，没有 UnityEngine/UnityEditor 引用 |
| 当时的开发工程包锁文件 | 仅自有 Local 包与 Unity builtin 模块；后续 URP 修复已增加下文列明的 Unity 官方依赖 |

API 解释同时核对了安装版 `Editor/Data/Managed/UnityEditor.xml`：`NET_Standard` 表示 .NET Standard 2.1，`NET_Standard_2_0` 为弃用别名；编译参数来自实际 `Unity/Library/Bee/artifacts/1900b0aEDbg.dag/LxNetcode.Sockets.rsp`。不能由 Editor 运行时 10.0.10 推断 Socket 模块可使用任意 .NET 10 API。

Git LFS 已执行 `git lfs install --local`。封面转为指针后，工作区 PNG 与对应本地 LFS 对象均匹配指针的 SHA-256 `0f044bbff711a6640d3989c98bba7b267d13818893b699ff6effd23d27ab9790`，长度均为 1,885,982 字节；工作区保留真实图片。本次未迁移历史，也没有把本地验证当作远程 LFS 上传验证。

此外，通过本机 Python 标准库与 Git 执行静态检查：14 份 Markdown 的 20 个相对链接、UPM 清单及本地路径、包依赖来源、7 个资源/目录的 Unity 元数据、13 项 Git 忽略规则，以及临时探针清理均通过。这些检查未安装任何项目依赖。

## 图形初始化修复：2026-10-18

用户截图暴露了首次检查遗漏的两项问题：Game 最大化且 Inspector/Console 浮动，以及 Game 显示 `Your project has no render pipeline asset`。日志确认编辑器继承了全局 `default-6000.dwlt`，Graphics 中的管线为空。首次批处理通过不能作为图形初始化完整的证据。

保存并关闭 Unity 后，将原布局与工程配置备份到本机忽略目录 `Unity/UserSettings/InitializationRepair-20261018T143742Z/`。开发工程添加编辑器自带的 URP 17.7.0，并通过 URP 公开 API 生成 Renderer、管线及 Unity 自动建立的 Global Settings/Volume Profile；`Development.unity` 仅保存默认相机和方向光。自有源码包未改动，仍无 UnityEngine 或包依赖。

依赖解析实际增加了 Unity builtin 的 Burst 2.0.0、Collections 6.7.0、Graph Authoring 1.0.0、Profiling Core 1.0.3、SRP Core/URP/URP Config/Shader Graph 17.7.0，以及 Unity registry 的 Searcher 4.9.5。没有增加上层网络库或第三方直接依赖。

| 实际检查 | 结果 |
| --- | --- |
| 在修复前运行新增的管线非空检查 | 退出码 1，捕获 `Development project has no render pipeline asset.`，日志 `repair-baseline.log` |
| 使用 GPU 的一次性初始化与渲染检查 | 退出码 0；`LXNETCODE_RENDER_PASS`、`LXNETCODE_PROJECT_CHECK_PASS`、`LXNETCODE_GRAPHICS_REPAIR_PASS` |
| 渲染结果 | Direct3D12，640×360 PNG；中心像素 `RGBA(0.149, 0.651, 0.851, 1.000)` 符合临时青色立方体，已实际查看图像；测试物体和材质随后清理，未保存到场景 |
| 本地默认布局 | 从安装版 `Default.wlt` 恢复；真实图形编辑器日志确认加载 `UserSettings/Layouts/default-7000.dwlt` |
| 首次布局探针 | 退出码 1：探针沿用了旧类型名 `SceneHierarchyWindow`；实际 Unity 7 类型为 `Unity.Hierarchy.Editor.HierarchyWindow`，不是面板缺失 |
| 最终布局检查 | 退出码 0，`LXNETCODE_LAYOUT_PASS`；Scene、Game、Hierarchy、Inspector、Project、Console 均实际存在、`docked=True`、`maximized=False` |
| 清理临时脚本后的最终编译与包检查 | 退出码 0，`project-check-final.log` 输出 `LXNETCODE_PROJECT_CHECK_PASS`；保留的检查代码已实际重新编译 |

两次成功的临时检查所用参数分别为：

```text
-batchmode -quit -projectPath E:\LxNetcode\Unity -executeMethod LxNetcode.Development.LxNetcodeInitializationRepair.SetupAndCheck -logFile E:\LxNetcode\Unity\Logs\graphics-repair.log
-projectPath E:\LxNetcode\Unity -executeMethod LxNetcode.Development.LxNetcodeInitializationRepair.VerifyLayoutAndExit -logFile E:\LxNetcode\Unity\Logs\layout-repair-final.log
```

第二项是启动后检查并自动退出的真实图形编辑器实例，两项均未使用 `-nographics`。一次性修复脚本已从 `Assets` 清理，仅在本机 `Unity/Logs/initialization-repair/` 保留源文件用于审计；以上是实际执行记录，不作为日常可直接运行的命令。日常保留的 `LxNetcodeProjectCheck.Run` 已增加渲染管线非空检查，其命令仍见上方代码块。

渲染图 `Unity/Logs/render-check.png`、窗口状态 `Unity/Logs/layout-check.txt` 和日志均在本机忽略目录。当前管线实际解析为 `Assets/Settings/LxNetcode_RenderPipeline.asset`、类型 `UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset`；Runtime 无 Unity 引用和两次本地包刷新再次通过。旧 Input Manager 的弃用警告仍存在。本次没有验证 Player 构建或 Socket 行为，也不把程序读取的窗口状态当作人工桌面视觉验收。

最终静态检查也已通过：14 份 Markdown 的 21 个相对链接、14 个资源/目录的唯一 Unity GUID、Graphics 到管线的引用、开发场景仅含默认相机与方向光、临时脚本与包刷新探针已清理，以及日志/个人布局/备份的 Git 忽略规则。`Packages/com.counull.lxnetcode/` 没有 diff。

## 内嵌包迁移检查：2026-10-18

包从仓库根目录的 `Packages/com.counull.lxnetcode/` 迁入 `Unity/Packages/com.counull.lxnetcode/`。迁移前后 13 个文件的 SHA-256 全部一致，包括 Socket 任务刚写入的两份源码与 `.meta`；随后仅为迁移更新包 README 和 `package.json` 的 `repository.directory`，其余 11 个文件内容保持一致。旧目录已移除，未保留第二份包源码。

开发工程的 manifest 移除旧的自有包 `file:` 依赖，保留其余依赖。初始化检查改为要求 `PackageSource.Embedded` 和工程内的包路径；Unity 实际重新解析锁文件为 `version: file:com.counull.lxnetcode`、`source: embedded`。

实际运行以下 PowerShell 命令（Windows、Unity 7000.0.0a10、StandaloneWindows64、CoreCLR、.NET Standard 2.1）：

```powershell
$unityMigrationCheck = Start-Process -FilePath 'E:\Env\Unity\7000.0.0a10\Editor\Unity.exe' -ArgumentList @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', 'E:\LxNetcode\Unity',
    '-executeMethod', 'LxNetcode.Development.LxNetcodeProjectCheck.Run',
    '-logFile', 'E:\LxNetcode\Unity\Logs\embedded-package-check-final.log'
) -WindowStyle Hidden -Wait -PassThru
$unityMigrationCheck.ExitCode
```

首次同方法检查退出码 **1**（`embedded-package-check.log`）：新加入的 loopback 脚本中 `UdpReceiveResult` 与 BCL 同名类型产生 CS0104，未执行到项目检查。Socket 任务为该类型添加显式别名后重跑，最终退出码 **0**，日志输出 `LXNETCODE_PROJECT_CHECK_PASS`。实际报告确认来源为 Embedded、路径为 `E:\LxNetcode\Unity\Packages\com.counull.lxnetcode`、Runtime 程序集无 UnityEngine/UnityEditor 引用、两次显式资源刷新通过且临时探针已清理，URP 配置有效。

本节验证包迁移及当时的编译结果；没有执行 loopback 方法，Socket 行为由其独立测试记录说明。以上早期 Local 包路径与结果保留为历史证据；当前路径和来源以本节为准。独立消费工程的远程 Git URL 安装仍未运行。

## Socket loopback 覆盖范围

只验证模块对外可观察的 Socket 行为，不测试或补写上层协议。

| 行为 | 外部行为断言 |
| --- | --- |
| 绑定与端口 0 | 查询实际本地地址和系统分配的非零端口，绑定失败可观察 |
| 原始 payload | 按显式目标发送任意字节，接收内容与长度一致，原输入不被修改 |
| 发送方 endpoint | 接收结果保留真实发送端地址和端口 |
| datagram 边界 | 分开发送的数据包由分开的接收返回，不拼接或拆分；不假设 UDP 到达顺序 |
| 空 datagram | 长度 0 的数据包按获批契约返回，不能当作连接结束 |
| 缓冲区边界 | 恰好容纳、缓冲区不足和超大 datagram 均按契约报告；不能静默返回貌似完整的截断内容 |
| 停止接收 | 无包轮询立即返回；不继续调用即停止主动接收，随后能恢复读取；没有挂起等待或 CancellationToken |
| 关闭与 Dispose | 重复释放、释放后调用、端口释放及并发 I/O 关闭竞态行为明确且无挂起 |
| 并发与所有权 | 只验证承诺支持的并发方式；调用方缓冲区、返回数据与 endpoint 的所有权和复用规则符合契约 |
| 错误 | 参数、绑定、发送及接收错误按契约可观察，不静默吞掉；发送成功不能作为对端收到的断言 |

每个等待测试设置有限期限并可靠释放 Socket；不依赖固定端口、外网或特定到达顺序。测试代码位于 `Unity/Assets/Editor/LxNetcodeSocketLoopback.cs`，不会随 UPM 包分发。截断和关闭行为须在目标平台实际运行，不能从 Windows 结果推定其他平台通过。

## Socket 首次运行与截断修复：2026-10-18

首次运行 `LxNetcode.Development.LxNetcodeSocketLoopback.Run`，Unity 进程退出码 **1**；项目检查通过，29 项 Socket 用例中 **26 通过、3 失败、0 跳过**。失败均为 IPv4/IPv6 小缓冲区截断返回成功；失败前的报告保留在本机忽略目录 `Unity/Logs/socket-loopback-before-truncation-fix.json` 和同名 `.log`。

独立原生 Socket 探针实际退出码 **0**，记录于 `Unity/Logs/socket-truncation-probe.log`：IPv4 和 IPv6 发送 513 字节、使用 8 字节容量时，`ReceiveMessageFrom` 返回 8 且 flags=None；`ReceiveFrom` 则抛 MessageSize。使用 65,536 字节容量能完整收到 513 字节。该结果定位到实际 Editor CoreCLR API 行为，不能只靠参考 API 的存在推断截断可靠。临时探针及其 `.meta` 已清理。

修复使用每个 Socket 固定 65,536 字节暂存区接收单个普通 UDP datagram，再检查调用方容量；不足时消费该包、抛 MessageSize、保持调用方数组不变。增加一项 65000 字节完整收包/小缓冲区错误/后续恢复测试，并在既有截断用例加入数组不变断言，当前共 30 项。

## Socket 最终实际运行：2026-10-18

在官方 Pipeline/Rider 集成安装完成、旧 Editor 退出后执行以下命令：

```powershell
$socketFinalProcess = Start-Process -FilePath 'E:\Env\Unity\7000.0.0a10\Editor\Unity.exe' -ArgumentList @(
    '-batchmode', '-nographics', '-quit',
    '-projectPath', 'E:\LxNetcode\Unity',
    '-executeMethod', 'LxNetcode.Development.LxNetcodeSocketLoopback.Run',
    '-logFile', 'E:\LxNetcode\Unity\Logs\socket-loopback-final.log'
) -WindowStyle Hidden -Wait -PassThru
$socketFinalProcess.ExitCode
```

实际退出码 **0**，日志同时包含 `LXNETCODE_PROJECT_CHECK_PASS` 和 `LXNETCODE_SOCKET_LOOPBACK_PASS`。报告 `Unity/Logs/socket-loopback.json` 完成于 `2026-10-18T15:47:36.7026417Z`（北京时间 23:47），记录 **30 通过、0 失败、0 跳过**；无 C# 编译错误。日志及逐项结果保留在本机忽略目录。

| 环境/覆盖 | 实际结果 |
| --- | --- |
| 系统与 Editor | Windows 11 10.0.26300，Unity 7000.0.0a10，Editor 运行时 10.0.10 |
| 配置 | StandaloneWindows64、CoreCLR、API Compatibility Level 值 6（.NET Standard 2.1） |
| 绑定、所有权、错误 | 端口 0、通配绑定、endpoint 独立快照、参数错误不消费包、绑定冲突和过大发送均通过 |
| 原始收发 | 与原生 Socket 互通、IPv4/IPv6 双向字节和 endpoint、datagram 边界、空 datagram、offset 和数组复用均通过 |
| 截断边界 | IPv4/IPv6 恰好容纳、小 1 字节和明显不足时 MessageSize、消费后恢复、调用方数组不变均通过 |
| 大包 | 65000 字节原样收取、同大小包进入小区间时报错、后续正常收包通过 |
| 停止与关闭 | 无包轮询、停止后恢复、重复 Dispose、释放后拒绝访问、端口重绑和重复生命周期通过 |
| 并发 | 同实例并发发送、并发接收保留各发送方、I/O 与 Dispose 竞争且工作线程有界结束均通过 |
| 依赖边界 | 实际加载的 Runtime 程序集没有 UnityEngine/UnityEditor 引用；自有包 dependencies 为空 |

运行器本身不依赖测试框架；本次官方开发工具接入带入的 Unity Test Framework/NUnit、Newtonsoft Json、Mono.Cecil 等仅在开发工程中，具体版本与验证见 [UNITY_TOOLS.md](UNITY_TOOLS.md)。

## 独立审查与 Rider 检查：2026-10-18

按项目 `lxnetcode-socket-review` 技能只读检查当前 diff、未跟踪 Runtime/测试源码、包配置与真实失败日志，未发现新的 P1/P2 实现问题，也未发现上层协议、连接状态、后台循环或托管收包队列。审查建议补足 MessageSize 后数组不变断言，已加入既有用例并在上述最终运行中通过。静态审查与实际执行分开记录。

Rider MCP 最初返回“文件未包含在任何项目中”，该次不算分析通过。补齐官方 Rider 集成并生成工程后，`get_solution_projects` 实际识别三个 C# 项目；`lint_files` 完整分析 `UdpSocket.cs`、`UdpReceiveResult.cs`、`LxNetcodeSocketLoopback.cs`，无 ERROR、无超时或未分析文件。仍有 **40 WARNING、187 WEAK WARNING**：包括命名空间与物理包路径的建议、测试 lambda 捕获释放对象、刻意触发 getter/绑定的未使用变量及命名/var 风格建议。已核对同步异常断言、任务等待与释放顺序；没有为清除风格提示改变公开命名空间或扩大实现。完整返回保留在 `Unity/Logs/rider-socket-analysis.json`。

最终静态检查通过：16 份 Markdown 的 42 个相对链接、17 个唯一 Unity GUID、Runtime asmdef 禁止 Unity 引用、自有包无依赖、旧包目录和临时探针已移除、生成的 csproj/sln 与日志被忽略。`git diff --check` 通过；只有 Git 的 CRLF→LF 提示，不是内容检查失败。

## 尚未运行的 Unity 与 UPM 验证

- 在独立消费工程中，通过真实的 Git URL 和固定发布引用安装；确认使用包目录中的代码。
- 在所声明支持的 Player 平台和脚本后端验证相应行为。尚未执行的组合明确标为未运行。

上述项目均未运行。工程检查覆盖本机 Editor 编译、本地包、显式资源刷新、Direct3D12 渲染及编辑器窗口状态；网络行为须按单独的 loopback 结果判断。这些记录不证明 Player 运行、其他 Unity/操作系统、远程 Git URL/LFS 安装或自动文件监听时延。

## 执行记录要求

实现后记录实际命令或操作、Unity 版本、API Compatibility Level、系统、构建目标、脚本后端、退出码及测试汇总。把测试计划、实际通过、失败、跳过和环境限制分别记录；若新增测试依赖，先取得作者批准。
