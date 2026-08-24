// ITSClient.cs
// Slim REST client for the main demo line: /session/new, /ask, /health.

using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using ITS;
using ITS.Protocol;
using Newtonsoft.Json;
using TuringSimulator.GameFlow.Events;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-200)]
public class ITSClient : MonoBehaviour, IAskClient
{
    public static ITSClient Instance { get; private set; }

    [Header("Server")]
    [SerializeField] private string _baseUrl = "http://localhost:8000";
    [SerializeField] private float _timeoutSeconds = 10f;

    [Header("Event Channels (event-driven wiring)")]
    [SerializeField] private TranscriptionReadyEventChannel _transcriptionReadyChannel;
    [SerializeField] private AskRequestedEventChannel _askRequestedChannel;
    [SerializeField] private AskResultEventChannel _askResultChannel;
    [SerializeField] private ThinkingStateChangedEventChannel _thinkingStateChannel;

    public event Action<string> OnAskReply;
    public event Action<string> OnServerError;
    public event Action<string> OnSessionCreated;

    public bool CanPostAsk => _serverAvailable;

    bool _serverAvailable;
    string _pendingAskCorrelationId = string.Empty;
    bool _isAwaitingAskResult;

    static string SerializeBody(object o) =>
        JsonConvert.SerializeObject(o, ItsRestJson.Settings);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (_transcriptionReadyChannel != null)
            _transcriptionReadyChannel.OnRaised += HandleTranscriptionReadyEvent;
        StartCoroutine(CheckServerHealth());
    }

    void OnDestroy()
    {
        if (_transcriptionReadyChannel != null)
            _transcriptionReadyChannel.OnRaised -= HandleTranscriptionReadyEvent;
    }

    /// <summary>Send a free-form student question. Reply via <see cref="OnAskReply"/>.</summary>
    public void Ask(string studentId, string levelId, string question)
    {
        Ask(studentId, levelId, question, BuildCorrelationId("ask"));
    }

    void Ask(string studentId, string levelId, string question, string correlationId)
    {
        if (!_serverAvailable)
        {
            EchoTranscription(correlationId, question);
            return;
        }

        _pendingAskCorrelationId = correlationId;
        _isAwaitingAskResult = true;
        PublishThinkingState(correlationId, true);

        var req = new AskRequest
        {
            student_id = studentId,
            level_id = levelId,
            question = question,
        };
        StartCoroutine(Post("/ask", SerializeBody(req), OnAskResponse));
    }

    public void RequestNewSession(Action<string> onComplete)
    {
        if (onComplete == null) return;
        StartCoroutine(Post("/session/new", "{}", (json, success) =>
        {
            if (!success)
            {
                var fallback = BuildLocalFallbackStudentId();
                OnSessionCreated?.Invoke(fallback);
                onComplete(fallback);
                return;
            }

            var dto = JsonConvert.DeserializeObject<SessionNewResponseDto>(json, ItsRestJson.Settings);
            if (string.IsNullOrWhiteSpace(dto?.StudentId))
            {
                var fallback = BuildLocalFallbackStudentId();
                Debug.LogWarning("[ITSClient] /session/new returned empty student_id; using local fallback.");
                OnSessionCreated?.Invoke(fallback);
                onComplete(fallback);
                return;
            }

            _serverAvailable = true;
            OnSessionCreated?.Invoke(dto.StudentId);
            onComplete(dto.StudentId);
        }));
    }

    public Task<string> RequestNewSessionAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        RequestNewSession(id => tcs.TrySetResult(id));
        return tcs.Task;
    }

    void OnAskResponse(string json, bool success)
    {
        if (!success) return;
        var dto = JsonConvert.DeserializeObject<AskResponseDto>(json, ItsRestJson.Settings);
        var reply = dto?.Reply;
        if (string.IsNullOrWhiteSpace(reply))
        {
            PublishAskFailure(
                _pendingAskCorrelationId,
                "ITS /ask returned an empty reply.",
                "Nao consegui formular uma resposta agora. Tente reformular sua pergunta.");
            return;
        }

        _isAwaitingAskResult = false;
        OnAskReply?.Invoke(reply);
        PublishThinkingState(_pendingAskCorrelationId, false);
        PublishAskResult(
            _pendingAskCorrelationId,
            success: true,
            reply: reply,
            error: string.Empty,
            audioUrl: ResolveAudioUrl(dto?.AudioUrl));
        _pendingAskCorrelationId = string.Empty;
    }

    IEnumerator Post(string path, string json, Action<string, bool> callback)
    {
        var url = _baseUrl + path;
        var bytes = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.timeout = (int)_timeoutSeconds;

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            callback(req.downloadHandler.text, true);
        }
        else
        {
            Debug.LogWarning($"[ITSClient] {path} failed: {req.error}");
            if (string.Equals(path, "/ask", StringComparison.Ordinal))
            {
                PublishAskFailure(
                    _pendingAskCorrelationId,
                    req.error,
                    "Hmm, parece que perdi o sinal. Tente novamente em um momento.");
            }
            else
            {
                OnServerError?.Invoke(req.error);
            }
            callback(null, false);
        }
    }

    IEnumerator CheckServerHealth()
    {
        using var req = UnityWebRequest.Get(_baseUrl + "/health");
        req.timeout = 3;
        yield return req.SendWebRequest();

        _serverAvailable = req.result == UnityWebRequest.Result.Success;

        if (_serverAvailable)
            Debug.Log("[ITSClient] Server reachable.");
        else
            Debug.LogWarning("[ITSClient] Server not reachable — Ask disabled until available.");
    }

    static string BuildLocalFallbackStudentId() =>
        $"local_{Guid.NewGuid():N}";

    void HandleTranscriptionReadyEvent(TranscriptionReadyEventData eventData)
    {
        var text = eventData.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            PublishAskFailure(
                eventData.Context.CorrelationId,
                "Empty transcription event.",
                "Nao entendi. Tente perguntar de novo.",
                emitServerError: false);
            return;
        }

        if (!CanPostAsk)
            return;

        var tracker = SkillTracker.Instance;
        var studentId = tracker != null && tracker.HasActiveSession ? tracker.StudentId : string.Empty;
        if (string.IsNullOrWhiteSpace(studentId))
        {
            PublishAskFailure(
                eventData.Context.CorrelationId,
                "Missing active student session.",
                "Inicie uma nova sessao no menu antes de conversar comigo.",
                emitServerError: false);
            return;
        }

        var levelId = tracker?.GetCurrentLevelId() ?? LevelID.MoveLeftRight;
        var correlationId = string.IsNullOrWhiteSpace(eventData.Context.CorrelationId)
            ? BuildCorrelationId("ask")
            : eventData.Context.CorrelationId;

        PublishAskRequested(correlationId, studentId, levelId, text);
        Ask(studentId, levelId, text, correlationId);
    }

    void EchoTranscription(string correlationId, string text)
    {
        OnAskReply?.Invoke(text);
        PublishThinkingState(correlationId, false);
        PublishAskResult(correlationId, success: true, reply: text, error: string.Empty);
        Debug.Log($"[ITSClient] /ask skipped; echoing STT via TTS chars={text.Length}.");
    }

    void PublishAskRequested(string correlationId, string studentId, string levelId, string question)
    {
        if (_askRequestedChannel == null)
            return;

        var payload = new AskRequestedEventData(
            EventContextFactory.Create(nameof(ITSClient), correlationId),
            studentId,
            levelId,
            question);
        _askRequestedChannel.Raise(payload, this);
    }

    void PublishAskResult(
        string correlationId,
        bool success,
        string reply,
        string error,
        string audioUrl = null)
    {
        if (_askResultChannel == null)
            return;

        var payload = new AskResultEventData(
            EventContextFactory.Create(nameof(ITSClient), correlationId),
            success,
            reply,
            error,
            audioUrl);
        _askResultChannel.Raise(payload, this);
    }

    void PublishThinkingState(string correlationId, bool isThinking)
    {
        if (_thinkingStateChannel == null)
            return;

        var payload = new ThinkingStateChangedEventData(
            EventContextFactory.Create(nameof(ITSClient), correlationId),
            isThinking);
        _thinkingStateChannel.Raise(payload, this);
    }

    void PublishAskFailure(
        string correlationId,
        string technicalError,
        string userFacingMessage,
        bool emitServerError = true)
    {
        if (!_isAwaitingAskResult && string.IsNullOrWhiteSpace(correlationId))
            correlationId = BuildCorrelationId("ask-fail");

        _isAwaitingAskResult = false;
        if (emitServerError)
            OnServerError?.Invoke(technicalError);
        PublishThinkingState(correlationId, false);
        PublishAskResult(correlationId, success: false, reply: string.Empty, error: userFacingMessage);
        _pendingAskCorrelationId = string.Empty;
    }

    string ResolveAudioUrl(string audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
            return string.Empty;

        var trimmed = audioUrl.Trim();
        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        var root = (_baseUrl ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrEmpty(root))
            return trimmed;

        return $"{root}/{trimmed.TrimStart('/')}";
    }

    static string BuildCorrelationId(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}";
}
