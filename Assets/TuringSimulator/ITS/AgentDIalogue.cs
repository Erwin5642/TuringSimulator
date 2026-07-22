// AgentDialogue.cs
// Voice Ask hub for the main demo line: controller mic -> Wit STT -> /ask -> bubble/TTS.

using System;
using System.Collections;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AgentDialogue : MonoBehaviour
{
    public static AgentDialogue Instance { get; private set; }

    [Header("Subtitle bubble")]
    [SerializeField] private GameObject _bubbleRoot;
    [SerializeField] private TMP_Text _bubbleText;
    [SerializeField] private float _typewriterSpeed = 40f;
    [SerializeField] private float _autoDismissAfter = 7f;

    [Header("Listening feedback (optional)")]
    [SerializeField] private GameObject _micActiveIndicator;
    [SerializeField] private TMP_Text _partialLabel;
    [SerializeField] private GameObject _loadingIndicator;

    [Header("Mode")]
    [Tooltip("Keep false to use event-channel wiring. True keeps direct legacy subscriptions.")]
    [SerializeField] private bool _useLegacyDirectWiring = true;

    public event Action OnThinkingStarted;
    public event Action OnThinkingFinished;

    private Coroutine _typewriterRoutine;
    private Coroutine _dismissRoutine;
    private bool _micToggle;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureBubbleExists();
    }

    private void Start()
    {
        _bubbleRoot?.SetActive(false);
        SetThinkingState(false);
        SetListeningState(false);
        SetPartialTranscription(string.Empty);

        if (AgentTTS.Instance != null)
            AgentTTS.Instance.OnSpeechFinished += OnTTSFinished;

        if (!_useLegacyDirectWiring)
            return;

        if (ITSClient.Instance != null)
        {
            ITSClient.Instance.OnAskReply += SayAndSpeak;
            ITSClient.Instance.OnServerError += OnServerError;
        }

        if (VoiceInputHandler.Instance != null)
        {
            VoiceInputHandler.Instance.OnTranscriptionReady += OnTranscriptionReady;
            VoiceInputHandler.Instance.OnPartialTranscription += OnPartialTranscription;
            VoiceInputHandler.Instance.OnListeningStarted += OnListeningStarted;
            VoiceInputHandler.Instance.OnListeningStopped += OnListeningStopped;
        }
    }

    private void OnDestroy()
    {
        if (_useLegacyDirectWiring && ITSClient.Instance != null)
        {
            ITSClient.Instance.OnAskReply -= SayAndSpeak;
            ITSClient.Instance.OnServerError -= OnServerError;
        }

        if (_useLegacyDirectWiring && VoiceInputHandler.Instance != null)
        {
            VoiceInputHandler.Instance.OnTranscriptionReady -= OnTranscriptionReady;
            VoiceInputHandler.Instance.OnPartialTranscription -= OnPartialTranscription;
            VoiceInputHandler.Instance.OnListeningStarted -= OnListeningStarted;
            VoiceInputHandler.Instance.OnListeningStopped -= OnListeningStopped;
        }

        if (AgentTTS.Instance != null)
            AgentTTS.Instance.OnSpeechFinished -= OnTTSFinished;

        if (Instance == this)
            Instance = null;
    }

    public void ToggleMicListening()
    {
        if (VoiceInputHandler.Instance == null)
        {
            SayAndSpeak("O microfone ainda não está configurado.");
            return;
        }

        AgentTTS.Instance?.Stop();

        if (!_micToggle)
            VoiceInputHandler.Instance.StartListening();
        else
            VoiceInputHandler.Instance.StopListening();
    }

    public void SayAndSpeak(string message)
    {
        SetThinkingState(false);
        ShowSubtitle(message);
        AgentTTS.Instance?.Speak(message);
    }

    public void ShowSubtitle(string message)
    {
        if (_bubbleRoot == null || _bubbleText == null)
            return;

        if (_typewriterRoutine != null)
            StopCoroutine(_typewriterRoutine);
        if (_dismissRoutine != null)
            StopCoroutine(_dismissRoutine);

        _bubbleRoot.SetActive(true);
        _typewriterRoutine = StartCoroutine(Typewriter(message));
    }

    private void OnListeningStarted()
    {
        SetListeningState(true);
        SetPartialTranscription(string.Empty);
    }

    private void OnListeningStopped()
    {
        SetListeningState(false);
        SetThinkingState(true);
        SetPartialTranscription(string.Empty);
    }

    private void OnPartialTranscription(string partial)
    {
        SetPartialTranscription(partial);
    }

    private void OnTranscriptionReady(string text)
    {
        if (_partialLabel != null)
            _partialLabel.text = string.Empty;

        var studentId = SkillTracker.Instance != null && SkillTracker.Instance.HasActiveSession
            ? SkillTracker.Instance.StudentId
            : string.Empty;
        var levelId = SkillTracker.Instance?.GetCurrentLevelId() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(studentId))
        {
            SetThinkingState(false);
            SayAndSpeak("Inicie uma nova sessão no menu antes de conversar comigo.");
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            SetThinkingState(false);
            SayAndSpeak("Nao entendi. Tente perguntar de novo.");
            return;
        }

        ITSClient.Instance?.Ask(studentId, levelId, text);
    }

    private void OnServerError(string error)
    {
        SetThinkingState(false);
        SayAndSpeak("Hmm, parece que perdi o sinal. Tente novamente em um momento, treineiro.");
        Debug.LogWarning($"[AgentDialogue] Server error: {error}");
    }

    public void SetThinkingState(bool thinking)
    {
        _loadingIndicator?.SetActive(thinking);
        if (thinking)
            OnThinkingStarted?.Invoke();
        else
            OnThinkingFinished?.Invoke();
    }

    public void SetListeningState(bool isListening)
    {
        _micToggle = isListening;
        _micActiveIndicator?.SetActive(isListening);
    }

    public void SetPartialTranscription(string partial)
    {
        if (_partialLabel != null)
            _partialLabel.text = partial ?? string.Empty;
    }

    private void OnTTSFinished()
    {
        if (_dismissRoutine != null)
            StopCoroutine(_dismissRoutine);
        _dismissRoutine = StartCoroutine(DismissAfterDelay());
    }

    private IEnumerator Typewriter(string message)
    {
        _bubbleText.text = string.Empty;
        foreach (var c in message)
        {
            _bubbleText.text += c;
            yield return new WaitForSeconds(1f / Mathf.Max(1f, _typewriterSpeed));
        }

        if (AgentTTS.Instance == null || !AgentTTS.Instance.IsSpeaking)
            _dismissRoutine = StartCoroutine(DismissAfterDelay());
    }

    private IEnumerator DismissAfterDelay()
    {
        yield return new WaitForSeconds(_autoDismissAfter);
        _bubbleRoot?.SetActive(false);
    }

    private void EnsureBubbleExists()
    {
        if (_bubbleRoot != null && _bubbleText != null)
            return;

        var canvasGo = new GameObject("AgentSubtitleCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();

        var rt = canvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(800f, 200f);
        rt.localPosition = new Vector3(0f, 1.6f, 2f);
        rt.localScale = Vector3.one * 0.0025f;

        var textGo = new GameObject("BubbleText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        _bubbleRoot = canvasGo;
        _bubbleText = tmp;
    }
}
