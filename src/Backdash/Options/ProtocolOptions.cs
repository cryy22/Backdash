using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;

namespace Backdash.Options;

/// <summary>
///     Network protocol configuration.
/// </summary>
public sealed record ProtocolOptions
{
    /// <summary>
    ///     Sets the <see cref="Endianness" /> used for network communication.
    /// </summary>
    /// <seealso cref="Platform" />
    /// <value>Defaults to <see cref="Endianness.BigEndian" /></value>
    public Endianness SerializationEndianness { get; set; } = Endianness.BigEndian;

    /// <summary>
    ///     Number of bytes used on the <see cref="UdpSocket" /> message buffer.
    /// </summary>
    /// <value>Defaults to (<see cref="NetcodeOptions.NumberOfPlayers" /> * <see cref="Max.CompressedBytes" /> * <c>2</c>)</value>
    public int UdpPacketBufferSize { get; set; }

    /// <summary>
    ///     Max allowed pending inputs in sending queue.
    ///     When reached <see cref="INetcodeSession{TInput}.AddLocalInput(Backdash.NetcodePlayer, in TInput)" /> will return
    ///     <see cref="ResultCode.InputDropped" />.
    /// </summary>
    /// <value>Defaults to <c>64</c></value>
    public int MaxPendingInputs { get; set; } = 64;

    /// <summary>
    ///     Max allowed pending UDP output messages.
    ///     When reached removes and ignores the oldest package in the queue in order to make room for the new package.
    /// </summary>
    /// <value>Defaults to <c>64</c></value>
    public int MaxPackageQueue { get; set; } = 64;

    /// <summary>
    ///     Number of synchronization roundtrips to consider two clients synchronized.
    /// </summary>
    /// <value>Defaults to <c>10</c></value>
    public int NumberOfSyncRoundTrips { get; set; } = 10;

    /// <summary>
    ///     Distance to check out-of-order packets.
    /// </summary>
    /// <value>Defaults to <c>32_768</c></value>
    public int MaxSequenceDistance { get; set; } = 1 << 15;

    /// <summary>
    ///     Total number of synchronization request retries.
    ///     When reached, session will dispatch the <see cref="PeerEvent.SynchronizationFailure" /> event.
    /// </summary>
    /// <value>Defaults to <c>64</c></value>
    public int MaxSyncRetries { get; set; } = 64;

    /// <summary>
    ///     Forced network packet sending latency for the current peer.
    ///     This value is processed using <see cref="DelayStrategy" />.
    /// </summary>
    /// <value>Defaults to <see cref="TimeSpan.Zero" /></value>
    /// <seealso cref="Backdash.Network.DelayStrategy" />
    public TimeSpan NetworkLatency { get; set; }

    /// <summary>
    ///     Strategy for applying delay to sending packages, forcing latency.
    ///     When <see cref="NetworkLatency" /> is <see cref="TimeSpan.Zero" /> this is ignored.
    /// </summary>
    /// <value>Defaults to <see cref="DelayStrategy.Gaussian" /></value>
    /// <seealso cref="NetworkLatency" />
    /// <seealso cref="Backdash.Network.DelayStrategy" />
    public DelayStrategy DelayStrategy { get; set; } = DelayStrategy.Gaussian;

    /// <summary>
    ///     The time to wait before the first <see cref="PeerEvent.ConnectionInterrupted" /> timeout will be sent.
    /// </summary>
    /// <value>Defaults to <c>750</c> milliseconds</value>
    public TimeSpan DisconnectNotifyStart { get; set; } = TimeSpan.FromMilliseconds(750);

    /// <summary>
    ///     The session will automatically disconnect from a remote peer if it has not received a packet in the timeout window.
    ///     You will be notified of the disconnect via <see cref="PeerEvent.Disconnected" /> event.
    /// </summary>
    /// <value>Defaults to <c>5_000</c> milliseconds</value>
    public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromMilliseconds(5_000);

    /// <summary>
    ///     Enable or disable the disconnect timeout check
    ///     <seealso cref="DisconnectTimeout"/>
    /// </summary>
    /// <value>Defaults to <c>true</c></value>
    public bool DisconnectTimeoutEnabled { get; set; } = true;

    internal bool IsDisconnectTimeoutEnabled() =>
        DisconnectTimeoutEnabled && DisconnectTimeout > TimeSpan.Zero;

    /// <summary>
    ///     The time to wait before end the session.
    /// </summary>
    /// <value>Defaults to <c>100</c> milliseconds</value>
    public TimeSpan ShutdownTime { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    ///     The time to wait before resend synchronization retries after the first.
    /// </summary>
    /// <value>Defaults to <c>1000</c> milliseconds</value>
    /// <seealso cref="SyncFirstRetryInterval" />
    public TimeSpan SyncRetryInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    ///     The time to wait before resend the first synchronization request retry.
    /// </summary>
    /// <value>Defaults to <c>500</c> milliseconds</value>
    /// <seealso cref="SyncRetryInterval" />
    public TimeSpan SyncFirstRetryInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     When the time from the last send package until now is greater than this, sends a keep alive packets.
    /// </summary>
    /// <value>Defaults to <c>200</c> milliseconds</value>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    ///     The time to wait before send the next quality report package (determines ping).
    /// </summary>
    /// <value>Defaults to <c>1000</c> milliseconds</value>
    public TimeSpan QualityReportInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    ///     The time to wait before recalculate network statistics.
    /// </summary>
    /// <value>Defaults to <c>1000</c> milliseconds</value>
    /// <seealso cref="PeerNetworkStats" />
    /// <seealso cref="NetworkPackageStatsEnabled" />
    public TimeSpan NetworkPackageStatsInterval { get; set; } = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    ///     When true, session will use calculate packages stats.
    /// </summary>
    /// <seealso cref="NetworkPackageStatsInterval"/>
    /// <seealso cref="PeerNetworkStats.PackagesInfo"/>
    /// <seealso cref="PeerNetworkStats.Send"/>
    /// <seealso cref="PeerNetworkStats.Received"/>
    /// <value>Defaults to <see lanword="true" /></value>
    public bool NetworkPackageStatsEnabled { get; set; } = false;

    internal bool IsNetworkPackageStatsEnabled() =>
        NetworkPackageStatsEnabled && NetworkPackageStatsInterval > TimeSpan.Zero;

    /// <summary>
    ///     When true, the session will calculate the remote player stats on each frame.
    /// </summary>
    /// <value>Defaults to <see lanword="true" /></value>
    /// <seealso cref="NetcodePlayer.NetworkStats"/>
    /// <seealso cref="NetworkPackageStatsEnabled"/>
    /// <seealso cref="INetcodeSession.UpdateNetworkStats"/>
    public bool CalculateRemotePlayerStats { get; set; } = true;

    /// <summary>
    ///     When the time from the last send input until now is greater than this, resends pending inputs.
    /// </summary>
    /// <value>Defaults to <c>200</c> milliseconds</value>
    public TimeSpan ResendInputInterval { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    ///     Offset to be applied to frame on checksum consistency check.
    ///     The frame sent is (<c>LastReceivedFrame - ConsistencyCheckOffset</c>).
    /// </summary>
    /// <value>Defaults to <c>8</c></value>
    /// <seealso cref="ConsistencyCheckTimeout" />
    /// <seealso cref="ConsistencyCheckInterval" />
    public int ConsistencyCheckDistance { get; set; } = 8;

    /// <summary>
    ///     The time to wait before send next consistency check (0 to disable).
    ///     On each interval one peer requests a frame to other peer which must respond
    ///     with the state checksum of that frame.
    /// </summary>
    /// <value>Defaults to <c>3_000</c> milliseconds</value>
    /// <seealso cref="ConsistencyCheckDistance" />
    /// <seealso cref="ConsistencyCheckTimeout" />
    public TimeSpan ConsistencyCheckInterval { get; set; } =
        TimeSpan.FromMilliseconds(3_000);

    /// <summary>
    ///     Enable/Disable consistency check.
    /// </summary>
    /// <seealso cref="ConsistencyCheckDistance" />
    /// <seealso cref="ConsistencyCheckTimeout" />
    public bool ConsistencyCheckEnabled { get; set; } = true;

    internal bool IsConsistencyCheckEnabled() =>
        ConsistencyCheckEnabled && ConsistencyCheckInterval > TimeSpan.Zero;

    /// <summary>
    ///     Max wait time for non-success consistency checks (0 to disable).
    /// </summary>
    /// <value>Defaults to <c>10_000</c> milliseconds</value>
    /// <seealso cref="ConsistencyCheckDistance" />
    /// <seealso cref="ConsistencyCheckInterval" />
    public TimeSpan ConsistencyCheckTimeout { get; set; } =
        TimeSpan.FromMilliseconds(10_000);

    /// <summary>
    ///     Custom receive socket address size
    /// </summary>
    public int ReceiveSocketAddressSize { get; set; }
}
