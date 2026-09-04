using TMPro;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

/// <summary>
/// Shows live STT on the right-palm TextMeshPro while Shaka is held or T listening is on.
/// Appends a UI-only "Cambio" when Wit capture stops; that cue is not sent to ITS.
/// </summary>
public sealed class PalmVoiceCaptionView : MonoBehaviour, IPalmVoiceCaptionView
{
    [Header("UI")]
    [SerializeField] private TMP_Text _label;

    [Header("Event Channels")]
    [SerializeField] private HandGesturePerformedEventChannel _handGestureChannel;
    [SerializeField] private ListeningStateChangedEventChannel _listeningStateChannel;
    [SerializeField] private PartialTranscriptionEventChannel _partialTranscriptionChannel;
    [SerializeField] private VoiceCaptureStoppedEventChannel _voiceCaptureStoppedChannel;

    [Header("Gesture")]
    [SerializeField] private string _gestureId = "Shaka";

    [Header("UI-only stop cue")]
    [SerializeField] private string _stoppedCue = PalmVoiceCaption.StoppedCue;

    bool _visible;
    bool _captureStopped;
    bool _gestureHeld;
    bool _listening;
    string _recordedText = string.Empty;

    void Awake()
    {
        if (_label == null)
            _label = GetComponent<TMP_Text>();
        Hide();
    }

    void OnEnable()
    {
        if (_handGestureChannel != null)
            _handGestureChannel.OnRaised += HandleGesture;
        if (_listeningStateChannel != null)
            _listeningStateChannel.OnRaised += HandleListening;
        if (_partialTranscriptionChannel != null)
            _partialTranscriptionChannel.OnRaised += HandlePartial;
        if (_voiceCaptureStoppedChannel != null)
            _voiceCaptureStoppedChannel.OnRaised += HandleCaptureStopped;
    }

    void OnDisable()
    {
        if (_handGestureChannel != null)
            _handGestureChannel.OnRaised -= HandleGesture;
        if (_listeningStateChannel != null)
            _listeningStateChannel.OnRaised -= HandleListening;
        if (_partialTranscriptionChannel != null)
            _partialTranscriptionChannel.OnRaised -= HandlePartial;
        if (_voiceCaptureStoppedChannel != null)
            _voiceCaptureStoppedChannel.OnRaised -= HandleCaptureStopped;
        Hide();
    }

    public void HandleGesture(HandGesturePerformedEventData eventData)
    {
        if (!PalmVoiceCaption.TryMatchGesture(
                eventData.GestureId,
                eventData.Phase,
                _gestureId,
                out var show))
            return;

        _gestureHeld = show;
        SyncVisibility();
    }

    public void HandleListening(ListeningStateChangedEventData eventData)
    {
        _listening = eventData.IsListening;
        SyncVisibility();
    }

    public void HandlePartial(PartialTranscriptionEventData eventData)
    {
        if (!_visible || _captureStopped)
            return;

        _recordedText = PalmVoiceCaption.FormatLive(eventData.PartialText);
        ApplyText(_recordedText);
    }

    public void HandleCaptureStopped(VoiceCaptureStoppedEventData eventData)
    {
        if (!_visible)
            return;

        var heard = PalmVoiceCaption.FormatLive(eventData.HeardText);
        if (!string.IsNullOrEmpty(heard))
            _recordedText = heard;

        _captureStopped = true;
        ApplyText(PalmVoiceCaption.AppendStoppedCue(_recordedText, _stoppedCue));
    }

    void SyncVisibility()
    {
        var shouldShow = PalmVoiceCaption.ShouldShowCaption(_gestureHeld, _listening);
        if (shouldShow)
        {
            if (!_visible)
                Show();
            return;
        }

        if (_visible)
            Hide();
    }

    void Show()
    {
        _visible = true;
        _captureStopped = false;
        _recordedText = string.Empty;
        ApplyText(string.Empty);
        SetLabelEnabled(true);
    }

    void Hide()
    {
        _visible = false;
        _captureStopped = false;
        _gestureHeld = false;
        _listening = false;
        _recordedText = string.Empty;
        ApplyText(string.Empty);
        SetLabelEnabled(false);
    }

    void ApplyText(string text)
    {
        if (_label != null)
            _label.text = text ?? string.Empty;
    }

    void SetLabelEnabled(bool enabled)
    {
        if (_label != null)
            _label.enabled = enabled;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_label == null)
            Debug.LogWarning($"{name}: assign a TMP_Text for the palm caption.", this);
        if (_handGestureChannel == null)
            Debug.LogWarning($"{name}: assign HandGesturePerformedEventChannel.", this);
        if (_listeningStateChannel == null)
            Debug.LogWarning($"{name}: assign ListeningStateChangedEventChannel.", this);
        if (_partialTranscriptionChannel == null)
            Debug.LogWarning($"{name}: assign PartialTranscriptionEventChannel.", this);
        if (_voiceCaptureStoppedChannel == null)
            Debug.LogWarning($"{name}: assign VoiceCaptureStoppedEventChannel.", this);
        if (string.IsNullOrWhiteSpace(_gestureId))
            Debug.LogWarning($"{name}: GestureId should not be empty.", this);
    }
#endif
}
