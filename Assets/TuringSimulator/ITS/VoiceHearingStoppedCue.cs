using TuringSimulator.GameFlow.Events;
using UnityEngine;

/// <summary>
/// Plays a short cue when Wit stops capturing audio while the ask session
/// is still waiting for Shaka/T to send.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class VoiceHearingStoppedCue : MonoBehaviour
{
    [SerializeField] private VoiceCaptureStoppedEventChannel _voiceCaptureStoppedChannel;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private AgentDialogue _agentDialogue;
    [SerializeField] [TextArea] private string _hintText = "O microfone parou de ouvir.";

    void Awake()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();
        if (_audioSource != null)
        {
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }
    }

    void OnEnable()
    {
        if (_voiceCaptureStoppedChannel != null)
            _voiceCaptureStoppedChannel.OnRaised += HandleCaptureStopped;
    }

    void OnDisable()
    {
        if (_voiceCaptureStoppedChannel != null)
            _voiceCaptureStoppedChannel.OnRaised -= HandleCaptureStopped;
    }

    void HandleCaptureStopped(VoiceCaptureStoppedEventData _)
    {
        if (_audioSource != null && _clip != null)
            _audioSource.PlayOneShot(_clip);

        var dialogue = _agentDialogue != null ? _agentDialogue : AgentDialogue.Instance;
        dialogue?.SetListeningState(false);
        if (!string.IsNullOrWhiteSpace(_hintText))
            dialogue?.ShowSubtitle(_hintText.Trim());
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_voiceCaptureStoppedChannel == null)
            Debug.LogWarning($"{name}: assign VoiceCaptureStoppedEventChannel.", this);
        if (_clip == null)
            Debug.LogWarning($"{name}: assign a hearing-stopped AudioClip.", this);
    }
#endif
}
