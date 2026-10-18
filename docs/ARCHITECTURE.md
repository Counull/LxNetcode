# 项目组织与 UPM 分发

## 当前组织

```text
LxNetcode/
├── README.md
├── AGENTS.md
├── .gitignore
├── .gitattributes
├── docs/                          维护文档
├── Unity/                         Unity 7000.0.0a10 开发工程
│   ├── Assets/Editor/             工程初始化检查
│   ├── Assets/Settings/           开发工程 URP 资源
│   ├── Assets/Scenes/             仅含默认相机与方向光的基础场景
│   ├── Packages/                  开发工程 manifest 与锁文件
│   │   └── com.counull.lxnetcode/ 内嵌 UPM 源码包与 Runtime 程序集
│   └── ProjectSettings/
└── .agents/
    └── skills/                    项目技能
```

Unity 工程由实际安装的编辑器创建，包已通过本地导入和编译检查。Runtime 实现原始 UDP Socket I/O，行为和验证范围分别见 [SOCKET.md](SOCKET.md) 与 [TESTING.md](TESTING.md)。

可分发源码集中在包目录。`LxNetcode.Sockets.asmdef` 设置 `noEngineReferences: true`，不引用开发工程的场景或脚本。开发工程的 Editor 检查与 loopback 脚本允许使用 Unity API，不随包分发。

## 本地开发与 Git URL 分发

包源码直接位于 `Unity/Packages/com.counull.lxnetcode/`，作为开发工程的 Embedded Package。Unity 自动识别该目录，不再需要在 `Unity/Packages/manifest.json` 中声明自有包的 `file:` 依赖；包锁文件由 Unity 重新解析生成。工程与 Git 分发使用同一份源码，不复制第二份包，不预先构建 DLL。参考 [Unity 7 内嵌包文档](https://docs.unity3d.com/7000.0/Documentation/Manual/upm-embed.html)。

原先包位于仓库根目录的 `Packages/`，开发工程通过 `file:` 引用。迁入工程后保留所有已有 `.meta` 和 GUID；Runtime 的纯 C# 边界仍由 asmdef 与实际程序集引用保证。历史检查结果保留在[测试记录](TESTING.md)，迁移后的编译和显式刷新另行记录；不把显式刷新等同于自动文件监听或远程实时同步。

独立游戏项目未来通过 Unity Package Manager（UPM）的 Git URL 安装源码包。Unity 支持使用 `?path=` 指向仓库中的包子目录，也支持在其后用 `#` 指定 tag 或完整 commit hash。参考 [Unity 7 Git 依赖文档](https://docs.unity3d.com/7000.0/Documentation/Manual/upm-git.html)。

将来的引用形式如下：

```text
https://github.com/Counull/LxNetcode.git?path=/Unity/Packages/com.counull.lxnetcode#<tag-or-full-commit>
```

其中 `<tag-or-full-commit>` 必须替换成实际已推送的引用。消费工程更新引用并让 UPM 重新解析后才取得对应版本；包仓库的 push 不会实时更新已锁定的游戏工程。当前仅验证本地包，没有验证独立工程的 Git URL 安装或创建发布 tag。

Git 依赖由消费工程的 `Packages/manifest.json` 声明；包自身的 `package.json` 不声明 Git URL 依赖。

## 源码程序集与 DLL 的取舍

| 方式 | 当前适用性 |
| --- | --- |
| 仓库内纯 C# 源码，以 UPM 包和独立 asmdef 组织 | 采用；便于学习、审查和调试，符合未来 Git URL 引用目标 |
| 单独构建托管 C# class library 并分发 DLL | 暂无必要；若以后有二进制分发需求，再确认目标框架和 Unity 兼容性 |

源码也可以由独立 `.csproj` 编译和测试，两种组织方式并不互斥。是否增加该工程应由测试需求决定，不为预先产出 DLL 增加构建流程；当前没有手工维护的独立 .NET 构建工程。Rider 集成生成的 `.csproj` 与 solution 仅用于 IDE，属于被 Git 忽略的 Unity 生成数据。

UPM 包不要求使用 Native DLL。源码包没有 Native 插件、第三方依赖或上层网络框架。开发工程包含 Unity 官方 URP 17.7.0、Pipeline 0.8.0-exp.1、Rider Editor 3.1.0 及其依赖，分别用于渲染、官方 MCP 和 IDE 集成；这些依赖不进入 `com.counull.lxnetcode/package.json`，Runtime 程序集也不引用它们。接入与真实验证见[开发工具记录](UNITY_TOOLS.md)。

最初使用命令行创建空工程时没有设置 Render Pipeline Asset，图形界面因此无法渲染。[Unity 官方说明](https://discussions.unity.com/t/the-path-to-a-single-render-pipeline-in-unity-7/1737908)确认 Built-in Render Pipeline 已在 Unity 7 移除。现使用编辑器自带版本的 URP，通过其公开 API 创建最小管线与 Renderer，并保存 Unity 自动生成的 Global Settings 和默认 Volume Profile；没有导入模板教程或示例游戏逻辑。

`Development.unity` 只有默认相机与方向光，供打开工程与后续独立设计测试时使用，不承担 Tick、状态同步或任何网络功能。旧 Input Manager 的弃用警告暂时保留。

编辑器布局存放在被忽略的 `Unity/UserSettings/Layouts/`。本机曾继承 Unity 6 的最大化 Game 布局，已备份后用 Unity 7 自带 `Default.wlt` 恢复，并通过实际窗口状态检查；这项修复不把机器分辨率和个人布局写入 Git。

## 实际兼容基线

当前包 manifest 声明 Unity `7000.0`、`unityRelease: 0a10`，对应实际验证的 7000.0.0a10。开发工程的 Standalone 配置是 .NET Standard 2.1 与 CoreCLR，Runtime 编译使用 C# 9.0；实际结果见[测试记录](TESTING.md)。Editor 运行时版本不代表可任意使用 .NET 10 或未来 C# API。后续 Socket API 选择仍须检查这个 Unity 版本实际提供的接口；其他 Unity 版本尚未验证。参考 [Unity 7 API 兼容性文档](https://docs.unity3d.com/7000.0/Documentation/Manual/dotnet-profile-support.html)。

## Git LFS

`.gitattributes` 对图片、模型、音频、视频和字体等列明的二进制扩展名启用 LFS。源码、Unity YAML 和 `.meta` 保持普通文本；Library、Logs、Temp 等缓存不纳入 Git 或 LFS。

本次执行 `git lfs install --local` 建立仓库本地过滤器和 pre-push hook，并将当前封面转入 LFS；未修改历史提交。克隆后同样执行该命令及 `git lfs pull`。使用 Git URL 的机器也需安装 Git LFS；Git URL 安装和远程 LFS 下载尚待独立消费工程验证。

## Socket 模块

本阶段按作者委托选择同步非阻塞方案：`Bind`、`LocalEndPoint`、`SendTo`、`TryReceiveFrom` 和 `Dispose`。调用方安排收发，不在包内引入后台线程、队列或 Unity 调度；具体生命周期、所有权、错误与截断语义见 [SOCKET.md](SOCKET.md)。后续设计变更仍遵守根目录 `AGENTS.md` 的确认规则及作者保留范围。
