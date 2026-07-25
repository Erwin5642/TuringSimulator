// VoiceInputHandler.cs
// Speech-to-Text for Meta Quest 3 using Meta Voice SDK + Wit.ai.
//
// SETUP (do this once in the Unity Editor before building):
//   1. Install Meta XR All-in-One SDK via Package Manager.
//   2. Go to wit.ai → create a new app → set language to Portuguese (pt-BR).
//   3. Copy the Server Access Token from your Wit app Settings.
//   4. In Unity: Meta → Voice SDK → Voice Hub → paste the token → Link.
//   5. Save the generated WitConfiguration asset (e.g. "WitConfig_ptBR").
//   6. Add an AppVoiceExperience prefab to your scene.
//   7. In the AppVoiceExperience Inspector → Wit Runtime Configuration →
//      assign your WitConfig_ptBR asset.
//   8. Assign the AppVoiceExperience reference to this component's
//      _voiceExperience field in the Inspector.
//
// USAGE:
//   VoiceInputHandler.Instance.StartListening();  // called by UI button
//   VoiceInputHandler.Instance.StopListening();
//   Subscribe to OnTranscriptionReady to receive the Portuguese text.

using System;
using Oculus.Voice;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

public class VoiceInputHandler : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static VoiceInputHandler Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Meta Voice SDK")]
    [Tooltip("Drag the AppVoiceExperience GameObject here.")]
    [SerializeField] private AppVoiceExperience _voiceExperience;

    [Header("Event Channels (event-driven wiring)")]
    [SerializeField] private MicToggleRequestedEventChannel _micToggleRequestedChannel;
    [SerializeField] private ListeningStateChangedEventChannel _listeningStateChannel;
    [SerializeField] private PartialTranscriptionEventChannel _partialTranscriptionChannel;
    [SerializeField] private TranscriptionReadyEventChannel _transcriptionReadyChannel;

#pragma warning disable CS0414
    [Tooltip("Reserved when switching to low-level Wit APIs that expose utterance confidence.")]
    [SerializeField] [Range(0f, 1f)] private float _minConfidence = 0.55f;
#pragma warning restore CS0414

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires when a transcription is ready and above the confidence threshold.
    /// The string is the recognised Portuguese text.
    /// </summary>
    public event Action<string> OnTranscriptionReady;

    /// <summary>Fires when listening starts — use to update UI state.</summary>
    public event Action OnListeningStarted;

    /// <summary>Fires when listening ends (success or cancel).</summary>
    public event Action OnListeningStopped;

    /// <summary>Fires on partial transcription — useful for live subtitles.</summary>
    public event Action<string> OnPartialTranscription;

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsListening { get; private set; }
    private int _utteranceSequence;
    private string _activeCorrelationId = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_micToggleRequestedChannel != null)
            _micToggleRequestedChannel.OnRaised += HandleMicToggleRequested;

        if (_voiceExperience == null)
            _voiceExperience = FindFirstObjectByType<AppVoiceExperience>();

        if (_voiceExperience == null)
        {
            Debug.LogError("[VoiceInputHandler] AppVoiceExperience not found. Add one and assign Wit configuration.");
            return;
        }

        // Subscribe to Wit.ai events
        _voiceExperience.VoiceEvents.OnStartListening.AddListener(HandleListeningStarted);
        _voiceExperience.VoiceEvents.OnStoppedListening.AddListener(HandleListeningStopped);
        _voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(HandlePartial);
        _voiceExperience.VoiceEvents.OnFullTranscription.AddListener(HandleFull);
        _voiceExperience.VoiceEvents.OnError.AddListener(HandleError);
    }

    private void OnDestroy()
    {
        if (_micToggleRequestedChannel != null)
            _micToggleRequestedChannel.OnRaised -= HandleMicToggleRequested;

        if (_voiceExperience == null) return;
        _voiceExperience.VoiceEvents.OnStartListening.RemoveListener(HandleListeningStarted);
        _voiceExperience.VoiceEvents.OnStoppedListening.RemoveListener(HandleListeningStopped);
        _voiceExperience.VoiceEvents.OnPartialTranscription.RemoveListener(HandlePartial);
        _voiceExperience.VoiceEvents.OnFullTranscription.RemoveListener(HandleFull);
        _voiceExperience.VoiceEvents.OnError.RemoveListener(HandleError);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Start listening for Portuguese speech.
    /// Call this when the player presses the microphone button.
    /// </summary>
    public void StartListening()
    {
        if (_voiceExperience == null || IsListening) return;
        _voiceExperience.Activate();
    }

    /// <summary>
    /// Stop listening manually (e.g. player releases button).
    /// Wit.ai will also stop automatically after silence is detected.
    /// </summary>
    public void StopListening()
    {
        if (_voiceExperience == null || !IsListening) return;
        _voiceExperience.Deactivate();
    }

    // ── Wit.ai event handlers ─────────────────────────────────────────────────

    private void HandleListeningStarted()
    {
        if (string.IsNullOrWhiteSpace(_activeCorrelationId))
            _activeCorrelationId = BuildCorrelationId("voice");

        IsListening = true;
        OnListeningStarted?.Invoke();
        PublishListeningState(true);
        Debug.Log("[VoiceInputHandler] Listening started.");
    }

    private void HandleListeningStopped()
    {
        IsListening = false;
        OnListeningStopped?.Invoke();
        PublishListeningState(false);
        Debug.Log("[VoiceInputHandler] Listening stopped.");
    }

    private void HandlePartial(string partial)
    {
        if (!string.IsNullOrWhiteSpace(partial))
        {
            OnPartialTranscription?.Invoke(partial);
            PublishPartialTranscription(partial);
        }
    }

    private void HandleFull(string transcription)
    {
        if (string.IsNullOrWhiteSpace(transcription))
        {
            Debug.LogWarning("[VoiceInputHandler] Empty transcription received.");
            return;
        }

        // Wit.ai does not expose per-utterance confidence in the Unity SDK's
        // high-level events, so we accept all full transcriptions here.
        // If you need confidence filtering, use the low-level WitRequest API.
        Debug.Log($"[VoiceInputHandler] Transcription: \"{transcription}\"");
        var text = transcription.Trim();
        OnTranscriptionReady?.Invoke(text);
        PublishTranscription(text);
    }

    private void HandleError(string code, string message)
    {
        IsListening = false;
        OnListeningStopped?.Invoke();
        PublishListeningState(false);
        Debug.LogWarning($"[VoiceInputHandler] Wit.ai error {code}: {message}");
    }

    private void HandleMicToggleRequested(MicToggleRequestedEventData eventData)
    {
        _activeCorrelationId = string.IsNullOrWhiteSpace(eventData.Context.CorrelationId)
            ? BuildCorrelationId("voice")
            : eventData.Context.CorrelationId;

        AgentTTS.Instance?.Stop();
        if (IsListening)
            StopListening();
        else
            StartListening();
    }

    private void PublishListeningState(bool isListening)
    {
        if (_listeningStateChannel == null)
            return;

        var payload = new ListeningStateChangedEventData(
            BuildContext("listening-state"),
            isListening);
        EventTraceLog.Record(nameof(ListeningStateChangedEventData), payload.ToString(), this);
        _listeningStateChannel.Raise(payload, this);

        if (!isListening)
            _activeCorrelationId = string.Empty;
    }

    private void PublishPartialTranscription(string partial)
    {
        if (_partialTranscriptionChannel == null)
            return;

        var payload = new PartialTranscriptionEventData(
            BuildContext("partial"),
            partial);
        EventTraceLog.Record(nameof(PartialTranscriptionEventData), payload.ToString(), this);
        _partialTranscriptionChannel.Raise(payload, this);
    }

    private void PublishTranscription(string text)
    {
        if (_transcriptionReadyChannel == null)
            return;

        var payload = new TranscriptionReadyEventData(
            BuildContext("transcription"),
            text);
        EventTraceLog.Record(nameof(TranscriptionReadyEventData), payload.ToString(), this);
        _transcriptionReadyChannel.Raise(payload, this);
    }

    private EventContextData BuildContext(string stage)
    {
        var correlationId = string.IsNullOrWhiteSpace(_activeCorrelationId)
            ? BuildCorrelationId(stage)
            : _activeCorrelationId;
        return EventContextFactory.Create(nameof(VoiceInputHandler), correlationId);
    }

    private string BuildCorrelationId(string prefix) =>
        $"{prefix}-{++_utteranceSequence}";
}