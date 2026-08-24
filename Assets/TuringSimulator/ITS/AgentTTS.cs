// AgentTTS.cs
// Synthesizes tutor speech through the scene TTSSpeaker (Wit.ai / Voice SDK).
//
// USAGE:
//   AgentTTS.Instance.Speak("Olá, treineiro!");
//   AgentTTS.Instance.Stop();

using System;
using System.Collections;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Utilities;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class AgentTTS : MonoBehaviour, IAgentSpeech
{
    public static AgentTTS Instance { get; private set; }

    [Header("Wit TTS")]
    [Tooltip("Scene TTSSpeaker under the TTS root. Required for speech.")]
    [SerializeField] private TTSSpeaker _ttsSpeaker;

    [Tooltip("If Wit never starts playback, stop and finish so animation/subtitles do not hang.")]
    [SerializeField] [Min(1f)] private float _loadTimeoutSeconds = 10f;

    public event Action<string> OnSpeechStarted;
    public event Action OnSpeechFinished;
    public event Action<string> OnSpeechError;

    public bool IsSpeaking { get; private set; }

    private int _speakGeneration;
    private bool _suppressEvents;
    private bool _playbackStarted;
    private bool _subscribed;
    private Coroutine _timeoutRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_ttsSpeaker == null)
            _ttsSpeaker = FindAnyObjectByType<TTSSpeaker>();

        if (transform.parent != null)
            transform.SetParent(null, true);

        PersistTtsHierarchy();
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this)
            Instance = null;
    }

    public void Speak(string text, string audioUrl = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!string.IsNullOrWhiteSpace(audioUrl))
            Debug.Log("[AgentTTS] Ignoring audioUrl; speech uses Wit TTS.");

        if (_ttsSpeaker == null)
        {
            Debug.LogError("[AgentTTS] TTSSpeaker is not assigned.");
            OnSpeechError?.Invoke("TTSSpeaker is not assigned.");
            return;
        }

        InterruptCurrent(invokeFinishedIfSpeaking: true);

        var generation = _speakGeneration;
        _playbackStarted = false;
        IsSpeaking = true;
        OnSpeechStarted?.Invoke(text);
        EventTraceLog.Record("AgentSpeechStarted", text, this);
        Debug.Log($"[AgentTTS] Speak requested chars={text.Length} voice={_ttsSpeaker.VoiceID}.");

        _ttsSpeaker.Speak(text);
        _timeoutRoutine = StartCoroutine(WatchLoadTimeout(generation));
    }

    public void Stop()
    {
        InterruptCurrent(invokeFinishedIfSpeaking: true);
    }

    [ContextMenu("Speak Debug Sample")]
    private void SpeakDebugSample()
    {
        Speak("Olá, treineiro. Este é um teste da voz do tutor.");
    }

    private void PersistTtsHierarchy()
    {
        if (_ttsSpeaker == null)
            return;

        var ttsRoot = _ttsSpeaker.transform.root;
        if (ttsRoot != null && ttsRoot != transform)
            DontDestroyOnLoad(ttsRoot.gameObject);
    }

    private void Subscribe()
    {
        if (_subscribed || _ttsSpeaker == null || _ttsSpeaker.Events == null)
            return;

        _ttsSpeaker.Events.OnPlaybackStart.AddListener(HandlePlaybackStart);
        _ttsSpeaker.Events.OnLoadFailed.AddListener(HandleLoadFailed);
        _ttsSpeaker.Events.OnComplete.AddListener(HandleComplete);
        _ttsSpeaker.Events.OnPlaybackQueueComplete.AddListener(HandleQueueComplete);
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _ttsSpeaker == null || _ttsSpeaker.Events == null)
        {
            _subscribed = false;
            return;
        }

        _ttsSpeaker.Events.OnPlaybackStart.RemoveListener(HandlePlaybackStart);
        _ttsSpeaker.Events.OnLoadFailed.RemoveListener(HandleLoadFailed);
        _ttsSpeaker.Events.OnComplete.RemoveListener(HandleComplete);
        _ttsSpeaker.Events.OnPlaybackQueueComplete.RemoveListener(HandleQueueComplete);
        _subscribed = false;
    }

    private void InterruptCurrent(bool invokeFinishedIfSpeaking)
    {
        _speakGeneration++;
        _playbackStarted = false;
        StopTimeout();

        _suppressEvents = true;
        _ttsSpeaker?.Stop();
        _suppressEvents = false;

        if (invokeFinishedIfSpeaking && IsSpeaking)
        {
            IsSpeaking = false;
            OnSpeechFinished?.Invoke();
        }
        else
        {
            IsSpeaking = false;
        }
    }

    private IEnumerator WatchLoadTimeout(int generation)
    {
        var elapsed = 0f;
        var timeout = Mathf.Max(1f, _loadTimeoutSeconds);
        while (generation == _speakGeneration && elapsed < timeout && !_playbackStarted)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _timeoutRoutine = null;
        if (generation != _speakGeneration || _playbackStarted)
            yield break;

        Debug.LogWarning("[AgentTTS] Wit TTS load timed out.");
        OnSpeechError?.Invoke("TTS load timed out.");
        _ttsSpeaker?.Stop();
        FinishIfIdle();
    }

    private void HandlePlaybackStart(TTSSpeaker speaker, TTSClipData clip)
    {
        if (_suppressEvents)
            return;

        _playbackStarted = true;
        StopTimeout();
    }

    private void HandleLoadFailed(TTSSpeaker speaker, TTSClipData clip, string error)
    {
        if (_suppressEvents)
            return;

        var message = string.IsNullOrWhiteSpace(error) ? "TTS load failed." : error;
        Debug.LogWarning($"[AgentTTS] Wit TTS load failed: {message}");
        OnSpeechError?.Invoke(message);
        FinishIfIdle();
    }

    private void HandleComplete(TTSSpeaker speaker, TTSClipData clip)
    {
        FinishIfIdle();
    }

    private void HandleQueueComplete()
    {
        FinishIfIdle();
    }

    private void FinishIfIdle()
    {
        if (_suppressEvents || !IsSpeaking)
            return;
        if (_ttsSpeaker != null && _ttsSpeaker.IsActive)
            return;

        StopTimeout();
        IsSpeaking = false;
        OnSpeechFinished?.Invoke();
        Debug.Log("[AgentTTS] Playback finished.");
    }

    private void StopTimeout()
    {
        if (_timeoutRoutine == null)
            return;

        StopCoroutine(_timeoutRoutine);
        _timeoutRoutine = null;
    }
}
