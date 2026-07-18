// ITSClient.cs
// Handles all HTTP communication with the ITS FastAPI server.
// Attach to a persistent GameObject (e.g. your GameManager).
//
// Dependencies: Newtonsoft.Json (REST snake_case), UnityWebRequest.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ITS;
using ITS.Protocol;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-200)]
public class ITSClient : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static ITSClient Instance { get; private set; }

    // ── Inspector config ─────────────────────────────────────────────────────

    [Header("Server")]
    [Tooltip("Base URL of the FastAPI server. Change to your deployed URL in production.")]
    [SerializeField] private string _baseUrl = "http://localhost:8000";

    [Tooltip("Seconds to wait before a request times out.")]
    [SerializeField] private float _timeoutSeconds = 10f;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fires when the server returns a comment after a game event.</summary>
    public event Action<string> OnAgentComment;

    /// <summary>BKT skill probabilities returned with /event (may be empty).</summary>
    public event Action<IReadOnlyDictionary<string, float>> OnSkillsUpdated;

    /// <summary>Fires when the server returns an answer to a student question.</summary>
    public event Action<string> OnAskReply;

    /// <summary>Fires when the server returns a hint.</summary>
    public event Action<HintResponse> OnHintReply;

    /// <summary>Fires when any server request fails.</summary>
    public event Action<string> OnServerError;

    /// <summary>Fires when a new student session is allocated.</summary>
    public event Action<string> OnSessionCreated;

    // ── Internals ─────────────────────────────────────────────────────────────

    private bool _serverAvailable = false;

    static string SerializeBody(object o) =>
        JsonConvert.SerializeObject(o, ItsRestJson.Settings);

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(CheckServerHealth());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Send a game event to the server. BKT is updated server-side.
    /// The optional agent comment is delivered via OnAgentComment.
    /// </summary>
    public void SendEvent(
        string   studentId,
        string   levelId,
        string   eventType,
        bool     correct,
        string[] skillIds)
    {
        if (!_serverAvailable) return;

        var req = new EventRequest
        {
            student_id = studentId,
            level_id   = levelId,
            event_type = eventType,
            correct    = correct,
            skill_ids  = skillIds ?? Array.Empty<string>(),
        };
        StartCoroutine(Post("/event", SerializeBody(req), OnEventResponse));
    }

    /// <summary>
    /// Send a free-form student question. Reply delivered via OnAskReply.
    /// </summary>
    public void Ask(string studentId, string levelId, string question)
    {
        if (!_serverAvailable)
        {
            OnServerError?.Invoke("Server not available.");
            return;
        }

        var req = new AskRequest
        {
            student_id = studentId,
            level_id   = levelId,
            question   = question,
        };
        StartCoroutine(Post("/ask", SerializeBody(req), OnAskResponse));
    }

    /// <summary>
    /// Request a graduated hint. Pass skillId = null to let the server
    /// choose the weakest skill automatically.
    /// Reply delivered via OnHintReply.
    /// </summary>
    public void RequestHint(string studentId, string levelId, string skillId = null)
    {
        if (!_serverAvailable)
        {
            OnServerError?.Invoke("Server not available.");
            return;
        }

        var req = new HintRequest
        {
            student_id = studentId,
            level_id   = levelId,
            skill_id   = skillId,
        };
        StartCoroutine(Post("/hint", SerializeBody(req), OnHintResponse));
    }

    /// <summary>
    /// Request a new student session identifier from the server.
    /// Falls back to a local ephemeral identifier if the server is unavailable.
    /// </summary>
    public void RequestNewSession(Action<string> onComplete)
    {
        if (onComplete == null) return;
        StartCoroutine(Post("/session/new", "{}", (json, success) =>
        {
            if (!success)
            {
                var fallback = BuildLocalFallbackStudentId();
                OnServerError?.Invoke("Session endpoint unavailable. Using local temporary session id.");
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

    /// <summary>Task wrapper around <see cref="RequestNewSession"/>.</summary>
    public Task<string> RequestNewSessionAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        RequestNewSession(id => tcs.TrySetResult(id));
        return tcs.Task;
    }

    // ── Response handlers ─────────────────────────────────────────────────────

    private void OnEventResponse(string json, bool success)
    {
        if (!success) return;
        var dto = JsonConvert.DeserializeObject<EventResponseDto>(json, ItsRestJson.Settings);
        if (dto == null) return;

        if (dto.UpdatedSkills != null && dto.UpdatedSkills.Count > 0)
            OnSkillsUpdated?.Invoke(dto.UpdatedSkills);

        if (!string.IsNullOrEmpty(dto.Comment))
            OnAgentComment?.Invoke(dto.Comment);
    }

    private void OnAskResponse(string json, bool success)
    {
        if (!success) return;
        var dto = JsonConvert.DeserializeObject<AskResponseDto>(json, ItsRestJson.Settings);
        if (!string.IsNullOrEmpty(dto?.Reply))
            OnAskReply?.Invoke(dto.Reply);
    }

    private void OnHintResponse(string json, bool success)
    {
        if (!success) return;
        var dto = JsonConvert.DeserializeObject<HintResponseDto>(json, ItsRestJson.Settings);
        if (dto == null) return;

        OnHintReply?.Invoke(new HintResponse
        {
            reply = dto.Reply,
            skill_id = dto.SkillId,
            hint_level = dto.HintLevel
        });
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private IEnumerator Post(
        string path,
        string json,
        Action<string, bool> callback)
    {
        var url   = _baseUrl + path;
        var bytes = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
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
            OnServerError?.Invoke(req.error);
            callback(null, false);
        }
    }

    private IEnumerator CheckServerHealth()
    {
        using var req = UnityWebRequest.Get(_baseUrl + "/health");
        req.timeout = 3;
        yield return req.SendWebRequest();

        _serverAvailable = req.result == UnityWebRequest.Result.Success;

        if (_serverAvailable)
            Debug.Log("[ITSClient] Server reachable.");
        else
            Debug.LogWarning("[ITSClient] Server not reachable — ITS features disabled.");
    }

    private static string BuildLocalFallbackStudentId() =>
        $"local_{Guid.NewGuid():N}";
}
