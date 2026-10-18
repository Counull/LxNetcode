# LxNetcode UPM 源码包

这是提供原始 UDP Socket I/O 的 UPM 源码包，manifest 版本为 `0.0.1-preview.1`。`Runtime/LxNetcode.Sockets.asmdef` 以 `noEngineReferences: true` 建立纯 C# 程序集。命名空间 `LxNetcode.Sockets` 提供 `UdpSocket.Bind`、`LocalEndPoint`、`SendTo`、`TryReceiveFrom`、`Dispose`，以及包含长度和发送方 endpoint 的 `UdpReceiveResult`。

接收同步非阻塞：无包返回 `false`，空 datagram 返回 `true` 和长度 0。调用方拥有数组；不足以容纳完整 datagram 时抛 `SocketException(MessageSize)`，消费该包但不写入调用方数组。每个 Socket 使用固定 64 KiB 单包暂存区，不设收包队列。普通 IPv4/IPv6 UDP 可用，IPv6 jumbogram 不支持。

实例调用与释放通过同一把锁串行化；停止调用即停止主动收包，`Dispose` 可重复，释放后操作抛 `ObjectDisposedException`。无后台线程、异步等待或取消任务。发送成功只表示本地 I/O 完成，不承诺送达、可靠性或顺序。完整所有权和错误语义见[Socket 契约](https://github.com/Counull/LxNetcode/blob/main/docs/SOCKET.md)。

当前兼容基线为 Unity 7000.0.0a10、.NET Standard 2.1、C# 9。包没有外部包依赖、Native 插件或预编译 DLL；其他 Unity 版本与 Player 平台尚未验证。实际测试证据见[测试记录](https://github.com/Counull/LxNetcode/blob/main/docs/TESTING.md)。尚未创建发布 tag。

开发工程直接将此目录识别为 Embedded Package，编辑器与 Git 分发使用同一份源码。未来实际游戏项目通过 Git URL 的 `?path=/Unity/Packages/com.counull.lxnetcode` 安装。具体 URL 形式与刷新行为见[组织决策](https://github.com/Counull/LxNetcode/blob/main/docs/ARCHITECTURE.md)。远程 Git URL 安装尚未运行。

Socket 运行时保持纯 C#，不依赖 UnityEngine。AI 只在设计获批后实现原始 UDP Socket I/O；上层包格式、协议、连接状态和游戏网络逻辑由项目作者手写，完整范围见[项目指令](https://github.com/Counull/LxNetcode/blob/main/AGENTS.md)。
