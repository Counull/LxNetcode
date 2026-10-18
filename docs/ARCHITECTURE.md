# 项目组织与 UPM 分发

## 已确认的组织方向

```text
LxNetcode/
├── README.md
├── AGENTS.md
├── .gitignore
├── docs/                          维护文档
├── Unity/                         开发与验证工程预留目录
├── Packages/
│   └── com.counull.lxnetcode/       UPM 源码包预留目录
└── .agents/
    └── skills/                    项目技能
```

当前各预留目录通过 README 纳入 Git，没有伪造 Unity 工程、包 manifest 或运行时实现。

可分发源码集中在包目录，Unity 开发工程及以后的实际游戏项目消费同一份包。包不依赖开发工程中的场景或脚本。Socket 运行时保持纯 C#，后续以独立 asmdef 组织并禁用 UnityEngine 引用；具体程序集结构仍需随 Socket 设计确认。

## 分发方式

目标是通过 Unity Package Manager（UPM）的 Git URL 安装源码包。Unity 支持使用 `?path=` 指向仓库中的包子目录，也支持在其后用 `#` 指定 tag 或完整 commit hash。参考 [Unity Git 依赖文档](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-git.html)。

将来的引用形式如下，其中 `<tag-or-full-commit>` 必须替换成实际已发布的引用：

```text
https://github.com/Counull/LxNetcode.git?path=/Packages/com.counull.lxnetcode#<tag-or-full-commit>
```

这只是未来的 URL 形式；当前本地只有预留目录，尚未建立可安装包，也未创建或验证发布 tag，未验证 Git URL 导入。

Git 依赖由消费工程的 `Packages/manifest.json` 声明。日常本地开发将由验证工程引用仓库中的本地包目录，避免维护第二份源码；接入时记录实际路径并验证。不要在包自身的 `package.json` 中声明 Git URL 依赖。

## 源码程序集与 DLL 的取舍

| 方式 | 当前适用性 |
| --- | --- |
| 仓库内纯 C# 源码，以 UPM 包和独立 asmdef 组织 | 推荐；便于学习、审查和调试，符合未来 Git URL 引用目标 |
| 单独构建托管 C# class library 并分发 DLL | 暂无必要；若以后有二进制分发需求，再确认目标框架和 Unity 兼容性 |

源码也可以由独立 `.csproj` 编译和测试，两种组织方式并不互斥。是否增加该工程应由测试需求决定，不为预先产出 DLL 增加构建流程。

UPM 包不要求使用 Native DLL。当前不引入 Native 插件、第三方依赖或上层网络框架。

## 包初始化的前置条件

选择实际开发工程的 Unity 版本，检查 API Compatibility Level、构建目标和脚本后端，再确定支持的 API、语言特性及包的最低 Unity 版本。包 manifest 和 Runtime/Tests 程序集按该基线创建；当前先保留目录。

后续包布局参考 [Unity UPM 包布局](https://docs.unity3d.com/6000.3/Documentation/Manual/cus-layout.html)，manifest 字段参考 [Unity 包 manifest](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-manifestPkg.html)。引用这些文档不代表已选用或验证 Unity 6000.3。

## Socket 设计仍待批准

实现前需提交最小设计，覆盖程序集组织、接口、同步或异步收发、线程模型、取消与关闭、缓冲区所有权、Socket 错误及截断语义。设计不能包含作者保留的上层网络实现范围；完整边界见根目录 `AGENTS.md`。
