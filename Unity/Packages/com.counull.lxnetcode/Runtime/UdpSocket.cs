using System;
using System.Net;
using System.Net.Sockets;

namespace LxNetcode.Sockets
{
    /// <summary>
    /// A bound, non-blocking UDP socket. Each call sends or receives at most one datagram.
    /// This class does not interpret payloads, run a receive loop, or queue datagrams.
    /// </summary>
    public sealed class UdpSocket : IDisposable
    {
        private readonly object _gate = new object();
        private readonly IPEndPoint _localEndPoint;
        private readonly AddressFamily _addressFamily;
        private readonly Socket _socket;
        // One ordinary IPv4/IPv6 UDP datagram fits here. Receiving directly into a small
        // caller buffer can silently truncate on some runtimes without setting Truncated.
        private readonly byte[] _receiveBuffer = new byte[65536];
        private EndPoint _receiveEndPoint;
        private bool _disposed;

        private UdpSocket(Socket socket)
        {
            _socket = socket;
            _addressFamily = socket.AddressFamily;
            _localEndPoint = CopyEndPoint((IPEndPoint)socket.LocalEndPoint);
            _receiveEndPoint = new IPEndPoint(
                _addressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, 0);
        }

        /// <summary>
        /// Creates and binds an IPv4 or IPv6 socket. Port zero requests an OS-assigned port.
        /// IPv6 sockets accept IPv6 addresses only; no dual-mode address conversion is performed.
        /// </summary>
        /// <exception cref="SocketException">Socket creation, configuration, or binding failed.</exception>
        public static UdpSocket Bind(IPEndPoint localEndPoint)
        {
            if (localEndPoint == null)
                throw new ArgumentNullException(nameof(localEndPoint));

            IPEndPoint endpoint = CopyEndPoint(localEndPoint);
            var socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                if (endpoint.AddressFamily == AddressFamily.InterNetworkV6)
                    socket.DualMode = false;
                socket.ExclusiveAddressUse = true;
                socket.Blocking = false;
                socket.Bind(endpoint);
                return new UdpSocket(socket);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        /// <summary>
        /// The actual bound endpoint, including an OS-assigned port. Returns an independent
        /// endpoint and address snapshot. A wildcard binding remains a wildcard address.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                lock (_gate)
                {
                    ThrowIfDisposed();
                    return CopyEndPoint(_localEndPoint);
                }
            }
        }

        /// <summary>
        /// Sends the specified byte range as one datagram to an explicit destination.
        /// Count zero sends an empty datagram. The buffer is borrowed only for this call.
        /// A successful return reports local I/O only, not delivery to the destination.
        /// </summary>
        /// <remarks>
        /// No automatic retry or send queue is provided. Socket errors, including WouldBlock,
        /// are propagated to the caller. Do not mutate the buffer or destination during the call.
        /// </remarks>
        public int SendTo(byte[] buffer, int offset, int count, IPEndPoint destination)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                ValidateBuffer(buffer, offset, count, false);
                if (destination == null)
                    throw new ArgumentNullException(nameof(destination));
                if (destination.AddressFamily != _addressFamily)
                    throw new ArgumentException("Destination must match the bound address family.", nameof(destination));

                return _socket.SendTo(buffer, offset, count, SocketFlags.None, CopyEndPoint(destination));
            }
        }

        /// <summary>
        /// Tries to receive one datagram into the specified byte range. Returns false only
        /// when no datagram is currently available. An empty datagram returns true with BytesReceived zero.
        /// </summary>
        /// <remarks>
        /// Count must be positive. The buffer belongs to the caller and may be reused after
        /// return. On success, result contains the byte count and an independent sender snapshot.
        /// On false, result is default and the buffer is unchanged. Do not share or mutate a
        /// buffer concurrently. No method waits for a datagram or starts background work.
        /// </remarks>
        /// <exception cref="SocketException">
        /// Socket I/O failed. MessageSize means the datagram did not fit: it has been consumed,
        /// the caller buffer is unchanged, and no successful result is returned.
        /// </exception>
        public bool TryReceiveFrom(byte[] buffer, int offset, int count, out UdpReceiveResult result)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                ValidateBuffer(buffer, offset, count, true);
                result = default;
                // Available may be zero for an empty datagram. Poll handles that case without
                // allocating a WouldBlock exception on every idle call; it never waits for data.
                if (!_socket.Poll(0, SelectMode.SelectRead))
                    return false;

                SocketFlags flags = SocketFlags.None;
                EndPoint sender = _receiveEndPoint;
                int received = -1;
                try
                {
                    received = _socket.ReceiveMessageFrom(_receiveBuffer, 0, _receiveBuffer.Length,
                        ref flags, ref sender, out _);
                    _receiveEndPoint = sender;
                    if ((flags & SocketFlags.Truncated) != 0 || received > count)
                        throw new SocketException((int)SocketError.MessageSize);

                    Buffer.BlockCopy(_receiveBuffer, 0, buffer, offset, received);
                    result = new UdpReceiveResult(received, CopyEndPoint((IPEndPoint)sender));
                    return true;
                }
                catch (SocketException error) when (error.SocketErrorCode == SocketError.WouldBlock)
                {
                    return false;
                }
                finally
                {
                    // A failing native receive may have written bytes without returning a
                    // length. Clear the whole staging buffer in that case; retain no payload.
                    Array.Clear(_receiveBuffer, 0, received >= 0 ? received : _receiveBuffer.Length);
                }
            }
        }

        /// <summary>
        /// Closes the socket once. Safe to repeat and to race with other calls; the shared
        /// lock orders each whole call before or after disposal. Later operations throw
        /// ObjectDisposedException. There is no pending receive to cancel or drain.
        /// </summary>
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;
                _disposed = true;
                _socket.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UdpSocket));
        }

        private static void ValidateBuffer(byte[] buffer, int offset, int count, bool receiving)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || offset > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset || (receiving && count == 0))
                throw new ArgumentOutOfRangeException(nameof(count));
        }

        private static IPEndPoint CopyEndPoint(IPEndPoint endpoint)
        {
            IPAddress address = endpoint.Address;
            byte[] bytes = address.GetAddressBytes();
            IPAddress copy = address.AddressFamily == AddressFamily.InterNetworkV6
                ? new IPAddress(bytes, address.ScopeId)
                : new IPAddress(bytes);
            return new IPEndPoint(copy, endpoint.Port);
        }
    }
}
