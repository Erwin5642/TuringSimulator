// AgentTTS.cs
// Text-to-Speech for the ITS agent using Android's built-in TTS engine.
// Targets Brazilian Portuguese (pt-BR) with fallbacks when Quest lacks that voice.
//
// No external SDK or API key required — Android TTS is entirely on-device.
//
// SETUP:
//   1. Assets/Plugins/Android/TtsPackageVisibility.androidlib must declare
//      android.intent.action.TTS_SERVICE under <queries> (Android 11+ / Quest).
//   2. On the Quest headset: Settings → Accessibility → Text-to-Speech —
//      confirm a Portuguese TTS voice is installed when possible.
//
// USAGE:
//   AgentTTS.Instance.Speak("Olá, treineiro! Precisa de ajuda?");
//   AgentTTS.Instance.Stop();
//   Subscribe to OnSpeechStarted / OnSpeechFinished for UI sync.

using System;
using System.Collections;
using System.Threading;
using UnityEngine;

#pragma warning disable CS0414, CS0067

public class AgentTTS : MonoBehaviour
{
    public static AgentTTS Instance { get; private set; }

    [Header("Voice settings")]
    [Tooltip("BCP-47 language tag for Brazilian Portuguese.")]
    [SerializeField] private string _languageTag = "pt-BR";

    [Tooltip("Speech rate. 1.0 = normal, 0.85 = slightly slower (clearer for learners).")]
    [SerializeField] [Range(0.5f, 2.0f)] private float _speechRate = 0.9f;

    [Tooltip("Pitch. 1.0 = normal. Slightly higher = friendlier robot voice.")]
    [SerializeField] [Range(0.5f, 2.0f)] private float _pitch = 1.1f;

    public event Action<string> OnSpeechStarted;
    public event Action OnSpeechFinished;
    public event Action<string> OnTTSError;

    public bool IsSpeaking { get; private set; }

    private AndroidJavaObject _tts;
    private AndroidJavaObject _unityActivity;
    private bool _ttsReady;
    private string _activeLanguage = string.Empty;

    private int _pendingInitStatus = InitStatusNone;
    private int _pendingSpeechFinished;

    private const string UttId = "ITS_AGENT";
    private const int InitStatusNone = int.MinValue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // DontDestroyOnLoad only works on root objects.
        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        InitAndroidTTS();
#else
        Debug.Log("[AgentTTS] Running in Editor — TTS output will be logged only.");
        _ttsReady = true;
        _activeLanguage = _languageTag;
#endif
    }

    private void Update()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var status = Interlocked.Exchange(ref _pendingInitStatus, InitStatusNone);
        if (status != InitStatusNone)
            FinishAndroidInit(status);

        if (Interlocked.Exchange(ref _pendingSpeechFinished, 0) == 1)
        {
            IsSpeaking = false;
            OnSpeechFinished?.Invoke();
        }
#endif
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        ShutdownTTS();
#endif
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Speak a string. Interrupts any currently playing speech.
    /// </summary>
    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_ttsReady)
        {
            Debug.LogWarning("[AgentTTS] TTS not ready yet — queuing.");
            StartCoroutine(SpeakWhenReady(text));
            return;
        }

        SpeakNative(text);
#else
        Debug.Log($"[AgentTTS] SPEAK: {text}");
        OnSpeechStarted?.Invoke(text);
        OnSpeechFinished?.Invoke();
#endif
    }

    /// <summary>Stop any ongoing speech immediately.</summary>
    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            _tts?.Call("stop");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AgentTTS] stop() failed: {e.Message}");
        }
#endif
        IsSpeaking = false;
        OnSpeechFinished?.Invoke();
    }

#if UNITY_ANDROID && !UNITY_EDITOR

    private void InitAndroidTTS()
    {
        try
        {
            using var playerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _unityActivity = playerClass.GetStatic<AndroidJavaObject>("currentActivity");

            // Only enqueue status here — never touch AndroidJavaObject from the binder thread.
            var initListener = new TTSInitListener(status =>
                Interlocked.Exchange(ref _pendingInitStatus, status));

            _tts = new AndroidJavaObject(
                "android.speech.tts.TextToSpeech",
                _unityActivity,
                initListener);
        }
        catch (Exception e)
        {
            Debug.LogError($"[AgentTTS] Failed to initialise Android TTS: {e.Message}");
            OnTTSError?.Invoke(e.Message);
        }
    }

    private void FinishAndroidInit(int status)
    {
        if (status != 0)
        {
            Debug.LogError($"[AgentTTS] TTS init failed with status {status}.");
            OnTTSError?.Invoke($"TTS init failed: {status}");
            return;
        }

        if (_tts == null)
        {
            Debug.LogError("[AgentTTS] TTS object is null after init.");
            return;
        }

        try
        {
            _activeLanguage = ApplyLanguageWithFallback();
            _tts.Call<int>("setSpeechRate", _speechRate);
            _tts.Call<int>("setPitch", _pitch);
            TryAttachProgressListener();

            string engine = null;
            try
            {
                engine = _tts.Call<string>("getDefaultEngine");
            }
            catch
            {
                // Optional diagnostic.
            }

            _ttsReady = true;
            Debug.Log(
                $"[AgentTTS] Android TTS ready — requested={_languageTag} active={_activeLanguage} engine={engine ?? "unknown"}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AgentTTS] FinishAndroidInit failed: {e}");
            OnTTSError?.Invoke(e.Message);
        }
    }

    private string ApplyLanguageWithFallback()
    {
        using var localeClass = new AndroidJavaClass("java.util.Locale");

        // Preferred BCP-47 tag (e.g. pt-BR).
        if (TrySetLanguageTag(localeClass, _languageTag, out var chosen))
            return chosen;

        // Language only (pt).
        var dash = _languageTag != null ? _languageTag.IndexOf('-') : -1;
        if (dash > 0)
        {
            var languageOnly = _languageTag.Substring(0, dash);
            if (TrySetLanguageTag(localeClass, languageOnly, out chosen))
            {
                Debug.LogWarning(
                    $"[AgentTTS] '{_languageTag}' unavailable; fell back to '{languageOnly}'.");
                return chosen;
            }
        }

        // Device default — better than staying silent on Quest without pt-BR voice data.
        using var defaults = localeClass.CallStatic<AndroidJavaObject>("getDefault");
        var result = _tts.Call<int>("setLanguage", defaults);
        var defaultTag = SafeLocaleTag(defaults);
        Debug.LogWarning(
            $"[AgentTTS] Preferred languages unavailable (lastResult={result}). Using device default '{defaultTag}'.");
        return defaultTag;
    }

    private bool TrySetLanguageTag(AndroidJavaClass localeClass, string tag, out string appliedTag)
    {
        appliedTag = tag;
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        using var locale = localeClass.CallStatic<AndroidJavaObject>("forLanguageTag", tag);
        var result = _tts.Call<int>("setLanguage", locale);

        // 0+ = available / country or language available. -1 missing data, -2 not supported.
        if (result >= 0)
            return true;

        Debug.LogWarning($"[AgentTTS] Language '{tag}' not available (result={result}).");
        return false;
    }

    private static string SafeLocaleTag(AndroidJavaObject locale)
    {
        try
        {
            return locale.Call<string>("toLanguageTag");
        }
        catch
        {
            try
            {
                return locale.Call<string>("toString");
            }
            catch
            {
                return "unknown";
            }
        }
    }

    private void TryAttachProgressListener()
    {
        try
        {
            var progressListener = new TTSProgressListener(
                onStart: _ => { /* IsSpeaking already set in SpeakNative */ },
                onDone: _ => Interlocked.Exchange(ref _pendingSpeechFinished, 1),
                onError: id =>
                {
                    Debug.LogError($"[AgentTTS] Utterance error for '{id}'.");
                    Interlocked.Exchange(ref _pendingSpeechFinished, 1);
                });

            // void method — do not use Call<int>.
            _tts.Call("setOnUtteranceProgressListener", progressListener);
        }
        catch (Exception e)
        {
            // Speech can still work without progress callbacks.
            Debug.LogWarning($"[AgentTTS] Progress listener unavailable: {e.Message}");
        }
    }

    private void SpeakNative(string text)
    {
        try
        {
            using var paramsBundle = new AndroidJavaObject("android.os.Bundle");
            var result = _tts.Call<int>("speak", text, 0, paramsBundle, UttId);
            if (result != 0)
            {
                Debug.LogError($"[AgentTTS] speak() failed with result={result} for text length {text.Length}.");
                OnTTSError?.Invoke($"speak failed: {result}");
                return;
            }

            IsSpeaking = true;
            OnSpeechStarted?.Invoke(text);
            Debug.Log($"[AgentTTS] Speaking ({_activeLanguage}, {text.Length} chars): {text}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[AgentTTS] speak() threw: {e}");
            OnTTSError?.Invoke(e.Message);
        }
    }

    private void ShutdownTTS()
    {
        if (_tts == null)
            return;

        try
        {
            _tts.Call("stop");
            _tts.Call("shutdown");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[AgentTTS] ShutdownTTS: {e.Message}");
        }

        _tts.Dispose();
        _tts = null;
        _ttsReady = false;
    }

#endif

    private IEnumerator SpeakWhenReady(string text)
    {
        float waited = 0f;
        while (!_ttsReady && waited < 8f)
        {
            yield return new WaitForSeconds(0.1f);
            waited += 0.1f;
        }

        if (_ttsReady)
            Speak(text);
        else
            Debug.LogWarning("[AgentTTS] TTS did not become ready in time.");
    }
}

#if UNITY_ANDROID && !UNITY_EDITOR

/// <summary>Proxies android.speech.tts.TextToSpeech.OnInitListener</summary>
internal sealed class TTSInitListener : AndroidJavaProxy
{
    private readonly Action<int> _callback;

    public TTSInitListener(Action<int> callback)
        : base("android.speech.tts.TextToSpeech$OnInitListener")
    {
        _callback = callback;
    }

    // Called by Android on a binder thread — keep this allocation-free and JNI-free.
    public void onInit(int status) => _callback(status);
}

/// <summary>Proxies android.speech.tts.UtteranceProgressListener</summary>
internal sealed class TTSProgressListener : AndroidJavaProxy
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
        _onDone = onDone;
        _onError = onError;
    }

    public void onStart(string utteranceId) => _onStart(utteranceId);
    public void onDone(string utteranceId) => _onDone(utteranceId);
    public void onError(string utteranceId) => _onError(utteranceId);
    public void onError(string utteranceId, int errorCode) => _onError(utteranceId);
}

#endif

#pragma warning restore CS0414, CS0067
