using System;
using System.Collections;
using System.Collections.Generic;
using MyLibrary.Scripts;
using UnityEngine;

public class CommandScheduler : MonoBehaviour
{
    private readonly Queue<ICommand> _commandQueue = new Queue<ICommand>();

    private bool _isRunning = false;
    private bool _isPaused = false;
    private bool _isHalted = false;
    private bool _isExecutingCommand = false;
    private bool _active = false;
    
    private Coroutine _schedulerRoutine;

    public TM machine;
    public UIManager uiManager;
    public LevelManager levelManager;
    public WireConnector beginningConnector;

    public event Action OnProgramStart;   // Event for when the program starts
    public event Action OnProgramEnd;     // Event for when the program ends
    public event Action OnProgramPause;   // Event for when the program pauses
    public event Action OnProgramResume;   // Event for when the program resumes
    public event Action OnProgramHalt;    // Event for when the program halts
    public event Action OnProgramReset;   // Event for when the program resets
    
    public void SetActive(bool active)
    {
        _active = active;
    }
    
    public void Enqueue(ICommand command)
    {
        if(_isRunning) _commandQueue.Enqueue(command);
    }

    public void StartScheduler()
    {
        if (_isRunning || _isHalted || !_active) return;

        if (beginningConnector.GetConnectedWireCount() == 0)
        {
            uiManager.specialText.text = "Nenhum programa conectado a porta inicial";
            return;
        }
        
        _isPaused = false;
        _isRunning = true;
        
        OnProgramStart?.Invoke();
        
        _schedulerRoutine = StartCoroutine(SchedulerLoop());
        
        uiManager.OnStartPressed();
        
        beginningConnector.PropagateSignal();
    }

    public void Pause()
    {
        if (!_isRunning || _isHalted) return;
        _isPaused = true;
        uiManager.OnPausePressed();
        OnProgramPause?.Invoke();
    }   

    public void Resume()
    {
        if (!_isRunning || _isHalted) return;
        _isPaused = false;
        uiManager.OnResumePressed();
        OnProgramResume?.Invoke();   
    }

    public void Halt()
    {
        if(!_isRunning) return;
        _isHalted = true;
        _isRunning = false;
        uiManager.OnHaltPressed();
        _commandQueue.Clear();
        OnProgramHalt?.Invoke();
    }

    public void Reset()
    {
        _isRunning = false;
        _isPaused = false;
        _isHalted = false;
        _isExecutingCommand = false;
        uiManager.OnResetPressed();
        levelManager.ReloadLevel();
        OnProgramReset?.Invoke();
    }

    private IEnumerator SchedulerLoop()
    {
        while (_isRunning)
        {
            if (_isHalted)
            {
                yield break;
            }

            if (_isPaused || _commandQueue.Count == 0 || _isExecutingCommand)
            {
                yield return null;
                continue;
            }

            var command = _commandQueue.Dequeue();
            _isExecutingCommand = true;

            yield return StartCoroutine(command.Execute());

            _isExecutingCommand = false;
        }
    }

    public void EndProgram()
    {
        if (levelManager.CheckChallengeCorrection())
        {
            uiManager.nextLevelButton.gameObject.SetActive(true);
        }
        else
        {
            uiManager.nextLevelButton.gameObject.SetActive(false);
        }
        uiManager.OnProgramEnded();
        OnProgramEnd?.Invoke();
    }
}
