using System;
using ITS;
using UnityEngine;

/// <summary>
/// Slim session identity holder for the main voice Ask demo.
/// Keeps student and level context for /ask payloads.
/// </summary>
public class SkillTracker : MonoBehaviour
{
    public static SkillTracker Instance { get; private set; }

    [Header("Session")]
    [Tooltip("Current active student session id (assigned at runtime).")]
    [SerializeField] private string _studentId = "";

    private string _currentLevelId = "";

    public string StudentId => _studentId;
    public bool HasActiveSession => !string.IsNullOrWhiteSpace(_studentId);

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void OnLevelLoaded(string levelId)
    {
        _currentLevelId = levelId ?? string.Empty;
        Debug.Log($"[SkillTracker] Level loaded: {_currentLevelId}");
    }

    public void BeginSession(string studentId)
    {
        _studentId = string.IsNullOrWhiteSpace(studentId) ? $"local_{Guid.NewGuid():N}" : studentId;
        _currentLevelId = string.Empty;
        Debug.Log($"[SkillTracker] Session started: {_studentId}");
    }

    public void ClearSession()
    {
        _studentId = string.Empty;
        _currentLevelId = string.Empty;
        Debug.Log("[SkillTracker] Session cleared.");
    }

    public string GetCurrentLevelId() =>
        string.IsNullOrWhiteSpace(_currentLevelId) ? LevelID.MoveLeftRight : _currentLevelId;
}
