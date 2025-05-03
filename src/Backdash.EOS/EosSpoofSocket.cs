using System.Net;
using System.Net.Sockets;
using Backdash.Network.Client;

namespace Backdash.EOS;

public class EosSpoofSocket : IPeerSocket
{
    public AddressFamily AddressFamily { get; }
    public int Port { get; }

    public EosSpoofSocket(int port, bool useIPv6 = false)
    {
        Port = port;
        AddressFamily = useIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
    }

    public ValueTask<int> ReceiveFromAsync(
        Memory<byte> buffer,
        SocketAddress address,
        CancellationToken cancellationToken
    )
    {

    }

    public ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) => default;

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress, CancellationToken cancellationToken) => default;

    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken) => default;

    public void Dispose() { }
    public void Close() { }
}
