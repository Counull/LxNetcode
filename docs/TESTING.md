# 测试与证据

## 当前状态

工程初始化检查已实际运行并通过；Socket 实现与网络测试运行器尚未建立，以下 Socket loopback 仍为待设计批准后的计划，没有网络测试通过结果。

## 工程初始化检查：2026-10-18

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
| 开发工程包锁文件 | 仅自有 Local 包与 Unity builtin 模块，没有 registry/Git 包依赖 |

API 解释同时核对了安装版 `Editor/Data/Managed/UnityEditor.xml`：`NET_Standard` 表示 .NET Standard 2.1，`NET_Standard_2_0` 为弃用别名；编译参数来自实际 `Unity/Library/Bee/artifacts/1900b0aEDbg.dag/LxNetcode.Sockets.rsp`。不能由 Editor 运行时 10.0.10 推断 Socket 模块可使用任意 .NET 10 API。

Git LFS 已执行 `git lfs install --local`。封面转为指针后，工作区 PNG 与对应本地 LFS 对象均匹配指针的 SHA-256 `0f044bbff711a6640d3989c98bba7b267d13818893b699ff6effd23d27ab9790`，长度均为 1,885,982 字节；工作区保留真实图片。本次未迁移历史，也没有把本地验证当作远程 LFS 上传验证。

此外，通过本机 Python 标准库与 Git 执行静态检查：14 份 Markdown 的 20 个相对链接、UPM 清单及本地路径、包依赖来源、7 个资源/目录的 Unity 元数据、13 项 Git 忽略规则，以及临时探针清理均通过。这些检查未安装任何项目依赖。

## Socket loopback 测试计划

只验证模块对外可观察的 Socket 行为，不测试或补写上层协议。

| 行为 | 计划验证的结果 |
| --- | --- |
| 绑定与端口 0 | 查询实际本地地址和系统分配的非零端口，绑定失败可观察 |
| 原始 payload | 按显式目标发送任意字节，接收内容与长度一致，原输入不被修改 |
| 发送方 endpoint | 接收结果保留真实发送端地址和端口 |
| datagram 边界 | 分开发送的数据包由分开的接收返回，不拼接或拆分；不假设 UDP 到达顺序 |
| 空 datagram | 长度 0 的数据包按获批契约返回，不能当作连接结束 |
| 缓冲区边界 | 恰好容纳、缓冲区不足和超大 datagram 均按契约报告；不能静默返回貌似完整的截断内容 |
| 取消或停止接收 | 无来包时的等待能按契约结束，明确是否可以继续接收及取消与来包竞态的结果 |
| 关闭与 Dispose | 等待中的接收结束，重复释放、释放后调用与关闭竞态行为明确且无挂起 |
| 并发与所有权 | 只验证承诺支持的并发方式；调用方缓冲区、返回数据与 endpoint 的所有权和复用规则符合契约 |
| 错误 | 参数、绑定、发送及接收错误按契约可观察，不静默吞掉；发送成功不能作为对端收到的断言 |

每个等待测试设置有限期限并可靠释放 Socket。测试应捕获失败，不依赖固定端口、无限等待、外网或必须有特定到达顺序。截断和取消行为要在目标平台实际运行，不能从 Windows 结果推定其他平台通过。

## 尚未运行的 Unity 与 UPM 验证

- Socket 模块设计批准并实现后，在开发工程中通过本地包运行相应 loopback 测试。
- 在独立消费工程中，通过真实的 Git URL 和固定发布引用安装；确认使用包目录中的代码。
- 在所声明支持的 Player 平台和脚本后端验证相应行为。尚未执行的组合明确标为未运行。

上述项目均未运行。已通过的工程初始化检查只证明本机 Editor 编译、本地包路径及显式资源刷新；不证明 Player 运行、其他 Unity/操作系统、远程 Git URL/LFS 安装、自动文件监听时延或 UDP I/O。

## 执行记录要求

实现后记录实际命令或操作、Unity 版本、API Compatibility Level、系统、构建目标、脚本后端、退出码及测试汇总。把测试计划、实际通过、失败、跳过和环境限制分别记录；若新增测试依赖，先取得作者批准。
