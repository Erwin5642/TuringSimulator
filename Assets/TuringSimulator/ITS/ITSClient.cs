// ITSClient.cs
// Handles all HTTP communication with the ITS FastAPI server.
// Attach to a persistent GameObject (e.g. your GameManager).
//
// Dependencies: none beyond Unity built-ins.
// Usage: ITSClient.Instance.SendEvent(...) / .Ask(...) / .RequestHint(...)

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ITS;

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
    // Subscribe to these from AgentDialogue or any other consumer.

    /// <summary>Fires when the server returns a comment after a game event.</summary>
    public event Action<string> OnAgentComment;

    /// <summary>Fires when the server returns an answer to a student question.</summary>
    public event Action<string> OnAskReply;

    /// <summary>Fires when the server returns a hint.</summary>
    public event Action<HintResponse> OnHintReply;

    /// <summary>Fires when any server request fails.</summary>
    public event Action<string> OnServerError;

    // ── Internals ─────────────────────────────────────────────────────────────

    private bool _serverAvailable = false;

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
        StartCoroutine(Post("/event", JsonUtility.ToJson(req), OnEventResponse));
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
        StartCoroutine(Post("/ask", JsonUtility.ToJson(req), OnAskResponse));
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
        StartCoroutine(Post("/hint", JsonUtility.ToJson(req), OnHintResponse));
    }

    // ── Response handlers ─────────────────────────────────────────────────────

    private void OnEventResponse(string json, bool success)
    {
        if (!success) return;
        var resp = JsonUtility.FromJson<EventResponse>(json);
        if (!string.IsNullOrEmpty(resp.comment))
            OnAgentComment?.Invoke(resp.comment);
    }

    private void OnAskResponse(string json, bool success)
    {
        if (!success) return;
        var resp = JsonUtility.FromJson<AskResponse>(json);
        if (!string.IsNullOrEmpty(resp.reply))
            OnAskReply?.Invoke(resp.reply);
    }

    private void OnHintResponse(string json, bool success)
    {
        if (!success) return;
        var resp = JsonUtility.FromJson<HintResponse>(json);
        OnHintReply?.Invoke(resp);
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
}