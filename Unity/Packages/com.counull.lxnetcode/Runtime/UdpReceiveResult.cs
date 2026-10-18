using System.Net;

namespace LxNetcode.Sockets
{
    /// <summary>Metadata for one complete datagram; payload bytes remain in the caller's buffer.</summary>
    public readonly struct UdpReceiveResult
    {
        /// <summary>The number of received bytes. Zero is a valid empty datagram.</summary>
        public int BytesReceived { get; }

        /// <summary>
        /// The actual sender, owned by the caller and never reused by the socket.
        /// Null in the default result returned when no datagram is available.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; }

        internal UdpReceiveResult(int bytesReceived, IPEndPoint remoteEndPoint)
        {
            BytesReceived = bytesReceived;
            RemoteEndPoint = remoteEndPoint;
        }
    }
}
