---
name: lxnetcode-socket-workflow
description: Design and implement LxNetcode raw UDP Socket I/O after explicit design approval. Use for Socket API, lifecycle, cancellation, buffer ownership, compatibility, or loopback tests; excludes upper-layer protocol implementation.
---

# LxNetcode Socket 工作流程

用于最小原始 UDP Socket 模块的调查、设计及获批后的实现。遵守仓库根目录 `AGENTS.md`；技能被调用不等于设计或实施已获批准。

## 调查与设计

1. 阅读根目录 `README.md`、`AGENTS.md`、`docs/STATUS.md`，按需查阅 `docs/ARCHITECTURE.md` 和 `docs/TESTING.md`。这些路径均相对于仓库根目录。
2. 检查 Git 工作区与实际工程。读取 `ProjectVersion.txt`、API Compatibility Level、构建目标、脚本后端及测试入口。缺失项标为未建立；本机编辑器和 .NET SDK 不能替代工程证据。
3. 提出小接口方案：绑定、实际本地 endpoint、显式目标发送、单 datagram 接收及发送方 endpoint、停止与释放。同步或异步、线程并发、取消竞态、缓冲区所有权、错误和截断语义都需明确；不预选未经验证的 API。
4. 给出 loopback 外部行为测试及兼容性验证方案。默认没有收包队列；如确需队列，先提出容量及背压或丢弃策略供作者批准。
5. 先取得作者对设计的确认，再修改对应实现。文档初始化、路线图、目录权限或技能调用都不能替代确认。

## 获批后实现与验证

- 按获批契约实现原始 I/O，不解析、序列化或改写 payload；不添加 `AGENTS.md` 中由作者保留的协议或游戏网络功能。
- 包源码作为内嵌 UPM 包位于 `Unity/Packages/com.counull.lxnetcode/`，开发场景与验证脚本放在 `Unity/Assets/`。运行时保持纯 C#，不依赖 UnityEngine；包不得引用验证工程的代码或场景。
- 不擅自引入依赖、Native 插件或上层网络框架。不为了 DLL 分发增加构建流程。
- 保留 datagram 边界和发送方 endpoint。发送成功只表示本地发送操作完成，不能解释为对端收到或提供可靠、有序传输。
- 实际运行适用的 loopback 测试，覆盖边界、取消、关闭及释放；在承诺支持的平台验证截断行为。没有可用运行环境时明确报告未运行。
- Git URL 导入使用真实存在的包路径和发布引用；本地包编译通过不能替代 Git URL 消费工程验证。
- 更新相关维护文档，报告公开接口、关键语义、改动路径、实际命令与结果、剩余限制。提交与推送遵守根目录规则。

讨论上层网络时，只解释概念、权衡、不变量和测试思路，不生成可直接并入项目的上层实现。
