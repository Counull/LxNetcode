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
│   ├── Packages/                  开发工程 manifest 与锁文件
│   └── ProjectSettings/
├── Packages/
│   └── com.counull.lxnetcode/       UPM manifest 与 Runtime 程序集骨架
└── .agents/
    └── skills/                    项目技能
```

Unity 工程由实际安装的编辑器创建，包已通过本地导入和编译检查。目前 Runtime 只有程序集版本元数据，没有 Socket 实现。

可分发源码集中在包目录。`LxNetcode.Sockets.asmdef` 设置 `noEngineReferences: true`，不引用开发工程的场景或脚本。开发工程的 Editor 检查脚本允许使用 Unity API，不随包分发。Socket 接口与实现仍待设计批准。

## 本地开发与 Git URL 分发

开发工程 `Unity/Packages/manifest.json` 中的引用为：

```json
"com.counull.lxnetcode": "file:../../Packages/com.counull.lxnetcode"
```

路径相对于开发工程的 `Packages/` 目录，解析到仓库中的唯一源码。已实际验证修改文本资源后，Unity 的两次显式 `AssetDatabase.Refresh` 均能导入新内容。日常可以在编辑器刷新后查看改动；此检查没有验证自动文件监听的时延，也没有把 Git 依赖变成实时同步。

独立游戏项目未来通过 Unity Package Manager（UPM）的 Git URL 安装源码包。Unity 支持使用 `?path=` 指向仓库中的包子目录，也支持在其后用 `#` 指定 tag 或完整 commit hash。参考 [Unity 7 Git 依赖文档](https://docs.unity3d.com/7000.0/Documentation/Manual/upm-git.html)。

将来的引用形式如下：

```text
https://github.com/Counull/LxNetcode.git?path=/Packages/com.counull.lxnetcode#<tag-or-full-commit>
```

其中 `<tag-or-full-commit>` 必须替换成实际已推送的引用。消费工程更新引用并让 UPM 重新解析后才取得对应版本；包仓库的 push 不会实时更新已锁定的游戏工程。当前仅验证本地包，没有验证独立工程的 Git URL 安装或创建发布 tag。

Git 依赖由消费工程的 `Packages/manifest.json` 声明；包自身的 `package.json` 不声明 Git URL 依赖。

## 源码程序集与 DLL 的取舍

| 方式 | 当前适用性 |
| --- | --- |
| 仓库内纯 C# 源码，以 UPM 包和独立 asmdef 组织 | 采用；便于学习、审查和调试，符合未来 Git URL 引用目标 |
| 单独构建托管 C# class library 并分发 DLL | 暂无必要；若以后有二进制分发需求，再确认目标框架和 Unity 兼容性 |

源码也可以由独立 `.csproj` 编译和测试，两种组织方式并不互斥。是否增加该工程应由测试需求决定，不为预先产出 DLL 增加构建流程；当前没有额外 .NET 工程。

UPM 包不要求使用 Native DLL。当前没有 Native 插件、第三方依赖或上层网络框架；开发工程仅保留 Unity 自动生成的内建模块和自有本地包。

## 实际兼容基线

当前包 manifest 声明 Unity `7000.0`、`unityRelease: 0a10`，对应实际验证的 7000.0.0a10。开发工程的 Standalone 配置是 .NET Standard 2.1 与 CoreCLR，Runtime 编译使用 C# 9.0；实际结果见[测试记录](TESTING.md)。Editor 运行时版本不代表可任意使用 .NET 10 或未来 C# API。后续 Socket API 选择仍须检查这个 Unity 版本实际提供的接口；其他 Unity 版本尚未验证。参考 [Unity 7 API 兼容性文档](https://docs.unity3d.com/7000.0/Documentation/Manual/dotnet-profile-support.html)。

## Git LFS

`.gitattributes` 对图片、模型、音频、视频和字体等列明的二进制扩展名启用 LFS。源码、Unity YAML 和 `.meta` 保持普通文本；Library、Logs、Temp 等缓存不纳入 Git 或 LFS。

本次执行 `git lfs install --local` 建立仓库本地过滤器和 pre-push hook，并将当前封面转入 LFS；未修改历史提交。克隆后同样执行该命令及 `git lfs pull`。使用 Git URL 的机器也需安装 Git LFS；Git URL 安装和远程 LFS 下载尚待独立消费工程验证。

## Socket 设计仍待批准

实现前需提交最小设计，覆盖程序集组织、接口、同步或异步收发、线程模型、取消与关闭、缓冲区所有权、Socket 错误及截断语义。设计不能包含作者保留的上层网络实现范围；完整边界见根目录 `AGENTS.md`。
