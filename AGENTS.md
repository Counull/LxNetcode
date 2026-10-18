# LxNetcode 项目指令

## 工作入口与目录

- 开始工作时阅读 `README.md`、`docs/STATUS.md`，再读与任务有关的设计或测试记录。
- `docs/` 存放维护文档；`.agents/skills/` 存放项目技能，两者都纳入 Git。
- `Unity/` 用作开发与验证工程目录；可分发源码放在 `Packages/com.counull.lxnetcode/`。
- Unity 7000.0.0a10 开发工程和 UPM 源码包骨架已建立；实际配置与验证结果见 `docs/STATUS.md`、`docs/TESTING.md`。Runtime 目前只有程序集元数据，Socket 接口与实现仍待设计批准。

## 项目目标与 AI 实现边界

这是从零学习游戏网络的项目。作者自己研究并手写 UDP 包格式、协议、连接状态及上层网络功能。参考资料仅是学习背景，不是实现授权。

AI 只可在已批准设计内实现原始 UDP Socket I/O：

- 创建、绑定、查询实际本地 endpoint、关闭和释放 Socket。
- 向调用方明确指定的 endpoint 发送调用方提供的任意字节。
- 接收单个 datagram，返回原始字节、实际长度及发送方 endpoint，保留 datagram 边界。
- 处理生命周期、取消或停止接收、并发访问、Socket 错误和缓冲区所有权。
- 定义并验证空 datagram、缓冲区不足及截断行为，编写模块外部行为测试和必要文档。

以下由作者设计并实现，AI 不得实现、顺手补齐或引入框架接管：

- 包头、包体格式、序列化、消息类型、协议版本。
- Sequence number、ACK、ACK bitfield、可靠性、重传、排序、去重。
- 分片、重组、压缩。
- 虚拟连接、握手、peer/session 状态、心跳、超时断连。
- 流量控制、拥塞控制、协议层限速。
- NAT 穿透、发现、认证、加密。
- 游戏状态同步、Tick、Snapshot、权威服务器逻辑。
- 客户端预测、服务器协调、实体插值、Lag Compensation、Rollback。
- Mirror、FishNet、Netcode for GameObjects、LiteNetLib、ENet 等上层网络框架。

Socket 模块不得解析或改写 payload，不得把发送成功解释为对端收到，不得提供可靠性或顺序保证，不得提供看似已建立连接的高层状态。

## 实现前的设计确认

- 先调查仓库、Unity 的 `ProjectVersion.txt`、实际 API Compatibility Level、构建配置及现有改动，再提出小而明确的方案。
- 方案要说明包与程序集组织、最小接口、同步或异步模型、线程模型、取消与关闭、缓冲区所有权、错误语义和测试方案。
- 等作者确认设计后才修改相关实现文件。文档和目录初始化授权不等于 Socket 实现授权。
- 讨论上层网络设计时可以解释概念、权衡、不变量和测试思路，不提供可直接并入项目的上层实现。

## 工程与验证

- Socket 运行时保持纯 C#，不依赖 UnityEngine，不耦合主线程、游戏 Tick 或场景对象。
- 分发目标是可通过 Git URL 引用的 UPM 源码包；不要默认采用 Native DLL，也不要求先产出托管 DLL。
- 兼容性以实际工程及对应版本官方 API 文档为准，不能由本机 .NET SDK 或某个 Unity 版本名称推定。
- 新增第三方依赖、Native 插件或上层网络库必须事先获得作者批准。
- 默认不引入接收队列；如确需队列，先说明容量、背压或丢弃策略并获得批准，禁止无界积压。
- 实现后实际运行适用的 loopback 集成测试，报告命令、环境、结果和未验证的平台；未运行的检查明确标为未运行。
- 审查当前 diff 时先报告发现与严重度，不借审查扩展功能或直接重写模块。
- README 等文档与本指令冲突时，先指出冲突并建议修订，不能据此扩展实现范围。

## 维护与 Git

- 需求、设计和测试记录不等于已实现或已验证；状态文档应准确区分这些状态。
- 行为或构建方式变化时，同步相关文档；不要写不存在的测试命令、通过结果或发布版本。
- 保留已有 WIP；提交前检查 diff，按明确路径暂存，避免 `git add .`。
- 二进制资源按 `.gitattributes` 使用 Git LFS；源码、Unity YAML 和 `.meta` 保持普通文本。克隆后执行 `git lfs install --local` 和 `git lfs pull`，不擅自迁移历史。
- 本地提交与远程推送分开处理；只在获得远程推送授权时执行，不能由本地提交授权推断推送授权。

## Agent skills

- 设计或实现 Socket 模块时使用 `.agents/skills/lxnetcode-socket-workflow/SKILL.md`。
- 审查 Socket 改动时使用 `.agents/skills/lxnetcode-socket-review/SKILL.md`。
- 技能细化工作流程，不能扩大上述授权边界。

### Issue tracker

使用 GitHub Issues：Counull/LxNetcode。具体约定见 `docs/agents/issue-tracker.md`。

### Triage labels

采用五项默认标签，见 `docs/agents/triage-labels.md`。

### Domain docs

采用 single-context：根目录 `CONTEXT.md` 与 `docs/adr/`。读取与维护规则见 `docs/agents/domain.md`。
