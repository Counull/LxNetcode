using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LxNetcode.Sockets;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UdpReceiveResult = LxNetcode.Sockets.UdpReceiveResult;

namespace LxNetcode.Development
{
    /// <summary>Real UDP loopback checks; this development-only runner is not part of the package.</summary>
    public static class LxNetcodeSocketLoopback
    {
        private const int DeadlineMilliseconds = 3000;

        [Serializable]
        private sealed class CaseResult
        {
            public string name;
            public string status;
            public long milliseconds;
            public string details;
        }

        [Serializable]
        private sealed class Report
        {
            public string completedUtc;
            public string unityVersion;
            public string buildTarget;
            public string apiCompatibilityLevel;
            public int apiCompatibilityLevelValue;
            public string scriptingBackend;
            public string operatingSystem;
            public string editorRuntimeVersion;
            public string runtimeAssemblyPath;
            public bool operatingSystemSupportsIPv6;
            public int passed;
            public int failed;
            public int skipped;
            public CaseResult[] cases;
        }

        private sealed class SkipCaseException : Exception
        {
            public SkipCaseException(string message) : base(message) { }
        }

        private sealed class Received
        {
            public byte[] bytes;
            public IPEndPoint source;
        }

        /// <summary>Throws on failure so Unity -batchmode -executeMethod returns a nonzero exit code.</summary>
        [MenuItem("LxNetcode/Run Socket Loopback Checks")]
        public static void Run()
        {
            LxNetcodeProjectCheck.Run();
            var results = new List<CaseResult>();
            Check(results, "runtime_has_no_Unity_references", RuntimeAssemblyBoundary);
            Check(results, "IPv4_bind_zero_and_endpoint_ownership", BindAndEndpointOwnership);
            Check(results, "wildcard_binding_reports_actual_wildcard", WildcardBinding);
            Check(results, "native_sender_to_wrapper_bytes_and_endpoint", NativeSenderToWrapper);
            Check(results, "wrapper_sender_to_native_bytes_and_endpoint", WrapperSenderToNative);
            Check(results, "IPv4_bidirectional", () => Bidirectional(IPAddress.Loopback));
            Check(results, "IPv6_bidirectional", IPv6Bidirectional);
            Check(results, "empty_datagrams_both_directions", () => EmptyDatagrams(IPAddress.Loopback));
            Check(results, "IPv6_empty_datagrams_both_directions", () => WithIPv6(() => EmptyDatagrams(IPAddress.IPv6Loopback)));
            Check(results, "independent_datagram_boundaries", DatagramBoundaries);
            Check(results, "receive_exact_fit", () => ReceiveExactFit(IPAddress.Loopback));
            Check(results, "IPv6_receive_exact_fit", () => WithIPv6(() => ReceiveExactFit(IPAddress.IPv6Loopback)));
            Check(results, "receive_offset_and_guard_bytes", ReceiveOffsetAndGuards);
            Check(results, "send_offset_and_buffer_reuse", SendOffsetAndReuse);
            Check(results, "truncation_reports_MessageSize_and_consumes_datagram",
                () => TruncationAndNextDatagram(IPAddress.Loopback, 513, 8));
            Check(results, "truncation_by_one_byte_reports_MessageSize",
                () => TruncationAndNextDatagram(IPAddress.Loopback, 33, 32));
            Check(results, "IPv6_truncation_reports_MessageSize_and_recovers",
                () => WithIPv6(() => TruncationAndNextDatagram(IPAddress.IPv6Loopback, 33, 32)));
            Check(results, "large_65000_byte_datagram_and_small_buffer_recovery", LargeDatagramAndSmallBuffer);
            Check(results, "WouldBlock_returns_promptly", EmptyReceiveIsNonblocking);
            Check(results, "stop_polling_then_resume", StopAndResumePolling);
            Check(results, "receive_endpoint_snapshots_are_independent", ReceiveEndpointOwnership);
            Check(results, "Dispose_is_idempotent_and_rejects_later_operations", DisposedOperations);
            Check(results, "disposed_port_can_be_rebound", RebindDisposedPort);
            Check(results, "bind_conflict_is_observable", BindConflict);
            Check(results, "invalid_arguments_are_observable", InvalidArguments);
            Check(results, "oversized_send_reports_MessageSize", OversizedSend);
            Check(results, "concurrent_sends_and_receive", ConcurrentSendsAndReceive);
            Check(results, "concurrent_receivers_preserve_each_sender", ConcurrentReceivers);
            Check(results, "concurrent_IO_and_Dispose_terminate", ConcurrentDispose);
            Check(results, "repeated_bind_send_receive_dispose", RepeatedLifecycle);

            var apiLevel = PlayerSettings.GetApiCompatibilityLevel(NamedBuildTarget.Standalone);
            var report = new Report
            {
                completedUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                apiCompatibilityLevel = apiLevel.ToString(),
                apiCompatibilityLevelValue = (int)apiLevel,
                scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone).ToString(),
                operatingSystem = SystemInfo.operatingSystem,
                editorRuntimeVersion = Environment.Version.ToString(),
                runtimeAssemblyPath = typeof(UdpSocket).Assembly.Location,
                operatingSystemSupportsIPv6 = Socket.OSSupportsIPv6,
                cases = results.ToArray()
            };
            foreach (CaseResult result in results)
            {
                if (result.status == "passed") report.passed++;
                else if (result.status == "failed") report.failed++;
                else report.skipped++;
            }

            string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "..", "Logs", "socket-loopback.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(reportPath, json, new UTF8Encoding(false));
            if (report.failed != 0)
                throw new InvalidOperationException("LXNETCODE_SOCKET_LOOPBACK_FAIL\n" + json);
            Debug.Log("LXNETCODE_SOCKET_LOOPBACK_PASS\n" + json);
        }

        private static void Check(List<CaseResult> results, string name, Action action)
        {
            var watch = Stopwatch.StartNew();
            var result = new CaseResult { name = name };
            try
            {
                action();
                result.status = "passed";
                result.details = "External behavior assertions passed.";
            }
            catch (SkipCaseException exception)
            {
                result.status = "skipped";
                result.details = exception.Message;
            }
            catch (Exception exception)
            {
                result.status = "failed";
                result.details = exception.ToString();
                Debug.LogError("Socket loopback case failed: " + name + "\n" + exception);
            }
            result.milliseconds = watch.ElapsedMilliseconds;
            results.Add(result);
        }

        private static UdpSocket Bind4()
        {
            return UdpSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        }

        private static Socket Native4()
        {
            return Native(IPAddress.Loopback);
        }

        private static Socket Native(IPAddress address)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                if (address.AddressFamily == AddressFamily.InterNetworkV6) socket.DualMode = false;
                socket.Blocking = false;
                socket.Bind(new IPEndPoint(address, 0));
                return socket;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        private static void RuntimeAssemblyBoundary()
        {
            Assembly assembly = typeof(UdpSocket).Assembly;
            Require(assembly.GetName().Name == "LxNetcode.Sockets", "Unexpected runtime assembly.");
            foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                Require(!reference.Name.StartsWith("UnityEngine", StringComparison.Ordinal) &&
                    !reference.Name.StartsWith("UnityEditor", StringComparison.Ordinal),
                    "Socket runtime references " + reference.Name);
        }

        private static void BindAndEndpointOwnership()
        {
            var requested = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 0);
            using (UdpSocket socket = UdpSocket.Bind(requested))
            {
                IPEndPoint actual = socket.LocalEndPoint;
                Require(actual.Port > 0 && actual.Address.Equals(IPAddress.Loopback), "Port 0 was not resolved.");
                int boundPort = actual.Port;
                requested.Port = 1;
                requested.Address = IPAddress.Any;
                actual.Port = 2;
                actual.Address = IPAddress.Any;
                RequireEndpoint(new IPEndPoint(IPAddress.Loopback, boundPort), socket.LocalEndPoint);

                // IPAddress is mutable for IPv6 scope IDs; the getter must copy the address too.
                if (Socket.OSSupportsIPv6)
                {
                    using (UdpSocket ipv6 = UdpSocket.Bind(new IPEndPoint(IPAddress.IPv6Loopback, 0)))
                    {
                        IPEndPoint first = ipv6.LocalEndPoint;
                        long originalScope = first.Address.ScopeId;
                        first.Address.ScopeId = originalScope + 1;
                        Require(ipv6.LocalEndPoint.Address.ScopeId == originalScope,
                            "LocalEndPoint leaked its mutable IPv6 address.");
                    }
                }
            }
        }

        private static void NativeSenderToWrapper()
        {
            using (Socket sender = Native4())
            using (UdpSocket receiver = Bind4())
            {
                byte[] payload = Pattern(257, 31);
                Require(sender.SendTo(payload, receiver.LocalEndPoint) == payload.Length, "Native send length.");
                var buffer = new byte[512];
                UdpReceiveResult result = Receive(receiver, buffer, 0, buffer.Length);
                RequireBytes(payload, buffer, 0, result.BytesReceived);
                RequireEndpoint((IPEndPoint)sender.LocalEndPoint, result.RemoteEndPoint);
            }
        }

        private static void WildcardBinding()
        {
            using (UdpSocket socket = UdpSocket.Bind(new IPEndPoint(IPAddress.Any, 0)))
            using (Socket sender = Native4())
            {
                IPEndPoint local = socket.LocalEndPoint;
                Require(local.Address.Equals(IPAddress.Any) && local.Port > 0,
                    "Wildcard binding must report the actual wildcard and allocated port.");
                byte[] packet = Pattern(13, 29);
                sender.SendTo(packet, new IPEndPoint(IPAddress.Loopback, local.Port));
                var buffer = new byte[32];
                UdpReceiveResult result = Receive(socket, buffer, 0, buffer.Length);
                RequireBytes(packet, buffer, 0, result.BytesReceived);
                RequireEndpoint((IPEndPoint)sender.LocalEndPoint, result.RemoteEndPoint);
                RequireEndpoint(local, socket.LocalEndPoint);
            }
        }

        private static void WrapperSenderToNative()
        {
            using (UdpSocket sender = Bind4())
            using (Socket receiver = Native4())
            {
                byte[] payload = Pattern(509, 43);
                byte[] original = (byte[])payload.Clone();
                Require(sender.SendTo(payload, 0, payload.Length, (IPEndPoint)receiver.LocalEndPoint) == payload.Length,
                    "Wrapper send length.");
                RequireBytes(original, payload, 0, payload.Length);
                var buffer = new byte[1024];
                Received received = ReceiveNative(receiver, buffer);
                RequireBytes(original, received.bytes, 0, received.bytes.Length);
                RequireEndpoint(sender.LocalEndPoint, received.source);
            }
        }

        private static void Bidirectional(IPAddress address)
        {
            using (UdpSocket first = UdpSocket.Bind(new IPEndPoint(address, 0)))
            using (UdpSocket second = UdpSocket.Bind(new IPEndPoint(address, 0)))
            {
                Require(first.LocalEndPoint.Port > 0 && second.LocalEndPoint.Port > 0, "Unresolved ephemeral port.");
                byte[] outbound = Pattern(67, 73);
                byte[] inbound = Pattern(129, 109);
                Require(first.SendTo(outbound, 0, outbound.Length, second.LocalEndPoint) == outbound.Length,
                    "First send length.");
                Require(second.SendTo(inbound, 0, inbound.Length, first.LocalEndPoint) == inbound.Length,
                    "Second send length.");
                var buffer = new byte[256];
                UdpReceiveResult received = Receive(second, buffer, 0, buffer.Length);
                RequireBytes(outbound, buffer, 0, received.BytesReceived);
                RequireEndpoint(first.LocalEndPoint, received.RemoteEndPoint);
                received = Receive(first, buffer, 0, buffer.Length);
                RequireBytes(inbound, buffer, 0, received.BytesReceived);
                RequireEndpoint(second.LocalEndPoint, received.RemoteEndPoint);
            }
        }

        private static void IPv6Bidirectional()
        {
            WithIPv6(() => Bidirectional(IPAddress.IPv6Loopback));
        }

        private static void WithIPv6(Action action)
        {
            if (!Socket.OSSupportsIPv6)
                throw new SkipCaseException("OS reports IPv6 unavailable; this IPv6 loopback case was not run.");
            action();
        }

        private static void EmptyDatagrams(IPAddress address)
        {
            using (UdpSocket wrapper = UdpSocket.Bind(new IPEndPoint(address, 0)))
            using (Socket native = Native(address))
            {
                byte[] empty = Array.Empty<byte>();
                Require(wrapper.SendTo(empty, 0, 0, (IPEndPoint)native.LocalEndPoint) == 0,
                    "Empty wrapper send did not return zero.");
                Received received = ReceiveNative(native, new byte[8]);
                Require(received.bytes.Length == 0, "Empty datagram was changed.");
                RequireEndpoint(wrapper.LocalEndPoint, received.source);
                Require(native.SendTo(empty, wrapper.LocalEndPoint) == 0, "Empty native send length.");
                var guard = new byte[] { 41, 42, 43 };
                UdpReceiveResult result = Receive(wrapper, guard, 1, 1);
                Require(result.BytesReceived == 0, "Empty datagram must be a successful zero-byte receive.");
                RequireEndpoint((IPEndPoint)native.LocalEndPoint, result.RemoteEndPoint);
                RequireBytes(new byte[] { 41, 42, 43 }, guard, 0, guard.Length);
            }
        }

        private static void DatagramBoundaries()
        {
            using (Socket sender = Native4())
            using (UdpSocket receiver = Bind4())
            {
                byte[][] packets = { Pattern(1, 7), Pattern(17, 13), Pattern(255, 19), Array.Empty<byte>() };
                foreach (byte[] packet in packets)
                    Require(sender.SendTo(packet, receiver.LocalEndPoint) == packet.Length, "Boundary send length.");
                var seen = new bool[packets.Length];
                var buffer = new byte[512];
                for (int receiveIndex = 0; receiveIndex < packets.Length; receiveIndex++)
                {
                    UdpReceiveResult result = Receive(receiver, buffer, 0, buffer.Length);
                    RequireEndpoint((IPEndPoint)sender.LocalEndPoint, result.RemoteEndPoint);
                    int match = FindPacket(packets, buffer, result.BytesReceived);
                    Require(match >= 0 && !seen[match], "Datagram was split, merged, changed or duplicated.");
                    seen[match] = true;
                }
                Require(!receiver.TryReceiveFrom(buffer, 0, buffer.Length, out _), "Unexpected extra datagram.");
            }
        }

        private static void ReceiveExactFit(IPAddress address)
        {
            using (Socket sender = Native(address))
            using (UdpSocket receiver = UdpSocket.Bind(new IPEndPoint(address, 0)))
            {
                byte[] packet = Pattern(127, 89);
                sender.SendTo(packet, receiver.LocalEndPoint);
                var buffer = new byte[packet.Length];
                UdpReceiveResult result = Receive(receiver, buffer, 0, buffer.Length);
                RequireBytes(packet, buffer, 0, result.BytesReceived);
            }
        }

        private static void ReceiveOffsetAndGuards()
        {
            using (Socket sender = Native4())
            using (UdpSocket receiver = Bind4())
            {
                byte[] packet = Pattern(31, 97);
                byte[] buffer = Filled(71, 0xCD);
                sender.SendTo(packet, receiver.LocalEndPoint);
                UdpReceiveResult result = Receive(receiver, buffer, 9, packet.Length);
                RequireBytes(packet, buffer, 9, result.BytesReceived);
                RequireGuard(buffer, 0, 9, 0xCD);
                RequireGuard(buffer, 9 + packet.Length, buffer.Length, 0xCD);
            }
        }

        private static void SendOffsetAndReuse()
        {
            using (UdpSocket sender = Bind4())
            using (Socket receiver = Native4())
            {
                byte[] expected = Pattern(53, 151);
                byte[] source = Filled(100, 0xEF);
                Array.Copy(expected, 0, source, 11, expected.Length);
                byte[] original = (byte[])source.Clone();
                Require(sender.SendTo(source, 11, expected.Length, (IPEndPoint)receiver.LocalEndPoint) == expected.Length,
                    "Offset send length.");
                RequireBytes(original, source, 0, source.Length);
                Array.Clear(source, 0, source.Length);
                Received received = ReceiveNative(receiver, new byte[256]);
                RequireBytes(expected, received.bytes, 0, received.bytes.Length);
            }
        }

        private static void TruncationAndNextDatagram(IPAddress address, int packetLength, int receiveCount)
        {
            using (Socket sender = Native(address))
            using (UdpSocket receiver = UdpSocket.Bind(new IPEndPoint(address, 0)))
            {
                sender.SendTo(Pattern(packetLength, 163), receiver.LocalEndPoint);
                var watch = Stopwatch.StartNew();
                bool reported = false;
                byte[] small = Filled(receiveCount + 9, 0xA7);
                while (watch.ElapsedMilliseconds < DeadlineMilliseconds)
                {
                    try
                    {
                        Require(!receiver.TryReceiveFrom(small, 3, receiveCount, out _),
                            "Truncated data was returned as a successful datagram.");
                    }
                    catch (SocketException exception)
                    {
                        Require(exception.SocketErrorCode == SocketError.MessageSize,
                            "Truncation reported " + exception.SocketErrorCode + ".");
                        reported = true;
                        break;
                    }
                    Thread.Sleep(1);
                }
                Require(reported, "Truncation was not reported before the deadline.");
                RequireGuard(small, 0, small.Length, 0xA7);
                // MessageSize consumes the oversized datagram without writing any caller-buffer bytes.
                byte[] next = Pattern(7, 181);
                sender.SendTo(next, receiver.LocalEndPoint);
                var buffer = new byte[32];
                UdpReceiveResult result = Receive(receiver, buffer, 0, buffer.Length);
                RequireBytes(next, buffer, 0, result.BytesReceived);
                RequireEndpoint((IPEndPoint)sender.LocalEndPoint, result.RemoteEndPoint);
                Require(!receiver.TryReceiveFrom(buffer, 0, buffer.Length, out _), "Truncated datagram remained pending.");
            }
        }

        private static void LargeDatagramAndSmallBuffer()
        {
            using (Socket sender = Native4())
            using (UdpSocket receiver = Bind4())
            {
                byte[] packet = Pattern(65000, 137);
                Require(sender.SendTo(packet, receiver.LocalEndPoint) == packet.Length,
                    "Large native send length.");
                byte[] buffer = Filled(packet.Length + 16, 0xCD);
                UdpReceiveResult result = Receive(receiver, buffer, 7, packet.Length);
                RequireBytes(packet, buffer, 7, result.BytesReceived);
                RequireEndpoint((IPEndPoint)sender.LocalEndPoint, result.RemoteEndPoint);
                RequireGuard(buffer, 0, 7, 0xCD);
                RequireGuard(buffer, 7 + packet.Length, buffer.Length, 0xCD);

                Require(sender.SendTo(packet, receiver.LocalEndPoint) == packet.Length,
                    "Large native send into small receive buffer length.");
                byte[] small = Filled(64, 0xB9);
                var watch = Stopwatch.StartNew();
                bool reported = false;
                while (watch.ElapsedMilliseconds < DeadlineMilliseconds)
                {
                    try
                    {
                        Require(!receiver.TryReceiveFrom(small, 0, small.Length, out _),
                            "Large datagram was silently truncated into the small buffer.");
                    }
                    catch (SocketException exception)
                    {
                        Require(exception.SocketErrorCode == SocketError.MessageSize,
                            "Large datagram buffer overflow reported " + exception.SocketErrorCode + ".");
                        reported = true;
                        break;
                    }
                    Thread.Sleep(1);
                }
                Require(reported, "Large datagram buffer overflow was not reported before deadline.");
                RequireGuard(small, 0, small.Length, 0xB9);
                byte[] next = Pattern(13, 173);
                Require(sender.SendTo(next, receiver.LocalEndPoint) == next.Length, "Recovery send length.");
                result = Receive(receiver, small, 0, small.Length);
                RequireBytes(next, small, 0, result.BytesReceived);
                RequireEndpoint((IPEndPoint)sender.LocalEndPoint, result.RemoteEndPoint);
                Require(!receiver.TryReceiveFrom(small, 0, small.Length, out _),
                    "Large rejected datagram remained pending after recovery.");
            }
        }

        private static void EmptyReceiveIsNonblocking()
        {
            using (UdpSocket socket = Bind4())
            {
                byte[] buffer = Filled(3, 0xA5);
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < 128; i++)
                {
                    Require(!socket.TryReceiveFrom(buffer, 1, 1, out UdpReceiveResult result),
                        "Empty socket reported a datagram.");
                    Require(result.BytesReceived == 0 && result.RemoteEndPoint == null,
                        "WouldBlock must return a default result.");
                    RequireGuard(buffer, 0, buffer.Length, 0xA5);
                }
                Require(watch.ElapsedMilliseconds < 1000, "128 empty polls did not return promptly.");
            }
        }

        private static void StopAndResumePolling()
        {
            using (Socket sender = Native4())
            using (UdpSocket receiver = Bind4())
            {
                var buffer = new byte[32];
                Require(!receiver.TryReceiveFrom(buffer, 0, buffer.Length, out _), "Socket unexpectedly had data.");
                byte[] packet = Pattern(19, 193);
                sender.SendTo(packet, receiver.LocalEndPoint);
                // No receive call and no cancellation API is needed while this caller stops polling.
                Thread.Sleep(15);
                UdpReceiveResult result = Receive(receiver, buffer, 0, buffer.Length);
                RequireBytes(packet, buffer, 0, result.BytesReceived);
            }
        }

        private static void ReceiveEndpointOwnership()
        {
            using (Socket first = Native4())
            using (Socket second = Native4())
            using (UdpSocket receiver = Bind4())
            {
                var buffer = new byte[8];
                first.SendTo(new byte[] { 1 }, receiver.LocalEndPoint);
                UdpReceiveResult oldResult = Receive(receiver, buffer, 0, buffer.Length);
                second.SendTo(new byte[] { 2 }, receiver.LocalEndPoint);
                UdpReceiveResult newResult = Receive(receiver, buffer, 0, buffer.Length);
                RequireEndpoint((IPEndPoint)first.LocalEndPoint, oldResult.RemoteEndPoint);
                RequireEndpoint((IPEndPoint)second.LocalEndPoint, newResult.RemoteEndPoint);
                Require(!ReferenceEquals(oldResult.RemoteEndPoint, newResult.RemoteEndPoint),
                    "Two receives share their mutable endpoint.");
                oldResult.RemoteEndPoint.Port = 1;
                RequireEndpoint((IPEndPoint)second.LocalEndPoint, newResult.RemoteEndPoint);
                first.SendTo(new byte[] { 3 }, receiver.LocalEndPoint);
                UdpReceiveResult next = Receive(receiver, buffer, 0, buffer.Length);
                RequireEndpoint((IPEndPoint)first.LocalEndPoint, next.RemoteEndPoint);
            }
        }

        private static void DisposedOperations()
        {
            UdpSocket socket = Bind4();
            IPEndPoint destination = socket.LocalEndPoint;
            socket.Dispose();
            socket.Dispose();
            Expect<ObjectDisposedException>(() => { IPEndPoint ignored = socket.LocalEndPoint; });
            Expect<ObjectDisposedException>(() => socket.SendTo(new byte[1], 0, 1, destination));
            Expect<ObjectDisposedException>(() => socket.TryReceiveFrom(new byte[1], 0, 1, out _));
        }

        private static void RebindDisposedPort()
        {
            IPEndPoint endpoint;
            using (UdpSocket socket = Bind4()) endpoint = socket.LocalEndPoint;
            using (UdpSocket rebound = UdpSocket.Bind(endpoint))
                RequireEndpoint(endpoint, rebound.LocalEndPoint);
        }

        private static void BindConflict()
        {
            using (UdpSocket first = Bind4())
            {
                SocketException exception = Expect<SocketException>(() =>
                {
                    using (UdpSocket unexpected = UdpSocket.Bind(first.LocalEndPoint)) { }
                });
                Require(exception.SocketErrorCode == SocketError.AddressAlreadyInUse ||
                    exception.SocketErrorCode == SocketError.AccessDenied,
                    "Bind conflict reported " + exception.SocketErrorCode + ".");
            }
        }

        private static void InvalidArguments()
        {
            Expect<ArgumentNullException>(() => UdpSocket.Bind(null));
            using (UdpSocket socket = Bind4())
            using (Socket sender = Native4())
            {
                var data = new byte[8];
                IPEndPoint destination = socket.LocalEndPoint;
                byte[] pendingPacket = Pattern(5, 61);
                sender.SendTo(pendingPacket, destination);
                Thread.Sleep(15);
                Expect<ArgumentNullException>(() => socket.SendTo(null, 0, 1, destination));
                Expect<ArgumentNullException>(() => socket.SendTo(data, 0, 1, null));
                Expect<ArgumentOutOfRangeException>(() => socket.SendTo(data, -1, 1, destination));
                Expect<ArgumentOutOfRangeException>(() => socket.SendTo(data, 0, -1, destination));
                Expect<ArgumentException>(() => socket.SendTo(data, 7, 2, destination));
                Expect<ArgumentException>(() => socket.SendTo(data, 1, int.MaxValue, destination));
                Expect<ArgumentNullException>(() => socket.TryReceiveFrom(null, 0, 1, out _));
                Expect<ArgumentOutOfRangeException>(() => socket.TryReceiveFrom(data, -1, 1, out _));
                Expect<ArgumentOutOfRangeException>(() => socket.TryReceiveFrom(data, 0, 0, out _));
                Expect<ArgumentOutOfRangeException>(() => socket.TryReceiveFrom(data, 0, -1, out _));
                Expect<ArgumentException>(() => socket.TryReceiveFrom(data, 7, 2, out _));
                Expect<ArgumentException>(() => socket.TryReceiveFrom(data, 1, int.MaxValue, out _));
                UdpReceiveResult pending = Receive(socket, data, 0, data.Length);
                RequireBytes(pendingPacket, data, 0, pending.BytesReceived);
                RequireEndpoint((IPEndPoint)sender.LocalEndPoint, pending.RemoteEndPoint);
                Require(!socket.TryReceiveFrom(data, 0, data.Length, out _), "Invalid calls manufactured a datagram.");
                Require(socket.SendTo(data, data.Length, 0, destination) == 0,
                    "Zero-length send at the end of a buffer was rejected.");
                UdpReceiveResult empty = Receive(socket, data, 0, data.Length);
                Require(empty.BytesReceived == 0, "Zero-length offset send was changed.");
            }
        }

        private static void OversizedSend()
        {
            using (UdpSocket sender = Bind4())
            using (UdpSocket receiver = Bind4())
            {
                var oversized = new byte[65536];
                SocketException exception = Expect<SocketException>(() =>
                    sender.SendTo(oversized, 0, oversized.Length, receiver.LocalEndPoint));
                Require(exception.SocketErrorCode == SocketError.MessageSize,
                    "Oversized send reported " + exception.SocketErrorCode + ".");
                Require(!receiver.TryReceiveFrom(new byte[8], 0, 8, out _),
                    "Oversized send unexpectedly delivered a datagram.");
            }
        }

        private static void ConcurrentSendsAndReceive()
        {
            const int count = 24;
            using (UdpSocket sender = Bind4())
            using (UdpSocket receiver = Bind4())
            using (var start = new ManualResetEventSlim(false))
            {
                var packets = new byte[count][];
                for (int i = 0; i < count; i++) packets[i] = Pattern(i + 1, i + 17);
                IPEndPoint destination = receiver.LocalEndPoint;
                IPEndPoint expectedSource = sender.LocalEndPoint;
                var received = new List<byte[]>();
                Task receiving = Task.Run(() =>
                {
                    AwaitStart(start);
                    var buffer = new byte[128];
                    for (int i = 0; i < count; i++)
                    {
                        UdpReceiveResult result = Receive(receiver, buffer, 0, buffer.Length);
                        RequireEndpoint(expectedSource, result.RemoteEndPoint);
                        received.Add(Copy(buffer, 0, result.BytesReceived));
                    }
                });
                Task first = Task.Run(() => SendSubset(sender, destination, packets, 0, start));
                Task second = Task.Run(() => SendSubset(sender, destination, packets, 1, start));
                start.Set();
                AwaitTasks(receiving, first, second);
                RequirePacketSet(packets, received);
            }
        }

        private static void SendSubset(UdpSocket sender, IPEndPoint destination, byte[][] packets,
            int parity, ManualResetEventSlim start)
        {
            AwaitStart(start);
            for (int i = parity; i < packets.Length; i += 2)
            {
                byte[] source = (byte[])packets[i].Clone();
                Require(sender.SendTo(source, 0, source.Length, destination) == source.Length, "Concurrent send length.");
                Array.Clear(source, 0, source.Length);
                Thread.Yield();
            }
        }

        private static void ConcurrentReceivers()
        {
            const int count = 20;
            using (Socket first = Native4())
            using (Socket second = Native4())
            using (UdpSocket receiver = Bind4())
            using (var start = new ManualResetEventSlim(false))
            {
                var packets = new byte[count][];
                for (int i = 0; i < count; i++) packets[i] = Pattern(i + 1, i + 53);
                var received = new ConcurrentBag<Received>();
                int total = 0;
                Action consume = () =>
                {
                    AwaitStart(start);
                    var watch = Stopwatch.StartNew();
                    var buffer = new byte[128];
                    while (Volatile.Read(ref total) < count && watch.ElapsedMilliseconds < DeadlineMilliseconds)
                    {
                        if (receiver.TryReceiveFrom(buffer, 0, buffer.Length, out UdpReceiveResult result))
                        {
                            received.Add(new Received
                            {
                                bytes = Copy(buffer, 0, result.BytesReceived),
                                source = result.RemoteEndPoint
                            });
                            Interlocked.Increment(ref total);
                        }
                        else Thread.Sleep(1);
                    }
                };
                Task receivingA = Task.Run(consume);
                Task receivingB = Task.Run(consume);
                start.Set();
                for (int i = 0; i < count; i++)
                {
                    Socket sender = i % 2 == 0 ? first : second;
                    Require(sender.SendTo(packets[i], receiver.LocalEndPoint) == packets[i].Length,
                        "Native concurrent-receiver send length.");
                }
                AwaitTasks(receivingA, receivingB);
                Require(total == count, "Concurrent receivers did not consume every test datagram.");
                var seen = new bool[count];
                foreach (Received packet in received)
                {
                    int index = FindPacket(packets, packet.bytes, packet.bytes.Length);
                    Require(index >= 0 && !seen[index], "Concurrent receiver payload changed or duplicated.");
                    seen[index] = true;
                    RequireEndpoint((IPEndPoint)(index % 2 == 0 ? first : second).LocalEndPoint, packet.source);
                }
            }
        }

        private static void ConcurrentDispose()
        {
            using (UdpSocket socket = Bind4())
            using (Socket destination = Native4())
            using (var entered = new CountdownEvent(2))
            {
                IPEndPoint target = (IPEndPoint)destination.LocalEndPoint;
                int disposedObservations = 0;
                Action worker = () =>
                {
                    var buffer = new byte[8];
                    var watch = Stopwatch.StartNew();
                    entered.Signal();
                    while (watch.ElapsedMilliseconds < DeadlineMilliseconds)
                    {
                        try
                        {
                            socket.SendTo(buffer, 0, buffer.Length, target);
                            socket.TryReceiveFrom(buffer, 0, buffer.Length, out _);
                            IPEndPoint ignored = socket.LocalEndPoint;
                        }
                        catch (ObjectDisposedException)
                        {
                            Interlocked.Increment(ref disposedObservations);
                            return;
                        }
                        Thread.Sleep(1);
                    }
                    throw new InvalidOperationException("Worker did not observe Dispose before the deadline.");
                };
                Task first = Task.Run(worker);
                Task second = Task.Run(worker);
                Require(entered.Wait(DeadlineMilliseconds), "Dispose workers did not start.");
                Task disposing = Task.Run(() => { socket.Dispose(); socket.Dispose(); });
                AwaitTasks(first, second, disposing);
                Require(disposedObservations == 2, "Not all workers observed disposal.");
            }
        }

        private static void RepeatedLifecycle()
        {
            for (int i = 0; i < 16; i++)
            {
                using (UdpSocket sender = Bind4())
                using (UdpSocket receiver = Bind4())
                {
                    byte[] packet = Pattern(9, i + 211);
                    sender.SendTo(packet, 0, packet.Length, receiver.LocalEndPoint);
                    var buffer = new byte[16];
                    UdpReceiveResult result = Receive(receiver, buffer, 0, buffer.Length);
                    RequireBytes(packet, buffer, 0, result.BytesReceived);
                }
            }
        }

        private static UdpReceiveResult Receive(UdpSocket socket, byte[] buffer, int offset, int count)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < DeadlineMilliseconds)
            {
                if (socket.TryReceiveFrom(buffer, offset, count, out UdpReceiveResult result)) return result;
                Thread.Sleep(1);
            }
            throw new TimeoutException("UDP datagram did not arrive before the loopback deadline.");
        }

        private static Received ReceiveNative(Socket socket, byte[] buffer)
        {
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < DeadlineMilliseconds)
            {
                EndPoint remote = new IPEndPoint(socket.AddressFamily == AddressFamily.InterNetworkV6
                    ? IPAddress.IPv6Any : IPAddress.Any, 0);
                try
                {
                    int length = socket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remote);
                    return new Received { bytes = Copy(buffer, 0, length), source = (IPEndPoint)remote };
                }
                catch (SocketException exception) when (exception.SocketErrorCode == SocketError.WouldBlock)
                {
                    Thread.Sleep(1);
                }
            }
            throw new TimeoutException("Native UDP receive did not complete before the loopback deadline.");
        }

        private static void AwaitStart(ManualResetEventSlim start)
        {
            Require(start.Wait(DeadlineMilliseconds), "Concurrent worker start deadline expired.");
        }

        private static void AwaitTasks(params Task[] tasks)
        {
            Require(Task.WaitAll(tasks, DeadlineMilliseconds * 2), "Concurrent workers did not terminate before deadline.");
        }

        private static void RequirePacketSet(byte[][] expected, List<byte[]> actual)
        {
            Require(actual.Count == expected.Length, "Unexpected datagram count.");
            var seen = new bool[expected.Length];
            foreach (byte[] packet in actual)
            {
                int index = FindPacket(expected, packet, packet.Length);
                Require(index >= 0 && !seen[index], "Payload changed, merged, split or duplicated.");
                seen[index] = true;
            }
        }

        private static int FindPacket(byte[][] packets, byte[] buffer, int length)
        {
            for (int i = 0; i < packets.Length; i++)
            {
                if (packets[i].Length != length) continue;
                bool same = true;
                for (int j = 0; j < length; j++)
                    if (packets[i][j] != buffer[j]) { same = false; break; }
                if (same) return i;
            }
            return -1;
        }

        private static byte[] Pattern(int length, int seed)
        {
            var bytes = new byte[length];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = unchecked((byte)(seed + i * 37));
            return bytes;
        }

        private static byte[] Filled(int length, byte value)
        {
            var bytes = new byte[length];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = value;
            return bytes;
        }

        private static byte[] Copy(byte[] buffer, int offset, int count)
        {
            var copy = new byte[count];
            Array.Copy(buffer, offset, copy, 0, count);
            return copy;
        }

        private static void RequireGuard(byte[] buffer, int begin, int end, byte value)
        {
            for (int i = begin; i < end; i++) Require(buffer[i] == value, "Receive overwrote guard byte " + i + ".");
        }

        private static void RequireEndpoint(IPEndPoint expected, IPEndPoint actual)
        {
            Require(actual != null && expected.Port == actual.Port && expected.Address.Equals(actual.Address),
                "Endpoint mismatch: expected " + expected + ", actual " + actual + ".");
        }

        private static void RequireBytes(byte[] expected, byte[] actual, int offset, int count)
        {
            Require(expected.Length == count, "Datagram length mismatch: expected " + expected.Length + ", actual " + count + ".");
            for (int i = 0; i < count; i++) Require(expected[i] == actual[offset + i], "Payload byte mismatch at " + i + ".");
        }

        private static TException Expect<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException exception) { return exception; }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
