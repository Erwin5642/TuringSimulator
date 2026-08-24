// VoiceInputHandler.cs
// Speech-to-Text for Meta Quest 3 using Meta Voice SDK + Wit.ai.
//
// SETUP (do this once in the Unity Editor before building):
//   1. Install Meta XR All-in-One SDK via Package Manager.
//   2. Use two Wit apps: turing_stt (Portuguese STT) and turing_tts (English TTS).
//   3. Assign stt_witconfig only on AppVoiceExperience (this component).
//   4. Assign tts_witconfig only on TTS/TTSWitService (AgentTTS / TTSSpeaker).
//   5. Assign the AppVoiceExperience reference to _voiceExperience.
//
// USAGE:
//   VoiceInputHandler.Instance.StartListening();  // called by UI button
//   VoiceInputHandler.Instance.StopListening();
//   Subscribe to OnTranscriptionReady to receive the Portuguese text.
//
// COMMIT RULE:
//   Text is the latest string Meta Voice emitted (no joining). TranscriptionReady
//   (ITS /ask or echo TTS) fires only after _silenceCommitSeconds of no new
//   STT, or when the player stops Shaka / T.

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
    [SerializeField] private VoiceCaptureStoppedEventChannel _voiceCaptureStoppedChannel;

    [Header("Commit")]
    [Tooltip("Seconds without new STT before TranscriptionReady is raised. 0 = only commit when Shaka/T stops.")]
    [SerializeField] private float _silenceCommitSeconds = 15f;

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

    /// <summary>
    /// Fires when Wit stops capturing while the Shaka/T session is still open.
    /// </summary>
    public event Action OnCaptureStopped;

    // ── State ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// True while the player session is open (T on, Shaka held), even if Wit
    /// already endpointed a short utterance.
    /// </summary>
    public bool IsListening => _sessionActive;

    public bool CanListen => _voiceExperience != null;

    private bool _sessionActive;
    private VoiceUtteranceBufferData _buffer = VoiceUtteranceBufferData.Empty;
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
            _voiceExperience = FindAnyObjectByType<AppVoiceExperience>();

        if (_voiceExperience == null)
        {
            Debug.LogError("[VoiceInputHandler] AppVoiceExperience not found. Add one and assign Wit configuration.");
            return;
        }

        _voiceExperience.VoiceEvents.OnStartListening.AddListener(HandleListeningStarted);
        _voiceExperience.VoiceEvents.OnStoppedListening.AddListener(HandleListeningStopped);
        _voiceExperience.VoiceEvents.OnPartialTranscription.AddListener(HandlePartial);
        _voiceExperience.VoiceEvents.OnFullTranscription.AddListener(HandleFull);
        _voiceExperience.VoiceEvents.OnError.AddListener(HandleError);
    }

    private void Update()
    {
        if (!_sessionActive)
            return;

        if (VoiceTranscriptionCommit.ShouldCommitOnSilence(
                _buffer,
                Time.unscaledTime,
                _silenceCommitSeconds))
        {
            Debug.Log("[VoiceInputHandler] Committing after silence.");
            EndSessionAndCommit();
        }
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
        if (_voiceExperience == null || _sessionActive) return;

        _sessionActive = true;
        _buffer = VoiceUtteranceBufferData.Empty;
        _voiceExperience.ActivateImmediately();

        OnListeningStarted?.Invoke();
        PublishListeningState(true);
        Debug.Log("[VoiceInputHandler] Listening started.");
    }

    /// <summary>
    /// Stop listening manually (e.g. player releases Shaka / second T)
    /// and send the latest Wit STT to ITS / echo TTS.
    /// </summary>
    public void StopListening()
    {
        if (!_sessionActive) return;
        EndSessionAndCommit();
    }

    // ── Wit.ai event handlers ─────────────────────────────────────────────────

    private void HandleListeningStarted()
    {
        if (!_sessionActive)
        {
            _voiceExperience?.Deactivate();
            return;
        }

        if (string.IsNullOrWhiteSpace(_activeCorrelationId))
            _activeCorrelationId = BuildCorrelationId("voice");
    }

    private void HandleListeningStopped()
    {
        if (!_sessionActive)
            return;

        if (VoiceTranscriptionCommit.ShouldCommitOnSilence(
                _buffer,
                Time.unscaledTime,
                _silenceCommitSeconds))
        {
            Debug.Log("[VoiceInputHandler] Committing after Wit stop + silence.");
            EndSessionAndCommit();
            return;
        }

        NotifyCaptureStopped();
    }

    private void HandlePartial(string partial) => CaptureSpeech(partial);

    private void HandleFull(string transcription)
    {
        CaptureSpeech(transcription);
    }

    private void CaptureSpeech(string incoming)
    {
        if (!_sessionActive || string.IsNullOrWhiteSpace(incoming))
            return;

        _buffer = VoiceTranscriptionCommit.Capture(incoming, Time.unscaledTime);
        var text = VoiceTranscriptionCommit.ResolveCommitText(_buffer);
        if (string.IsNullOrEmpty(text))
            return;

        OnPartialTranscription?.Invoke(text);
        PublishPartialTranscription(text);
    }

    private void HandleError(string code, string message)
    {
        Debug.LogWarning($"[VoiceInputHandler] Wit.ai error {code}: {message}");
        if (_sessionActive)
            EndSessionAndCommit();
    }

    private void HandleMicToggleRequested(MicToggleRequestedEventData eventData)
    {
        _activeCorrelationId = string.IsNullOrWhiteSpace(eventData.Context.CorrelationId)
            ? BuildCorrelationId("voice")
            : eventData.Context.CorrelationId;

        switch (eventData.Mode)
        {
            case MicListenMode.Start:
                AgentTTS.Instance?.Stop();
                StartListening();
                break;
            case MicListenMode.Stop:
                StopListening();
                break;
            default:
                if (IsListening)
                {
                    StopListening();
                }
                else
                {
                    AgentTTS.Instance?.Stop();
                    StartListening();
                }
                break;
        }
    }

    private void EndSessionAndCommit()
    {
        if (!_sessionActive)
            return;

        _sessionActive = false;

        if (_voiceExperience != null)
            _voiceExperience.Deactivate();

        var text = VoiceTranscriptionCommit.ResolveCommitText(_buffer);
        _buffer = VoiceUtteranceBufferData.Empty;

        OnListeningStopped?.Invoke();
        PublishListeningState(false);
        Debug.Log("[VoiceInputHandler] Listening stopped.");

        if (string.IsNullOrEmpty(text))
            return;

        Debug.Log($"[VoiceInputHandler] Transcription: \"{text}\"");
        OnTranscriptionReady?.Invoke(text);
        PublishTranscription(text);
    }

    private void PublishListeningState(bool isListening)
    {
        if (_listeningStateChannel == null)
            return;

        var payload = new ListeningStateChangedEventData(
            BuildContext("listening-state"),
            isListening);
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
        _partialTranscriptionChannel.Raise(payload, this);
    }

    private void PublishTranscription(string text)
    {
        if (_transcriptionReadyChannel == null)
            return;

        var payload = new TranscriptionReadyEventData(
            BuildContext("transcription"),
            text);
        _transcriptionReadyChannel.Raise(payload, this);
    }

    private void NotifyCaptureStopped()
    {
        OnCaptureStopped?.Invoke();
        if (_voiceCaptureStoppedChannel == null)
            return;

        var heard = VoiceTranscriptionCommit.ResolveCommitText(_buffer);
        var payload = new VoiceCaptureStoppedEventData(
            BuildContext("capture-stopped"),
            heard);
        _voiceCaptureStoppedChannel.Raise(payload, this);
        Debug.Log("[VoiceInputHandler] Wit capture stopped; waiting for Shaka/T to send.");
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
