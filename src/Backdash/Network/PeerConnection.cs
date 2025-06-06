using System.Diagnostics;
using System.Timers;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Network.Messages;
using Backdash.Network.Protocol;
using Backdash.Network.Protocol.Comm;
using Backdash.Options;
using Backdash.Synchronizing;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.State;
using Timer = System.Timers.Timer;

namespace Backdash.Network;

sealed class PeerConnection<TInput> : IDisposable where TInput : unmanaged
{
    readonly ProtocolOptions options;
    readonly ProtocolState state;
    readonly Logger logger;
    readonly TimeSync<TInput> timeSync;
    readonly IProtocolNetworkEventHandler networkEventHandler;
    readonly ProtocolSynchronizer syncRequest;
    readonly ProtocolInbox<TInput> inbox;
    readonly ProtocolOutbox outbox;
    readonly ProtocolInputBuffer<TInput> inputBuffer;
    readonly IStateStore stateStore;

    readonly Timer qualityReportTimer;
    readonly Timer networkStatsTimer;
    readonly Timer keepAliveTimer;
    readonly Timer resendInputsTimer;
    readonly Timer consistencyCheckTimer;
    readonly bool disconnectCheckEnabled;
    long startedAt;

    public PeerConnection(
        ProtocolOptions options,
        ProtocolState state,
        Logger logger,
        TimeSync<TInput> timeSync,
        IProtocolNetworkEventHandler networkEventHandler,
        ProtocolSynchronizer syncRequest,
        ProtocolInbox<TInput> inbox,
        ProtocolOutbox outbox,
        ProtocolInputBuffer<TInput> inputBuffer,
        IStateStore stateStore
    )
    {
        this.options = options;
        this.state = state;
        this.logger = logger;
        this.timeSync = timeSync;
        this.networkEventHandler = networkEventHandler;
        this.syncRequest = syncRequest;
        this.inbox = inbox;
        this.outbox = outbox;
        this.inputBuffer = inputBuffer;
        this.stateStore = stateStore;
        disconnectCheckEnabled = options.IsDisconnectTimeoutEnabled();

        keepAliveTimer = new(options.KeepAliveInterval);
        keepAliveTimer.Elapsed += OnKeepAliveTick;

        resendInputsTimer = new(options.ResendInputInterval);
        resendInputsTimer.Elapsed += OnResendInputs;

        qualityReportTimer = new(options.QualityReportInterval);
        qualityReportTimer.Elapsed += OnQualityReportTick;

        networkStatsTimer = new(options.NetworkPackageStatsInterval);
        networkStatsTimer.Elapsed += OnNetworkStatsTick;

        consistencyCheckTimer = new(options.ConsistencyCheckInterval);
        consistencyCheckTimer.Elapsed += OnConsistencyCheck;

        state.StoppingToken.Register(StopTimers);
    }

    public void Dispose()
    {
        state.CurrentStatus = ProtocolStatus.Disconnected;
        if (!state.StoppingTokenSource.IsCancellationRequested)
            state.StoppingTokenSource.Cancel();

        StopTimers();
        DispatchDisconnectEvent();

        keepAliveTimer.Elapsed -= OnKeepAliveTick;
        resendInputsTimer.Elapsed -= OnKeepAliveTick;
        qualityReportTimer.Elapsed -= OnQualityReportTick;
        networkStatsTimer.Elapsed -= OnNetworkStatsTick;
        consistencyCheckTimer.Elapsed -= OnConsistencyCheck;

        keepAliveTimer.Dispose();
        resendInputsTimer.Dispose();
        qualityReportTimer.Dispose();
        networkStatsTimer.Dispose();
        consistencyCheckTimer.Dispose();
    }

    void StartTimers()
    {
        keepAliveTimer.Start();
        qualityReportTimer.Start();
        resendInputsTimer.Start();

        if (options.IsNetworkPackageStatsEnabled())
            networkStatsTimer.Start();

        if (options.IsConsistencyCheckEnabled())
            consistencyCheckTimer.Start();
    }

    void StopTimers()
    {
        keepAliveTimer.Stop();
        qualityReportTimer.Stop();
        resendInputsTimer.Stop();

        if (options.IsNetworkPackageStatsEnabled())
            networkStatsTimer.Stop();

        if (options.IsConsistencyCheckEnabled())
            consistencyCheckTimer.Stop();
    }

    public void Disconnect()
    {
        if (!state.StoppingTokenSource.IsCancellationRequested)
        {
            state.CurrentStatus = ProtocolStatus.Disconnecting;
            logger.Write(LogLevel.Debug, $"Begin player disconnection: {state.Player}");
            DispatchInterruptedEvent(options.ShutdownTime);
            state.StoppingTokenSource.CancelAfter(options.ShutdownTime);
        }
        else
        {
            if (state.CurrentStatus is ProtocolStatus.Disconnected)
                return;

            logger.Write(LogLevel.Debug, $"Player disconnected: {state.Player}");
            state.CurrentStatus = ProtocolStatus.Disconnected;
            DispatchDisconnectEvent();
        }
    }

    public void Start()
    {
        if (startedAt is 0)
            StartTimers();

        startedAt = Stopwatch.GetTimestamp();
    }

    public void Update()
    {
        switch (state.CurrentStatus)
        {
            case ProtocolStatus.Syncing:
                syncRequest.Update();
                break;
            case ProtocolStatus.Running:
                CheckDisconnection();
                if (options.CalculateRemotePlayerStats)
                    GetNetworkStats(ref state.Player.NetworkStats);
                break;
            case ProtocolStatus.Disconnected:
                break;
        }
    }

    public SendInputResult SendInput(in GameInput<TInput> input) => inputBuffer.SendInput(in input);
    public ProtocolStatus Status => state.CurrentStatus;
    public bool IsRunning => state.CurrentStatus is ProtocolStatus.Running;
    public NetcodePlayer Player => state.Player;
    public PeerAddress Address => state.PeerAddress;

    // require idle input should be a configuration parameter
    public int GetRecommendFrameDelay() => timeSync.RecommendFrameWaitDuration();

    public void SetLocalFrameNumber(Frame localFrame, int fps)
    {
        /*
         * Estimate which frame the other guy is one by looking at the
         * last frame they gave us plus some delta for the one-way packet
         * trip time.
         */
        var deltaFrame = FrameSpan.FromMilliseconds(state.Stats.RoundTripTime.TotalMilliseconds, fps);
        var remoteFrame = inbox.LastReceivedInput.Frame + deltaFrame;
        /*
         * Our frame advantage is how many frames *behind* the other guy
         * we are.  Counter-intuitive, I know.  It's an advantage because
         * it means they'll have to predict more often and our moves will
         * pop more frequently.
         */
        state.Fairness.LocalFrameAdvantage = remoteFrame - localFrame;
    }

    public bool SendInputAck()
    {
        if (inbox.LastReceivedInput.Frame.IsNull)
            return true;

        ProtocolMessage msg = new(MessageType.InputAck)
        {
            InputAck = new()
            {
                AckFrame = inbox.LastReceivedInput.Frame,
            },
        };
        return outbox.SendMessage(in msg);
    }

    public void GetNetworkStats(ref PeerNetworkStats info)
    {
        var stats = state.Stats;
        info.Valid = true;
        info.Ping = stats.RoundTripTime;
        info.PendingInputCount = inputBuffer.PendingNumber;
        info.LastAckedFrame = inbox.LastAckedFrame;
        info.RemoteFramesBehind = state.Fairness.RemoteFrameAdvantage;
        info.LocalFramesBehind = state.Fairness.LocalFrameAdvantage;
        info.Send.LastFrame = inputBuffer.LastSent.Frame;
        info.Received.LastFrame = inbox.LastReceivedInput.Frame;

        if (!options.NetworkPackageStatsEnabled) return;
        info.Send.Fill(stats.Send);
        info.Received.Fill(stats.Received);
    }

    public bool GetPeerConnectStatus(int id, out Frame frame)
    {
        var peer = state.PeerConnectStatuses[id];
        frame = peer.LastFrame;
        return !peer.Disconnected;
    }

    public IPeerObserver<ProtocolMessage> GetUdpObserver() => inbox;
    public void Synchronize() => syncRequest.Synchronize();

    void CheckDisconnection()
    {
        if (state.Stats.Received.LastTime <= 0 || !disconnectCheckEnabled) return;
        var lastReceivedTime = Stopwatch.GetElapsedTime(state.Stats.Received.LastTime);
        if (lastReceivedTime > options.DisconnectNotifyStart &&
            DispatchInterruptedEvent(options.DisconnectTimeout - options.DisconnectNotifyStart))
        {
            logger.Write(LogLevel.Warning,
                $"{state.Player} endpoint has stopped receiving packets for {(int)lastReceivedTime.TotalMilliseconds}ms. Sending notification");
            return;
        }

        if (lastReceivedTime > options.DisconnectTimeout)
        {
            if (!DispatchDisconnectEvent()) return;
            logger.Write(LogLevel.Warning,
                $"{state.Player} endpoint has stopped receiving packets for {(int)lastReceivedTime.TotalMilliseconds}ms. Disconnecting");
        }
    }

    readonly object eventLocker = new();

    bool DispatchInterruptedEvent(TimeSpan timeout)
    {
        lock (eventLocker)
        {
            if (state.Connection is not { DisconnectNotifySent: false, DisconnectEventSent: false })
                return false;

            networkEventHandler.OnNetworkEvent(new(ProtocolEvent.NetworkInterrupted, state.Player)
            {
                NetworkInterrupted = new()
                {
                    DisconnectTimeout = timeout,
                },
            });

            state.Connection.DisconnectNotifySent = true;

            return true;
        }
    }

    bool DispatchDisconnectEvent()
    {
        lock (eventLocker)
        {
            if (state.Connection.DisconnectEventSent)
                return false;

            state.Connection.DisconnectEventSent = true;
            networkEventHandler.OnNetworkEvent(ProtocolEvent.Disconnected, state.Player);

            return true;
        }
    }

    void OnKeepAliveTick(object? sender, ElapsedEventArgs e)
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        var lastSend = state.Stats.Send.LastTime;
        if (lastSend is 0 || Stopwatch.GetElapsedTime(lastSend) < options.KeepAliveInterval)
            return;

        logger.Write(LogLevel.Information, "Sending keep alive packet");
        outbox.SendMessage(new(MessageType.KeepAlive)
        {
            KeepAlive = new(),
        });
    }

    void OnResendInputs(object? sender, ElapsedEventArgs e)
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        var lastReceivedInputTime = state.Stats.LastReceivedInputTime;
        if (lastReceivedInputTime <= 0 ||
            Stopwatch.GetElapsedTime(lastReceivedInputTime) <= options.ResendInputInterval)
            return;

        logger.Write(LogLevel.Information,
            $"{state.Player} haven't exchanged packets in a while (last received:{inbox.LastReceivedInput.Frame.Number} last sent:{inputBuffer.LastSent.Frame.Number}). Resending");

        inputBuffer.SendPendingInputs();
    }

    void OnQualityReportTick(object? sender, ElapsedEventArgs e)
    {
        if (state.CurrentStatus is not ProtocolStatus.Running)
            return;

        outbox.SendMessage(new(MessageType.QualityReport)
        {
            QualityReport = new()
            {
                Ping = Stopwatch.GetTimestamp(),
                FrameAdvantage = state.Fairness.LocalFrameAdvantage.FrameCount,
            },
        });
    }

    void OnNetworkStatsTick(object? sender, ElapsedEventArgs e)
    {
        const int packageHeaderSize = UdpSocket.UdpHeaderSize + UdpSocket.IpAddressHeaderSize;
        if (state.CurrentStatus is not ProtocolStatus.Running || !options.NetworkPackageStatsEnabled)
            return;

        var elapsed = Stopwatch.GetElapsedTime(startedAt);
        UpdateStats(ref state.Stats.Send);
        UpdateStats(ref state.Stats.Received);

        return;

        void UpdateStats(ref ProtocolState.PackagesStats stats)
        {
            var totalUdpHeaderSize = (ByteSize)(packageHeaderSize * stats.TotalPackets);
            stats.TotalBytesWithHeaders = stats.TotalBytes + totalUdpHeaderSize;
            stats.TotalBytesWithHeaders = stats.TotalBytes + totalUdpHeaderSize;
            stats.PackagesPerSecond = (float)(stats.TotalPackets * 1000f / elapsed.TotalMilliseconds);
            stats.Bandwidth = stats.TotalBytesWithHeaders / elapsed.TotalSeconds;
            stats.UdpOverhead =
                (float)(100.0 * (packageHeaderSize * stats.TotalPackets) / stats.TotalBytes.ByteCount);
        }
    }

    void OnConsistencyCheck(object? sender, ElapsedEventArgs e)
    {
        if (state.CurrentStatus is not ProtocolStatus.Running) return;

        var lastReceivedFrame = inbox.LastReceivedInput.Frame;
        var checkFrame = lastReceivedFrame.Number - options.ConsistencyCheckDistance;

        if (checkFrame <= 1)
            return;

        state.Consistency.AskedFrame = new(checkFrame);
        state.Consistency.AskedChecksum = stateStore.GetChecksum(state.Consistency.AskedFrame);

        if (state.Consistency.AskedFrame.IsNull || state.Consistency.AskedChecksum is 0)
            return;

        if (state.Consistency.LastCheck is 0)
            state.Consistency.LastCheck = Stopwatch.GetTimestamp();

        logger.Write(LogLevel.Trace,
            $"Start consistency check for frame {state.Consistency.AskedFrame} #{state.Consistency.AskedChecksum:x8}");

        var elapsed = Stopwatch.GetElapsedTime(state.Consistency.LastCheck);
        if (options.ConsistencyCheckTimeout > TimeSpan.Zero && elapsed > options.ConsistencyCheckTimeout)
        {
            logger.Write(LogLevel.Error,
                $"Consistency check timeout on frame {lastReceivedFrame.Number}. Disconnecting");
            Disconnect();
            return;
        }

        logger.Write(LogLevel.Debug,
            $"Send consistency request for frame {state.Consistency.AskedFrame.Number} #{state.Consistency.AskedChecksum:x8}");

        outbox
            .SendMessage(new(MessageType.ConsistencyCheckRequest)
            {
                ConsistencyCheckRequest = new()
                {
                    Frame = state.Consistency.AskedFrame,
                },
            });
    }
}
