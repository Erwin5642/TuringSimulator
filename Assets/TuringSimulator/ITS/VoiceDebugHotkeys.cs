// Editor/dev hotkeys for voice.
// L — speak a predefined Portuguese TTS line.
// T — toggle STT and show recognized text on an overlay (needs AppVoiceExperience).

using UnityEngine;
using UnityEngine.InputSystem;

public sealed class VoiceDebugHotkeys : MonoBehaviour
{
    [Header("Enable")]
    [SerializeField] private bool _enableInEditor = true;
    [SerializeField] private bool _enableInDevelopmentBuilds;

    [Header("Keys")]
    [SerializeField] private Key _listenKey = Key.L;
    [SerializeField] private Key _talkKey = Key.T;

    [Header("TTS sample")]
    [SerializeField] [TextArea] private string _sampleSpeech =
        "Olá, treineiro. Este é um teste da voz do tutor.";

    [Header("References")]
    [SerializeField] private AgentTTS _agentTts;
    [SerializeField] private AgentDialogue _agentDialogue;
    [SerializeField] private VoiceInputHandler _voiceInput;

    private string _overlayStatus = string.Empty;
    private string _sttPartial = string.Empty;
    private string _sttFinal = string.Empty;
    private bool _listening;
    private float _overlayUntil;
    private bool _subscribedToVoice;

    private bool IsHotkeyEnabled =>
        (_enableInEditor && Application.isEditor) ||
        (_enableInDevelopmentBuilds && Debug.isDebugBuild);

    private void Awake()
    {
        _agentTts ??= AgentTTS.Instance;
        _agentDialogue ??= AgentDialogue.Instance;
        _voiceInput ??= VoiceInputHandler.Instance;
    }

    private void OnEnable()
    {
        TrySubscribeVoice();
        if (_agentTts != null)
            _agentTts.OnSpeechError += HandleSpeechError;
    }

    private void OnDisable()
    {
        UnsubscribeVoice();
        if (_agentTts != null)
            _agentTts.OnSpeechError -= HandleSpeechError;
    }

    private void Update()
    {
        if (!IsHotkeyEnabled)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard[_listenKey].wasPressedThisFrame)
            SpeakSample();
        else if (keyboard[_talkKey].wasPressedThisFrame)
            ToggleStt();
    }

    private void OnGUI()
    {
        if (!IsHotkeyEnabled)
            return;

        var showDetail = _listening
            || Time.unscaledTime <= _overlayUntil
            || !string.IsNullOrEmpty(_sttPartial)
            || !string.IsNullOrEmpty(_sttFinal);
        const int width = 540;
        var height = showDetail ? 120 : 40;
        GUI.Box(new Rect(12, 12, width, height), GUIContent.none);
        GUI.Label(new Rect(20, 20, width - 16, height - 16), BuildOverlayText(showDetail));
    }

    [ContextMenu("Speak Debug Sample")]
    private void SpeakSample()
    {
        if (!IsHotkeyEnabled)
            return;

        StopSttIfListening();

        var text = string.IsNullOrWhiteSpace(_sampleSpeech)
            ? "Olá, treineiro. Este é um teste da voz do tutor."
            : _sampleSpeech.Trim();

        var tts = _agentTts != null ? _agentTts : AgentTTS.Instance;
        var dialogue = _agentDialogue != null ? _agentDialogue : AgentDialogue.Instance;

        if (tts == null)
        {
            ShowOverlay("TTS: AgentTTS não encontrado na cena.");
            Debug.LogError("[VoiceDebugHotkeys] AgentTTS is missing.");
            return;
        }

        dialogue?.ShowSubtitle(text);
        tts.Speak(text);
        ShowOverlay($"TTS (L): {text}");
        Debug.Log($"[VoiceDebugHotkeys] Sample TTS requested: {text}");
    }

    [ContextMenu("Toggle STT Debug")]
    private void ToggleStt()
    {
        if (!IsHotkeyEnabled)
            return;

        TrySubscribeVoice();
        var voice = _voiceInput != null ? _voiceInput : VoiceInputHandler.Instance;
        if (voice == null)
        {
            ShowOverlay(
                "STT (T): VoiceInputHandler não está na cena. " +
                "Adicione AppVoiceExperience + VoiceInputHandler para testar o microfone.");
            Debug.LogWarning("[VoiceDebugHotkeys] STT debug needs VoiceInputHandler.");
            return;
        }

        if (voice.IsListening)
        {
            voice.StopListening();
            _listening = false;
            ShowOverlay("STT (T): escuta encerrada.");
            return;
        }

        var tts = _agentTts != null ? _agentTts : AgentTTS.Instance;
        tts?.Stop();

        if (!voice.CanListen)
        {
            ShowOverlay(
                "STT (T): AppVoiceExperience não está configurado. " +
                "Atribua Wit Configuration e ligue o microfone no Editor.");
            Debug.LogWarning("[VoiceDebugHotkeys] StartListening did not begin. Check AppVoiceExperience.");
            return;
        }

        _sttPartial = string.Empty;
        _sttFinal = string.Empty;
        voice.StartListening();
        _listening = true;
        ShowOverlay("STT (T): ouvindo… fale em português.");
    }

    private void TrySubscribeVoice()
    {
        if (_subscribedToVoice)
            return;

        _voiceInput ??= VoiceInputHandler.Instance;
        if (_voiceInput == null)
            return;

        _voiceInput.OnListeningStarted += HandleListeningStarted;
        _voiceInput.OnListeningStopped += HandleListeningStopped;
        _voiceInput.OnPartialTranscription += HandlePartial;
        _voiceInput.OnTranscriptionReady += HandleFinal;
        _voiceInput.OnCaptureStopped += HandleCaptureStopped;
        _subscribedToVoice = true;
    }

    private void UnsubscribeVoice()
    {
        if (!_subscribedToVoice || _voiceInput == null)
        {
            _subscribedToVoice = false;
            return;
        }

        _voiceInput.OnListeningStarted -= HandleListeningStarted;
        _voiceInput.OnListeningStopped -= HandleListeningStopped;
        _voiceInput.OnPartialTranscription -= HandlePartial;
        _voiceInput.OnTranscriptionReady -= HandleFinal;
        _voiceInput.OnCaptureStopped -= HandleCaptureStopped;
        _subscribedToVoice = false;
    }

    private void StopSttIfListening()
    {
        var voice = _voiceInput != null ? _voiceInput : VoiceInputHandler.Instance;
        if (voice != null && voice.IsListening)
            voice.StopListening();
        _listening = false;
    }

    private void HandleListeningStarted()
    {
        _listening = true;
        ShowOverlay("STT (T): ouvindo…");
    }

    private void HandleListeningStopped()
    {
        _listening = false;
        if (string.IsNullOrEmpty(_sttFinal))
            ShowOverlay("STT (T): escuta encerrada.");
    }

    private void HandleCaptureStopped()
    {
        ShowOverlay("STT (T): parou de ouvir. Pressione T para enviar.");
    }

    private void HandlePartial(string partial)
    {
        _sttPartial = partial ?? string.Empty;
        KeepOverlay();
    }

    private void HandleFinal(string text)
    {
        _sttFinal = text ?? string.Empty;
        _sttPartial = string.Empty;
        _listening = false;
        ShowOverlay($"STT (T): {_sttFinal}");
    }

    private void HandleSpeechError(string error)
    {
        ShowOverlay($"TTS erro: {error}");
    }

    private void ShowOverlay(string status)
    {
        _overlayStatus = status ?? string.Empty;
        KeepOverlay();
    }

    private void KeepOverlay()
    {
        _overlayUntil = Time.unscaledTime + 8f;
    }

    private string BuildOverlayText(bool showDetail)
    {
        const string hint = "L: ouvir TTS de teste    T: falar com STT";
        if (!showDetail)
            return hint;

        if (_listening || !string.IsNullOrEmpty(_sttPartial) || !string.IsNullOrEmpty(_sttFinal))
        {
            var partial = string.IsNullOrEmpty(_sttPartial) ? "—" : _sttPartial;
            var final = string.IsNullOrEmpty(_sttFinal) ? "—" : _sttFinal;
            var status = string.IsNullOrEmpty(_overlayStatus) ? hint : _overlayStatus;
            return $"{status}\nParcial: {partial}\nFinal: {final}";
        }

        return string.IsNullOrEmpty(_overlayStatus) ? hint : $"{hint}\n{_overlayStatus}";
    }
}
