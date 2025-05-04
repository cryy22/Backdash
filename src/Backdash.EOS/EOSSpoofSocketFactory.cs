using Backdash.Network.Client;
using Backdash.Options;
using Epic.OnlineServices.P2P;

namespace Backdash.EOS;

/// <summary>
///     Factory for EOS P2P connections that spoof the peer socket interface
/// </summary>
public class EosSpoofSocketFactory(SocketId socketId) : IPeerSocketFactory
{
    /// <inheritdoc />
    public IPeerSocket Create(int port, NetcodeOptions options) => new EosSpoofSocket(port, socketId, options.UseIPv6);
}
