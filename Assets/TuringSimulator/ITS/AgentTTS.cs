// AgentTTS.cs
// Text-to-Speech for the ITS agent using Android's built-in TTS engine.
// Targets Brazilian Portuguese (pt-BR) which has robust support on Android
// (and therefore on Meta Quest 3, which runs Android).
//
// No external SDK or API key required — Android TTS is entirely on-device.
//
// SETUP:
//   No Unity-side setup needed. Android TTS initialises automatically.
//   On the Quest headset, go to Settings → Accessibility → Text-to-Speech
//   and confirm a Portuguese TTS engine is installed. The Google TTS engine
//   (pre-installed on most Quest devices) includes pt-BR.
//
// USAGE:
//   AgentTTS.Instance.Speak("Olá, treineiro! Precisa de ajuda?");
//   AgentTTS.Instance.Stop();
//   Subscribe to OnSpeechStarted / OnSpeechFinished for UI sync.

using System;
using System.Collections;
using UnityEngine;

public class AgentTTS : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static AgentTTS Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Voice settings")]
    [Tooltip("BCP-47 language tag for Brazilian Portuguese.")]
    [SerializeField] private string _languageTag = "pt-BR";

    [Tooltip("Speech rate. 1.0 = normal, 0.85 = slightly slower (clearer for learners).")]
    [SerializeField] [Range(0.5f, 2.0f)] private float _speechRate = 0.9f;

    [Tooltip("Pitch. 1.0 = normal. Slightly higher = friendlier robot voice.")]
    [SerializeField] [Range(0.5f, 2.0f)] private float _pitch = 1.1f;

    // ── Events ───────────────────────────────────────────────────────────────

    public event Action<string> OnSpeechStarted;
    public event Action         OnSpeechFinished;
    public event Action<string> OnTTSError;

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsSpeaking { get; private set; }

    // ── Android JNI references ────────────────────────────────────────────────

    private AndroidJavaObject _tts;
    private AndroidJavaObject _unityActivity;
    private bool              _ttsReady;

    // Unique utterance ID used to track completion callbacks
    private const string UTT_ID = "ITS_AGENT";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitAndroidTTS();
#else
        // In the Editor, log the text instead of speaking it
        Debug.Log("[AgentTTS] Running in Editor — TTS output will be logged only.");
        _ttsReady = true;
#endif
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ShutdownTTS();
#endif
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Speak a string in Brazilian Portuguese.
    /// Interrupts any currently playing speech.
    /// </summary>
    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_ttsReady)
        {
            Debug.LogWarning("[AgentTTS] TTS not ready yet — queuing.");
            StartCoroutine(SpeakWhenReady(text));
            return;
        }
        SpeakNative(text);
#else
        // Editor fallback
        Debug.Log($"[AgentTTS] SPEAK: {text}");
        OnSpeechStarted?.Invoke(text);
        OnSpeechFinished?.Invoke();
#endif
    }

    /// <summary>Stop any ongoing speech immediately.</summary>
    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _tts?.Call("stop");
#endif
        IsSpeaking = false;
        OnSpeechFinished?.Invoke();
    }

    // ── Android TTS internals ─────────────────────────────────────────────────

#if UNITY_ANDROID && !UNITY_EDITOR

    private void InitAndroidTTS()
    {
        try
        {
            using var playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _unityActivity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");

            // OnInitListener implemented via proxy
            var initListener = new TTSInitListener(OnTTSInit);
            _tts = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                _unityActivity,
                initListener
            );
        }
        catch (Exception e)
        {
            Debug.LogError($"[AgentTTS] Failed to initialise Android TTS: {e.Message}");
            OnTTSError?.Invoke(e.Message);
        }
    }

    private void OnTTSInit(int status)
    {
        // status 0 = SUCCESS
        if (status != 0)
        {
            Debug.LogError($"[AgentTTS] TTS init failed with status {status}.");
            OnTTSError?.Invoke($"TTS init failed: {status}");
            return;
        }

        // Set language to pt-BR
        using var locale = new AndroidJavaObject("java.util.Locale", _languageTag);
        int langResult = _tts.Call<int>("setLanguage", locale);

        if (langResult == -2 || langResult == -1)
        {
            Debug.LogWarning($"[AgentTTS] pt-BR not supported (result={langResult}). " +
                             "Falling back to device default language.");
        }

        // Set speech rate and pitch
        _tts.Call<int>("setSpeechRate", _speechRate);
        _tts.Call<int>("setPitch", _pitch);

        // Register utterance progress listener for completion callbacks
        var progressListener = new TTSProgressListener(
            onStart   : id => { IsSpeaking = true;  OnSpeechStarted?.Invoke(id); },
            onDone    : id => { IsSpeaking = false; OnSpeechFinished?.Invoke();  },
            onError   : id => { IsSpeaking = false; OnTTSError?.Invoke(id);      }
        );
        _tts.Call<int>("setOnUtteranceProgressListener", progressListener);

        _ttsReady = true;
        Debug.Log("[AgentTTS] Android TTS ready — pt-BR.");
    }

    private void SpeakNative(string text)
    {
        // Stop any current speech first (FLUSH mode = 0)
        _tts.Call<int>("speak", text, 0, null, UTT_ID);
        IsSpeaking = true;
        OnSpeechStarted?.Invoke(text);
    }

    private void ShutdownTTS()
    {
        if (_tts == null) return;
        _tts.Call("stop");
        _tts.Call("shutdown");
        _tts.Dispose();
        _tts = null;
    }

#endif

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator SpeakWhenReady(string text)
    {
        float waited = 0f;
        while (!_ttsReady && waited < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }
        if (_ttsReady) Speak(text);
        else Debug.LogWarning("[AgentTTS] TTS did not become ready in time.");
    }
}

// ── Android JNI proxy classes ─────────────────────────────────────────────────
// These bridge Android Java interfaces to C# delegates using Unity's
// AndroidJavaProxy, which handles the JNI marshalling automatically.

#if UNITY_ANDROID && !UNITY_EDITOR

/// <summary>Proxies android.speech.tts.TextToSpeech.OnInitListener</summary>
internal class TTSInitListener : AndroidJavaProxy
{
    private readonly Action<int> _callback;

    public TTSInitListener(Action<int> callback)
        : base("android.speech.tts.TextToSpeech$OnInitListener")
    {
        _callback = callback;
    }

    // Called by Android on the main thread when TTS engine is ready
    public void onInit(int status) => _callback(status);
}

/// <summary>Proxies android.speech.tts.UtteranceProgressListener</summary>
internal class TTSProgressListener : AndroidJavaProxy
{
    private readonly Action<string> _onStart;
    private readonly Action<string> _onDone;
    private readonly Action<string> _onError;

    public TTSProgressListener(
        Action<string> onStart,
        Action<string> onDone,
        Action<string> onError)
        : base("android.speech.tts.UtteranceProgressListener")
    {
        _onStart = onStart;
        _onDone  = onDone;
        _onError = onError;
    }

    public void onStart(string utteranceId)   => _onStart(utteranceId);
    public void onDone(string utteranceId)    => _onDone(utteranceId);
    public void onError(string utteranceId)   => _onError(utteranceId);

    // Android API 21+ variant — required override
    public void onError(string utteranceId, int errorCode) => _onError(utteranceId);
}

#endif