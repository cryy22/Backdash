using System.Net;
using System.Net.Sockets;

namespace Backdash.Network.Client;

/// <summary>
///     Socket abstraction over peers
/// </summary>
public interface IPeerSocket : IDisposable
{
    /// <inheritdoc cref="Socket.AddressFamily" />
    AddressFamily AddressFamily { get; }

    /// <summary>
    ///     Binding port
    /// </summary>
    int Port { get; }

    /// <summary>
    ///     Receive bytes from specified remote host
    /// </summary>
    ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken cancellationToken);

    /// <summary>
    ///     Receives data and returns the endpoint of the sending host.
    /// </summary>
    ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    /// <summary>
    ///     Sends data to the specified remote host.
    /// </summary>
    ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Sends data to the specified remote host.
    /// </summary>
    ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint,
        CancellationToken cancellationToken);

    /// <inheritdoc cref="Socket.Close()" />
    void Close();

    /// <summary>
    ///     Run synchronous update
    /// </summary>
    void Update() { }
}
