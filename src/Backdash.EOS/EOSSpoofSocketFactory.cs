using Backdash.Network.Client;
using Backdash.Options;

namespace Backdash.EOS;

/// <summary>
///     Factory for EOS P2P connections that spoof the peer socket interface
/// </summary>
public class EosSpoofSocketFactory : IPeerSocketFactory
{
    // TODO: needs the relevant SocketID provided by the game
    /// <inheritdoc />
    public IPeerSocket Create(int port, NetcodeOptions options) => new EosSpoofSocket(port, options.UseIPv6);
}
