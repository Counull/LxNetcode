# 原始 UDP Socket 契约

## 方案与边界

2026-10-18：在提出同步非阻塞与可取消异步两种接收模型后，作者要求按游戏最佳实践选择；本阶段采用同步非阻塞方案。它让调用方决定收包时机，不在基础 Socket 中引入线程、循环、队列或 Unity 调度。实现与验证状态以 [STATUS.md](STATUS.md) 和 [TESTING.md](TESTING.md) 为准。

这不是所有游戏或服务器的唯一选择。事件驱动、空闲时等待 I/O 的宿主也可以采用异步模型；此版不预建尚无调用方需求的第二套入口。非阻塞模型与 [Gaffer 的原始 UDP Socket 入门](https://gafferongames.com/post/sending_and_receiving_packets/)相符，但本模块独立定义空 datagram、截断和错误契约，不照搬示例中的错误处理。

包位于 `Unity/Packages/com.counull.lxnetcode/`，由 `LxNetcode.Sockets` asmdef 构建纯 C# 程序集；`noEngineReferences: true`。开发工程通过 Embedded Package 消费同一份源码，消费项目可通过 Git URL 引用。当前无需托管 DLL、Native 插件或第三方依赖；包组织见 [ARCHITECTURE.md](ARCHITECTURE.md)。

这里只处理原始 UDP datagram，不解析或改写 payload，不提供连接状态、可靠性或顺序保证，不实现作者保留的协议或游戏网络功能。

## 公开接口

命名空间：`LxNetcode.Sockets`。

```csharp
public sealed class UdpSocket : IDisposable
{
    public static UdpSocket Bind(IPEndPoint localEndPoint);
    public IPEndPoint LocalEndPoint { get; }
    public int SendTo(byte[] buffer, int offset, int count, IPEndPoint destination);
    public bool TryReceiveFrom(byte[] buffer, int offset, int count,
        out UdpReceiveResult result);
    public void Dispose();
}

public readonly struct UdpReceiveResult
{
    public int BytesReceived { get; }
    public IPEndPoint RemoteEndPoint { get; }
}
```

`UdpReceiveResult` 的有效值由成功接收产生；payload 写入调用方提供的缓冲区，结果结构体不携带数组。`false` 时 result 为默认值（长度 0、endpoint 为 null），不能把默认结果当作收到空包。

## 绑定与发送

- `Bind` 立即创建并绑定 UDP Socket；端口 0 由操作系统分配，`LocalEndPoint` 返回实际端口。绑定通配地址时，查询结果仍是通配地址，不推测机器的对外地址。
- 绑定地址选择 IPv4 或 IPv6；IPv6 设置 `DualMode=false`。目标地址必须与 Socket 地址族一致；此版不转换 IPv4-mapped IPv6 地址。
- 不启用端口复用；设置 `ExclusiveAddressUse=true`，绑定/选项失败原样抛出异常，并释放未成功建立的 Socket。
- `SendTo` 每次至多向一个显式目标提交一个 datagram。`count=0` 表示发送空 datagram，返回值是本地 Socket 报告的字节数。
- 返回成功只表示本地发送操作完成；不代表对端收到。不自动重试、拆包、合包、限速或排队；包括发送 `WouldBlock` 在内的 Socket 错误由调用方处理。

## 接收与缓冲区边界

| 情况 | 对外行为 |
| --- | --- |
| 当前不可读，或底层返回 `WouldBlock` | 立即返回 `false`，result 为默认值，缓冲区不变 |
| 完整非空 datagram | 返回 `true`，给出实际长度和发送方 endpoint，仅写入指定区间 |
| 完整空 datagram | 返回 `true`，长度为 0，并给出发送方 endpoint；不是连接关闭 |
| datagram 大于接收区间 | 抛 `SocketException`，`SocketErrorCode=MessageSize`；调用方缓冲区不变，该 datagram 已被消费，尾部不能随后续读取得到 |
| 其他 Socket 错误 | 原样抛出，不伪装成“当前无数据” |

接收先用 `Poll(0, SelectRead)` 检查即时可读状态，避免空闲轮询每次产生异常；不使用 `Available > 0`，因为空 datagram 的长度也是 0。每个 Socket 分配一个固定 65,536 字节暂存数组，用 `Socket.ReceiveMessageFrom(byte[], …)` 接收单个普通 IPv4/IPv6 UDP datagram，再检查实际长度是否超过调用方容量。容量足够才复制原始字节；调用结束清除暂存内容，不积压 datagram。代价是每个 Socket 64 KiB 常驻空间，以及每次成功接收的一次复制和清除。

这是针对实际运行时行为的处理：本机 Unity CoreCLR 的小缓冲区接收会成功返回部分字节且 `SocketFlags.Truncated` 为零；仅检查标志会漏报。固定暂存区能容纳普通 IPv4/IPv6 UDP payload，因此调用方容量检查不依赖该标志。仍保留对系统 `MessageSize` 和非零 `Truncated` 的处理。IPv6 jumbogram 不在支持范围内；其他运行时行为仍须实测。错误不返回有效结果，也不承诺返回原始长度或发送方元数据。复现与修复后的结果见 [TESTING.md](TESTING.md)。

接收 `count` 必须大于 0，即使用至少一个字节容量来接收空 datagram。数组为 null、offset/count 越界或接收 count 为 0 均抛标准参数异常，且不会消费 datagram。检查区间采用 `buffer.Length - offset`，避免相加溢出。

每次调用仅尝试接收一个 datagram。模块没有托管收包队列；操作系统自身的有限 Socket 接收缓冲区仍可能在未及时读取时溢出并丢包。

## 线程、停止和所有权

- 所有实例操作通过同一把锁串行化，锁内执行参数处理、即时可读检查、至多一次非阻塞收发以及有界的缓冲区复制/清除。可从多个线程调用，但不承诺并行执行、锁公平性或固定的完成时限；不等待网络数据。
- 模块没有永久收包循环或挂起的异步接收，因此不需要 `CancellationToken`。停止调用 `TryReceiveFrom` 就停止主动收包；再次调用可继续。没有取消任务、后台线程或接收队列需要排空。
- `Dispose` 永久关闭 Socket，且可重复调用。与 I/O 竞争时，一次完整调用先于或后于关闭：先执行的调用按普通契约完成，关闭后的 I/O 和 endpoint 查询抛 `ObjectDisposedException`。若需重新使用，创建并绑定新实例。
- 数组始终属于调用方；方法仅在调用期间借用，返回或抛异常后即可复用。调用期间不得修改相同缓冲区或 endpoint；实例的锁不保护调用方在其他对象或线程中共享的数组。
- 模块不保存 payload。绑定输入、本地 endpoint 查询和成功接收的发送方均使用独立快照，包括复制 IPAddress 字节及 IPv6 ScopeId，防止可变 endpoint/address 引用泄漏。每份结果的 endpoint 归调用方所有；复制同一个结果结构体不会再次深复制它的 endpoint。

## 验证边界

loopback 测试入口位于开发工程的 `Assets/Editor`，不会随 UPM 包分发；不引入第三方测试包。测试通过真实 Socket 验证 IPv4/IPv6、端口 0、原始字节与发送方、datagram 边界、空包、offset、截断恢复、参数/Socket 错误、所有权、并发与关闭，并设置有限等待期限。

兼容基线是实际工程的 Unity 7000.0.0a10、.NET Standard 2.1、C# 9；所用 byte[] 版 `ReceiveMessageFrom` 已在 Unity 实际引用的 netstandard2.1 参考程序集中核对。API 表面可用不等于行为已验证；实际命令、结果及未运行平台统一记入 [TESTING.md](TESTING.md)。
