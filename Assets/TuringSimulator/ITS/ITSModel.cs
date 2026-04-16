// ITSModels.cs
// Data classes for serialising requests to and deserialising responses
// from the ITS FastAPI server.
// All classes use [System.Serializable] so JsonUtility can handle them.

using System;
using System.Collections.Generic;

namespace ITS
{
    // ── Requests ────────────────────────────────────────────────────────────

    [Serializable]
    public class EventRequest
    {
        public string   student_id;
        public string   level_id;
        public string   event_type;
        public bool     correct;
        public string[] skill_ids;
    }

    [Serializable]
    public class AskRequest
    {
        public string student_id;
        public string level_id;
        public string question;
    }

    [Serializable]
    public class HintRequest
    {
        public string student_id;
        public string level_id;
        public string skill_id;   // null → server picks weakest skill
    }

    // ── Responses ───────────────────────────────────────────────────────────

    [Serializable]
    public class EventResponse
    {
        // JsonUtility cannot deserialise arbitrary dicts, so updated_skills
        // is parsed manually in ITSClient from the raw JSON string.
        public string comment;   // may be null — agent reactive comment
    }

    [Serializable]
    public class AskResponse
    {
        public string reply;
    }

    [Serializable]
    public class HintResponse
    {
        public string reply;
        public string skill_id;
        public int    hint_level;   // 1=Socratic 2=Conceptual 3=Partial 4=Direct
    }

    // ── Skill ID constants ──────────────────────────────────────────────────
    // Mirror of the skill IDs defined in domain/concepts.py.
    // Use these constants everywhere in Unity instead of raw strings.

    public static class SkillID
    {
        // Interface
        public const string PlaceWire          = "S1.1";
        public const string ConnectPort        = "S1.2";
        public const string TapePosition       = "S1.3";
        public const string BlankAsTapeEnd     = "S1.4";

        // Symbol operations
        public const string IdentifySymbol     = "S2.1";
        public const string UseWriteBlock      = "S2.2";
        public const string WriteAsMemory      = "S2.3";

        // Head motion
        public const string MoveLeftRight      = "S3.1";
        public const string ChainMoveWithAction= "S3.2";

        // Control flow
        public const string ConditionBlock     = "S4.1";
        public const string BranchLogic        = "S4.2";
        public const string ChainAllBlocks     = "S4.3";
        public const string MultiStateProgram  = "S4.4";
        public const string LoopConstruction   = "S4.5";

        // TM theory
        public const string Halting            = "S5.1";
        public const string AcceptBlock        = "S5.2a";
        public const string AcceptVsReject     = "S5.2b";
        public const string LanguageRecognition= "S5.3";
    }

    // ── Event type constants ────────────────────────────────────────────────

    public static class EventType
    {
        public const string ProgramRun     = "program_run";
        public const string ProgramSuccess = "program_success";
        public const string ProgramFail    = "program_fail";
        public const string LevelComplete  = "level_complete";
    }

    // ── Level ID constants ──────────────────────────────────────────────────

    public static class LevelID
    {
        public const string MoveLeftRight      = "MoveLeftRight";
        public const string PlaceGear          = "PlaceGear";
        public const string AppendScrew        = "AppendScrew";
        public const string ReplaceAllWithNuts = "ReplaceAllWithNuts";
        public const string RejectIfGearExists = "RejectIfGearExists";
        public const string SwapNutsAndScrews  = "SwapNutsAndScrews";
        public const string PatternRepeated    = "PatternRepeated";
        public const string BalancedPairs      = "BalancedPairs";
        public const string PatternSomewhere   = "PatternSomewhere";
    }
}