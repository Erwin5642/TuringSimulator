// ITSModels.cs — slim request/response shapes for session + ask on main.

using System;

namespace ITS
{
    [Serializable]
    public class AskRequest
    {
        public string student_id;
        public string level_id;
        public string question;
    }

    [Serializable]
    public class AskResponseDto
    {
        public string Reply { get; set; }
    }

    [Serializable]
    public class SessionNewResponseDto
    {
        public string StudentId { get; set; }
    }

    public static class LevelID
    {
        public const string MoveLeftRight = "MoveLeftRight";
        public const string PlaceGear = "PlaceGear";
        public const string AppendScrew = "AppendScrew";
        public const string ReplaceAllWithNuts = "ReplaceAllWithNuts";
        public const string RejectIfGearExists = "RejectIfGearExists";
        public const string SwapNutsAndScrews = "SwapNutsAndScrews";
        public const string PatternRepeated = "PatternRepeated";
        public const string BalancedPairs = "BalancedPairs";
        public const string PatternSomewhere = "PatternSomewhere";
    }
}
