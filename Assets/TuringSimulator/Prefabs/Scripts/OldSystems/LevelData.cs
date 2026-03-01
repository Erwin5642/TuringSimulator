using System;
using UnityEngine;

public enum ChallengeType
{
    Language,
    Function
};

[CreateAssetMenu(fileName = "Level", menuName = "Game/Level", order = 0)]
public class LevelData : ScriptableObject
{
    public string prompt;
    public ChallengeType challengeType;
    public SymbolType[] mainInputTape;
    public SymbolType[] mainExpectedOutput;
    public bool mainExpectedValue = true;

    public LevelTest[] additionalTests;
}

[Serializable]
public class LevelTest
{
    public SymbolType[] inputTape;
    public SymbolType[] expectedTapeOutput;
    public bool expectedValue = true;
}