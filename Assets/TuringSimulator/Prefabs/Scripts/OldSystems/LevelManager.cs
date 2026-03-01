using System;
using MyLibrary.Scripts;
using Unity.Burst.Intrinsics;
using UnityEngine;
using UnityEngine.Serialization;

public class LevelManager : MonoBehaviour
{
    public LevelData[] levels;
    private int _currentLevelIndex = 0;
    private LevelData _currentLevel = null;
    private int _failedTestIndex = -1;

    [FormerlySerializedAs("turingMachine")] public TM tm; // Runtime machine
    public WireConnector beginPort;     // Entry wire of player-built program
    private VirtualProgram _testProgram;
    public UIManager ui;
    public CommandScheduler commandScheduler;

    private void Start()
    {
        LoadLevel(0);
    }
    
    public void LoadLevel(int index)
    {
        if (index < 0 || index >= levels.Length)
        {
            Debug.LogError("Invalid level index");
            return;
        }
        
        _currentLevelIndex = index;
        _currentLevel = levels[index];

        // Reset the Turing Machine for this level
        tm.Reset();
        tm.SetTape(_currentLevel.mainInputTape, 0);
        ui.nextLevelButton.gameObject.SetActive(false);
        // Show the challenge to the player
        ui.ShowPrompt(_currentLevel.prompt);

        // Setup UI for expectations
        if (_currentLevel.challengeType == ChallengeType.Language)
            ui.ShowExpectedOutput(_currentLevel.mainExpectedOutput);

        ui.specialText.text = "";
        ui.resultText.text = "";

        commandScheduler.SetActive(true);
    }

    public void ReloadLevel()
    {
        LoadLevel(_currentLevelIndex);  
    }

    public bool CheckChallengeCorrection()
    {
        // 1. Try to build a program from current player wiring
        _testProgram = new VirtualProgram();
        _testProgram.CreateProgram(beginPort);

        if (_testProgram.rootNode == null)
        {
            Debug.LogWarning("Program not connected or invalid");
            ui.ShowResult(false, "Programa incompleto ou não conectado");
            return false;
        }

        
        VirtualTuringMachine machine = new VirtualTuringMachine();
        VirtualProcess mainProcess = _testProgram.CreateProcess(new[] { _currentLevel.mainInputTape });

        bool mainResult = mainProcess.Execute();

        bool passedMain = false;

        switch (_currentLevel.challengeType)
        {
            case ChallengeType.Language:
                SymbolType[] output = mainProcess.GetTapeOutput(0);
                passedMain = CompareTapes(output, _currentLevel.mainExpectedOutput);
                break;

            case ChallengeType.Function:
                passedMain = mainResult == _currentLevel.mainExpectedValue;
                break;
        }

        if (!passedMain)
        {
            ui.ShowResult(false, "Programa fornecido não cumpre com o desafio");
            return false;
        }
        
        int index = 0;
        foreach (var test in _currentLevel.additionalTests)
        {
            VirtualProcess testProcess = _testProgram.CreateProcess(new[] { test.inputTape });
            testProcess.Initialize(new[] { test.inputTape });

            bool testResult = testProcess.Execute();

            bool subPassed = false;

            switch (_currentLevel.challengeType)
            {
                case ChallengeType.Language:
                    SymbolType[] outTape = testProcess.GetTapeOutput(0);
                    subPassed = CompareTapes(outTape, test.expectedTapeOutput);
                    break;

                case ChallengeType.Function:
                    subPassed = testResult == test.expectedValue;
                    break;
            }

            if (!subPassed)
            {
                ui.ShowResult(false, "Falhou em teste extra.");
                _failedTestIndex = index;
                (_currentLevel.mainInputTape, _currentLevel.additionalTests[index].inputTape) = 
                    (_currentLevel.additionalTests[index].inputTape, _currentLevel.mainInputTape);
                (_currentLevel.mainExpectedOutput, _currentLevel.additionalTests[index].expectedTapeOutput) = 
                    (_currentLevel.additionalTests[index].expectedTapeOutput, _currentLevel.mainExpectedOutput);
                return false;
            }
            index++;
        }
        
        ui.ShowResult(true, "Desafio concluido com sucesso!");
        commandScheduler.SetActive(false);
        return true;
    }

    private bool CompareTapes(SymbolType[] a, SymbolType[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }

        return true;
    }

    public void NextLevel()
    {
        int next = _currentLevelIndex + 1;
        if (next < levels.Length)
            LoadLevel(next);
        else
            ui.ShowEndOfGame();
    }
}