---
name: lxnetcode-socket-review
description: Review LxNetcode Socket changes for scope violations, UDP I/O correctness, lifecycle races, ownership, truncation, and real test evidence. Use for a current diff or explicit base; report findings before fixes without rewriting the module.
---

# LxNetcode Socket 审查

先报告问题与严重度，不修改被审查代码、不扩展功能。遵守根目录 `AGENTS.md` 的范围约束；以下路径均相对于仓库根目录。

## 审查输入

- 阅读 `README.md`、`AGENTS.md`、`docs/STATUS.md` 和与改动相关的已批准设计、测试记录。
- 用 Git 确定实际审查范围：当前已暂存与未暂存 diff，或作者明确指定的基线。标明未跟踪文件与未覆盖的范围；没有代码 diff 时如实说明。
- 读取实际 Unity/构建配置及受影响调用方。不能把测试计划、注释、README 或旧记录当成本次测试通过的证据。

## 优先检查

1. 越界实现：包格式、ACK、可靠性、重传、排序、连接管理、游戏同步及其他作者保留的上层功能。
2. 依赖与分发：不必要的 Native DLL、未经批准的第三方库或 UnityEngine 依赖；包是否引用 Unity 验证工程内容。
3. 生命周期和并发：正在等待的接收如何取消和关闭，Dispose 是否幂等，关闭竞态、并发发送/接收及释放后调用是否符合公开契约。
4. 数据与错误：datagram 边界、字节和长度、发送方 endpoint、空 datagram、缓冲区所有权是否保留；错误或截断是否被静默掩盖；是否存在无界积压。
5. 测试质量：loopback 是否验证外部行为，是否存在无限等待或固定端口依赖；是否实际覆盖取消、关闭、边界及承诺的并发方式。
6. 证据范围：实际执行的命令、退出码、平台和脚本后端是否支持结论；是否把本地源码测试冒充 Git URL 安装或 Player 验证。

## 输出

先按严重度列出具体、可复现的问题，给出受影响文件/行、触发条件、后果及修复方向，避免直接重写模块。没有发现时说明审查范围和剩余不确定性。

可在授权范围内运行适用测试并报告真实结果，但先确认现有命令和依赖，不能凭空编造测试入口或安装新依赖。没有执行测试时明确标为未运行；已有日志注明来源与环境。
