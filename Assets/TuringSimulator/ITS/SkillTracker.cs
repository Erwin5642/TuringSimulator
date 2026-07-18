// SkillTracker.cs
// Maps game events (program run, success, fail, level complete) to the
// BKT skill IDs defined in domain/concepts.py and sends them to the
// ITS server via ITSClient.
//
// HOW TO INTEGRATE:
//   1. Add this component to your GameManager GameObject.
//   2. In the Inspector, assign your existing event sources
//      (ProgramRunner, LevelManager, etc.) OR wire them up in code
//      using the Subscribe() methods below.
//   3. Call the public On* methods from your existing event delegates.
//
// The skill mappings in each method reflect which BKT skills are
// exercised by each game event. Adjust them if your game tracks
// more granular information (e.g. which specific block type failed).

using System;
using System.Collections.Generic;
using UnityEngine;
using ITS;

public class SkillTracker : MonoBehaviour
{
    // ── Singleton (shares the GameManager pattern) ────────────────────────────

    public static SkillTracker Instance { get; private set; }

    // ── Session state (set by your login / session system) ───────────────────

    [Header("Session")]
    [Tooltip("Current active student session id (assigned at runtime).")]
    [SerializeField] private string _studentId = "";

    // ── Internals ─────────────────────────────────────────────────────────────

    private string _currentLevelId = "";

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires when the level is completed — subscribe in AgentAnimator
    /// to trigger the Celebrate animation.
    /// </summary>
    public event Action OnLevelCompleteAnimEvent;

    public string StudentId => _studentId;
    public bool HasActiveSession => !string.IsNullOrWhiteSpace(_studentId);

    // ── Level tracking ────────────────────────────────────────────────────────

    /// <summary>
    /// Call this whenever your LevelManager loads a new level.
    /// Pass the level ID string exactly as defined in LevelID constants.
    /// Example: SkillTracker.Instance.OnLevelLoaded(LevelID.AppendScrew);
    /// </summary>
    public void OnLevelLoaded(string levelId)
    {
        _currentLevelId = levelId;
        Debug.Log($"[SkillTracker] Level loaded: {levelId}");
    }

    public void BeginSession(string studentId)
    {
        _studentId = string.IsNullOrWhiteSpace(studentId) ? $"local_{Guid.NewGuid():N}" : studentId;
        _currentLevelId = "";
        LiveTutorSocket.Instance?.SetActiveStudentSession(_studentId);
        Debug.Log($"[SkillTracker] Session started: {_studentId}");
    }

    public void ClearSession()
    {
        LiveTutorSocket.Instance?.ClearActiveStudentSession();
        _studentId = "";
        _currentLevelId = "";
        Debug.Log("[SkillTracker] Session cleared.");
    }

    // ── Game event handlers ───────────────────────────────────────────────────

    /// <summary>
    /// Table-driven TM flow: sends coarse ProgramFail when simulation halts without passing validation tests.
    /// </summary>
    public void OnSimulationValidationFailedCoarse()
    {
        if (!Ready()) return;

        var skills = new List<string>
        {
            SkillID.PlaceWire,
            SkillID.ConnectPort,
            SkillID.MoveLeftRight,
            SkillID.ConditionBlock
        };

        ITSClient.Instance.SendEvent(
            _studentId,
            _currentLevelId,
            ITS.EventType.ProgramFail,
            correct: false,
            skills.ToArray());
    }

    /// <summary>
    /// Call from ProgramRunner when the player presses Run.
    /// programCorrect = true if the program is structurally valid
    /// (all ports wired, no dangling connections) before execution.
    /// </summary>
    public void OnProgramRun(bool programCorrect)
    {
        if (!Ready()) return;

        // Running the program exercises wiring and port connection skills
        var skills = new List<string> { SkillID.PlaceWire, SkillID.ConnectPort };

        ITSClient.Instance.SendEvent(
            _studentId, _currentLevelId,
            ITS.EventType.ProgramRun,
            programCorrect,
            skills.ToArray()
        );
    }

    /// <summary>
    /// Call from ProgramRunner when the tape simulation completes
    /// and the program reaches an accept block successfully.
    /// Pass the result details so skill evidence can be inferred.
    /// </summary>
    public void OnProgramSuccess(ProgramResult result)
    {
        if (!Ready()) return;

        var skills = BuildSkillsFromResult(result, correct: true);

        ITSClient.Instance.SendEvent(
            _studentId, _currentLevelId,
            ITS.EventType.ProgramSuccess,
            correct: true,
            skills.ToArray()
        );
    }

    /// <summary>
    /// Call from ProgramRunner when the tape simulation ends in reject,
    /// infinite loop detection, or a structural error.
    /// </summary>
    public void OnProgramFail(ProgramResult result)
    {
        if (!Ready()) return;

        var skills = BuildSkillsFromResult(result, correct: false);

        ITSClient.Instance.SendEvent(
            _studentId, _currentLevelId,
            ITS.EventType.ProgramFail,
            correct: false,
            skills.ToArray()
        );
    }

    /// <summary>
    /// Call from your LevelManager when the level is marked complete
    /// (after a successful run on the correct tape inputs).
    /// </summary>
    public void OnLevelComplete()
    {
        if (!Ready()) return;

        // Level completion gives positive evidence for all skills in the level
        var skills = SkillsForLevel(_currentLevelId);

        ITSClient.Instance.SendEvent(
            _studentId, _currentLevelId,
            ITS.EventType.LevelComplete,
            correct: true,
            skills
        );

        OnLevelCompleteAnimEvent?.Invoke();
    }

    // ── Skill inference ───────────────────────────────────────────────────────

    /// <summary>
    /// Infers which skills were exercised from a ProgramResult.
    /// Extend this as your ProgramRunner exposes more detail.
    /// </summary>
    private List<string> BuildSkillsFromResult(ProgramResult result, bool correct)
    {
        var skills = new List<string>();

        // Every program run exercises basic wiring
        skills.Add(SkillID.PlaceWire);
        skills.Add(SkillID.ConnectPort);

        // Move blocks used → head motion skills
        if (result.UsedMoveBlock)
        {
            skills.Add(SkillID.MoveLeftRight);
            skills.Add(SkillID.ChainMoveWithAction);
        }

        // Write block used → symbol operation skills
        if (result.UsedWriteBlock)
            skills.Add(SkillID.UseWriteBlock);

        // Condition block used → control flow skills
        if (result.UsedConditionBlock)
        {
            skills.Add(SkillID.ConditionBlock);
            skills.Add(SkillID.BranchLogic);
            skills.Add(SkillID.TapePosition);
        }

        // Both ports of condition wired → branch logic
        if (result.BothConditionPortsWired)
            skills.Add(SkillID.BranchLogic);

        // Loop detected in circuit → loop construction
        if (result.HasLoop)
            skills.Add(SkillID.LoopConstruction);

        // Blank cell used as terminator
        if (result.UsedBlankAsTerminator)
            skills.Add(SkillID.BlankAsTapeEnd);

        // Reached accept block
        if (result.ReachedAccept)
        {
            skills.Add(SkillID.AcceptBlock);
            skills.Add(SkillID.Halting);
        }

        // Reached reject block
        if (result.ReachedReject)
        {
            skills.Add(SkillID.AcceptVsReject);
            skills.Add(SkillID.Halting);
        }

        // All five block types used → chain all blocks
        if (result.UsedAllBlockTypes)
            skills.Add(SkillID.ChainAllBlocks);

        // Write used as marker (marker symbol detected on tape)
        if (result.UsedMarkerSymbol)
            skills.Add(SkillID.WriteAsMemory);

        // Multiple distinct loop bodies detected → multi-state program
        if (result.DistinctLoopCount > 1)
            skills.Add(SkillID.MultiStateProgram);

        return skills;
    }

    /// <summary>
    /// Returns the core skill IDs expected to be demonstrated by the
    /// end of a level. Used for level-complete evidence.
    /// </summary>
    private static string[] SkillsForLevel(string levelId) => levelId switch
    {
        LevelID.MoveLeftRight      => new[] { SkillID.MoveLeftRight, SkillID.PlaceWire, SkillID.ConnectPort, SkillID.TapePosition },
        LevelID.PlaceGear          => new[] { SkillID.UseWriteBlock, SkillID.ChainMoveWithAction },
        LevelID.AppendScrew        => new[] { SkillID.ConditionBlock, SkillID.BranchLogic, SkillID.LoopConstruction, SkillID.BlankAsTapeEnd, SkillID.AcceptBlock },
        LevelID.ReplaceAllWithNuts => new[] { SkillID.UseWriteBlock, SkillID.LoopConstruction, SkillID.BranchLogic },
        LevelID.RejectIfGearExists => new[] { SkillID.AcceptVsReject, SkillID.Halting, SkillID.LoopConstruction },
        LevelID.SwapNutsAndScrews  => new[] { SkillID.ChainAllBlocks, SkillID.BranchLogic, SkillID.UseWriteBlock },
        LevelID.PatternRepeated    => new[] { SkillID.MultiStateProgram, SkillID.LoopConstruction, SkillID.LanguageRecognition },
        LevelID.BalancedPairs      => new[] { SkillID.WriteAsMemory, SkillID.MultiStateProgram, SkillID.LanguageRecognition },
        LevelID.PatternSomewhere   => new[] { SkillID.WriteAsMemory, SkillID.MultiStateProgram, SkillID.LanguageRecognition },
        _                          => new[] { SkillID.PlaceWire },
    };

    // ── Accessors ─────────────────────────────────────────────────────────────

    public string GetCurrentLevelId() => _currentLevelId;

    // ── Guard ─────────────────────────────────────────────────────────────────

    private bool Ready()
    {
        if (ITSClient.Instance == null)
        {
            Debug.LogWarning("[SkillTracker] ITSClient not found.");
            return false;
        }
        if (!HasActiveSession)
        {
            Debug.LogWarning("[SkillTracker] No active student session.");
            return false;
        }
        if (string.IsNullOrEmpty(_currentLevelId))
        {
            Debug.LogWarning("[SkillTracker] No level loaded — call OnLevelLoaded first.");
            return false;
        }
        return true;
    }
}

// ── ProgramResult ─────────────────────────────────────────────────────────────
// Populate this from your ProgramRunner after each execution.
// Expose whichever fields your runner already tracks; leave the rest false.

[System.Serializable]
public class ProgramResult
{
    public bool UsedMoveBlock;
    public bool UsedWriteBlock;
    public bool UsedConditionBlock;
    public bool BothConditionPortsWired;
    public bool HasLoop;
    public bool UsedBlankAsTerminator;
    public bool ReachedAccept;
    public bool ReachedReject;
    public bool UsedAllBlockTypes;
    public bool UsedMarkerSymbol;
    public int  DistinctLoopCount;
}