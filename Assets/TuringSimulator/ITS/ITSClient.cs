// ITSClient.cs
// Slim REST client for the main demo line: /session/new, /ask, /health.

using System;
using System.Collections;
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
    public static ITSClient Instance { get; private set; }

    [Header("Server")]
    [SerializeField] private string _baseUrl = "http://localhost:8000";
    [SerializeField] private float _timeoutSeconds = 10f;

    public event Action<string> OnAskReply;
    public event Action<string> OnServerError;
    public event Action<string> OnSessionCreated;

    bool _serverAvailable;

    static string SerializeBody(object o) =>
        JsonConvert.SerializeObject(o, ItsRestJson.Settings);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(CheckServerHealth());
    }

    /// <summary>Send a free-form student question. Reply via <see cref="OnAskReply"/>.</summary>
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
            level_id = levelId,
            question = question,
        };
        StartCoroutine(Post("/ask", SerializeBody(req), OnAskResponse));
    }

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

    public Task<string> RequestNewSessionAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        RequestNewSession(id => tcs.TrySetResult(id));
        return tcs.Task;
    }

    void OnAskResponse(string json, bool success)
    {
        if (!success) return;
        var dto = JsonConvert.DeserializeObject<AskResponseDto>(json, ItsRestJson.Settings);
        if (!string.IsNullOrEmpty(dto?.Reply))
            OnAskReply?.Invoke(dto.Reply);
    }

    IEnumerator Post(string path, string json, Action<string, bool> callback)
    {
        var url = _baseUrl + path;
        var bytes = Encoding.UTF8.GetBytes(json);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
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

    IEnumerator CheckServerHealth()
    {
        using var req = UnityWebRequest.Get(_baseUrl + "/health");
        req.timeout = 3;
        yield return req.SendWebRequest();

        _serverAvailable = req.result == UnityWebRequest.Result.Success;

        if (_serverAvailable)
            Debug.Log("[ITSClient] Server reachable.");
        else
            Debug.LogWarning("[ITSClient] Server not reachable — Ask disabled until available.");
    }

    static string BuildLocalFallbackStudentId() =>
        $"local_{Guid.NewGuid():N}";
}
