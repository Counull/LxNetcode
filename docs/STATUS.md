# 项目状态

更新日期：2026-10-18。

## 当前阶段

仓库骨架初始化。Socket 设计未批准；没有 Socket 实现、可执行的网络测试或发布版本。

## 初始化内容

- `docs/`：维护文档及其索引。
- `Unity/`：开发与验证工程的预留目录，通过 README 纳入 Git。
- `Packages/com.counull.lxnetcode/`：未来 UPM 源码包的预留目录。
- `AGENTS.md`：持久化项目目标、AI 范围与设计确认要求。
- `.agents/skills/`：Socket 设计/实现流程和只读审查技能。
- README 明确作者与 AI 的职责；`.gitignore` 排除 Unity 和 .NET 的生成数据，保留维护文档与项目技能。

## 已确认的方向

- 维护文档与项目技能随仓库提交到 Git。
- `docs/` 与 Unity 工程目录分别组织。
- 未来实际游戏项目通过 Git URL 引用 UPM 包；开发工程消费同一份包源码。
- AI 只实现经批准的原始 UDP Socket I/O；当前初始化没有授权网络实现。

## 待确认与待建立

| 项目 | 状态 |
| --- | --- |
| Unity 工程版本 | 待选择；仓库内没有 `ProjectVersion.txt` |
| API Compatibility Level | 待工程创建后读取实际配置 |
| 构建目标与脚本后端 | 待确定 |
| UPM manifest、最低 Unity 版本 | 待确认兼容基线后建立 |
| Socket 程序集、接口与收发模型 | 待设计及批准 |
| 测试运行方式和依赖 | 待设计及批准，不预装第三方测试包 |
| Git URL 安装及独立游戏工程集成 | 未运行 |

本机发现的编辑器目录为 `6000.3.21f1` 与 `6000.7.0b2`；它们只是本机安装线索，不代表项目已选择或验证的版本。本机 .NET SDK 也不能代替 Unity 兼容性验证。

## 验证记录

网络集成测试、Unity 编译、Player 构建及 UPM 导入：未运行，相关工程与实现尚未建立。

初始化静态检查已执行：

- 在系统临时目录的隔离工具环境中运行 `skill-creator/scripts/quick_validate.py` 的 `validate_skill`：两项项目技能均返回 `Skill is valid!`。PyYAML 只用于临时校验，没有添加项目依赖。
- 10 份 Markdown 文档中的 12 个相对文件链接全部可解析。
- 14 项 Git 忽略规则检查通过：生成数据被排除，维护文档、技能、包源码、Unity `.meta`、工程配置和包锁文件可纳入 Git。

这些是文件格式、链接和忽略规则检查，不代表已验证技能自动选择、Unity 编译、Player 运行、UPM 安装或 UDP I/O。
