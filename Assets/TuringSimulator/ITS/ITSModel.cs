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
        public AskTapeDto tape;
        public AskProgramDto program;
    }

    [Serializable]
    public class AskTapeDto
    {
        public string[] Cells { get; set; }
        public int HeadOffset { get; set; }
    }

    [Serializable]
    public class AskProgramBlockDto
    {
        public string Id { get; set; }
        public string Tipo { get; set; }
        public string Cartao { get; set; }
    }

    [Serializable]
    public class AskProgramEdgeDto
    {
        public string De { get; set; }
        public string Porta { get; set; }
        public string Para { get; set; }
    }

    [Serializable]
    public class AskProgramDto
    {
        public bool TomadaLigada { get; set; }
        public string Entrada { get; set; }
        public AskProgramBlockDto[] Blocos { get; set; }
        public AskProgramEdgeDto[] Fios { get; set; }
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
