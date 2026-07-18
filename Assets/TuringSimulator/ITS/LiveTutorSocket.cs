using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
using System.Net.WebSockets;
#endif
using ITS.Protocol;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;
using UnityEngine;

/// <summary>
/// WebSocket client for live protocol v1 (advisory-only downstream). Desktop/player builds only.
/// </summary>
public class LiveTutorSocket : MonoBehaviour
{
    /// <summary>Matches TuringBotAPI <c>PROTOCOL_VERSION</c>; prefer <see cref="LiveV1Constants.ProtocolVersion"/>.</summary>
    public const string ProtocolVersion = LiveV1Constants.ProtocolVersion;

    public static LiveTutorSocket Instance { get; private set; }

    [Header("Live tutor WebSocket (protocol v1)")]
    [SerializeField] private string wsUrl = "ws://localhost:8000/ws/live";
    [SerializeField] private bool connectOnStart = true;

    ILiveTutorInboundHandler _inboundHandler;

    string _sessionId;
    string _activeStudentId;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
    ClientWebSocket _socket;
    CancellationTokenSource _cts;
    readonly System.Collections.Concurrent.ConcurrentQueue<string> _receiveQueue = new();
    bool _handshakeSent;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _sessionId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Binds the live channel to the currently active player session.
    /// This is called after /session/new so the handshake cannot race session allocation.
    /// </summary>
    public void SetActiveStudentSession(string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            return;

        _activeStudentId = studentId;
        _sessionId = Guid.NewGuid().ToString();

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        _handshakeSent = false;
        if (connectOnStart)
            _ = TryConnectAndHandshakeAsync();
#endif
    }

    /// <summary>Stops live telemetry for the previous player.</summary>
    public void ClearActiveStudentSession()
    {
        _activeStudentId = null;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
        _handshakeSent = false;
        _cts?.Cancel();
        try
        {
            _socket?.Abort();
        }
        catch { /* ignore */ }
        _socket?.Dispose();
        _socket = null;
#endif
    }

    /// <summary>Optional override; defaults to <see cref="DefaultLiveTutorInboundHandler.Instance"/>.</summary>
    public void SetInboundHandler(ILiveTutorInboundHandler handler) => _inboundHandler = handler;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
    void Start()
    {
        if (connectOnStart)
        {
            var studentId = SkillTracker.Instance != null && SkillTracker.Instance.HasActiveSession
                ? SkillTracker.Instance.StudentId
                : null;
            if (!string.IsNullOrWhiteSpace(studentId))
                SetActiveStudentSession(studentId);
        }
    }

    public async Task TryConnectAndHandshakeAsync()
    {
        if (string.IsNullOrWhiteSpace(_activeStudentId))
        {
            Debug.LogWarning("[LiveTutorSocket] Connect skipped: no active student session.");
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        try
        {
            _socket?.Dispose();
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(new Uri(wsUrl), _cts.Token);
            SendHandshake();
            _ = ReceiveLoopAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LiveTutorSocket] Connect failed: {e.Message}");
        }
    }

    async Task ReceiveLoopAsync()
    {
        var buf = new byte[65536];
        try
        {
            while (_socket != null && _socket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buf), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
                var text = Encoding.UTF8.GetString(buf, 0, result.Count);
                _receiveQueue.Enqueue(text);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LiveTutorSocket] Receive ended: {e.Message}");
        }
    }

    void Update()
    {
        while (_receiveQueue.TryDequeue(out var json))
            DispatchInbound(json);
    }

    void OnDestroy()
    {
        _cts?.Cancel();
        try
        {
            _socket?.Abort();
        }
        catch { /* ignore */ }

        _socket?.Dispose();
        _socket = null;
        if (Instance == this)
            Instance = null;
    }

    void DispatchInbound(string json)
    {
        if (!LiveV1Wire.TryDeserializeInbound(json, out var msg))
        {
            Debug.LogWarning("[LiveTutorSocket] ignored or unknown inbound frame");
            return;
        }

        var h = _inboundHandler ?? DefaultLiveTutorInboundHandler.Instance;

        if (msg.HandshakeAck != null)
        {
            h.OnHandshakeAck(msg.HandshakeAck);
            return;
        }

        if (msg.AdvisoryHint != null)
        {
            h.OnAdvisoryText(msg.Kind, msg.AdvisoryHint.Text);
            return;
        }

        if (msg.AdvisoryWarning != null)
        {
            h.OnAdvisoryText(msg.Kind, msg.AdvisoryWarning.Text);
            return;
        }

        if (msg.AdvisoryNudge != null)
        {
            h.OnAdvisoryText(msg.Kind, msg.AdvisoryNudge.Text);
            return;
        }

        if (msg.Error != null)
            h.OnError(msg.Error.Code ?? "", msg.Error.Message ?? "");
    }

    void SendHandshake()
    {
        var st = _activeStudentId;
        if (string.IsNullOrWhiteSpace(st))
        {
            Debug.LogWarning("[LiveTutorSocket] Handshake skipped: no active student session.");
            return;
        }
        var lv = SkillTracker.Instance != null ? SkillTracker.Instance.GetCurrentLevelId() : null;
        if (string.IsNullOrEmpty(lv))
            lv = ITS.LevelID.MoveLeftRight;

        var envelope = LiveV1Wire.BuildEnvelope(
            LiveV1Kinds.Handshake,
            _sessionId,
            st,
            lv,
            LiveV1Wire.DefaultHandshakePayload());
        _ = SendTextAsync(LiveV1Wire.SerializeEnvelope(envelope));
        _handshakeSent = true;
    }

    public void SendRunLifecycle(string phase)
    {
        if (_socket == null || _socket.State != WebSocketState.Open || !_handshakeSent)
            return;

        SendEnvelope(LiveV1Kinds.RunLifecycle, new RunLifecyclePayloadDto { Phase = phase });
    }

    public void SendLevelSnapshot(string title, string description, IReadOnlyList<Symbol> symbols, int headIndex)
    {
        if (_socket == null || _socket.State != WebSocketState.Open || !_handshakeSent)
            return;

        var body = LiveV1Wire.LevelSnapshotFromTape(title, description, symbols, headIndex);
        SendEnvelope(LiveV1Kinds.LevelSnapshot, body);
    }

    public void SendPlaybackStep(StepResult step)
    {
        if (_socket == null || _socket.State != WebSocketState.Open || !_handshakeSent)
            return;

        if (step.IsHalt)
        {
            SendEnvelope(LiveV1Kinds.SimHalt, new SimHaltPayloadDto { HaltStatus = step.AsHalt().ToString() });
            return;
        }

        var diff = step.AsDiff();
        SendEnvelope(LiveV1Kinds.SimStep, LiveV1Wire.SimStepFromDiff(diff));
    }

    void SendEnvelope(string kind, object payloadDto)
    {
        var st = _activeStudentId;
        if (string.IsNullOrWhiteSpace(st))
        {
            Debug.LogWarning($"[LiveTutorSocket] {kind} skipped: no active student session.");
            return;
        }
        var lv = SkillTracker.Instance != null ? SkillTracker.Instance.GetCurrentLevelId() : null;
        if (string.IsNullOrEmpty(lv))
            lv = ITS.LevelID.MoveLeftRight;

        var envelope = LiveV1Wire.BuildEnvelope(kind, _sessionId, st, lv, payloadDto);
        _ = SendTextAsync(LiveV1Wire.SerializeEnvelope(envelope));
    }

    async Task SendTextAsync(string json)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LiveTutorSocket] Send failed: {e.Message}");
        }
    }

#else
    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetInboundHandler(ILiveTutorInboundHandler _) { }

    public void SendRunLifecycle(string _) { }
    public void SendLevelSnapshot(string _t, string _d, IReadOnlyList<Symbol> _s, int _h) { }
    public void SendPlaybackStep(StepResult _) { }
#endif
}
