using System;
using System.Collections;
using MyLibrary.Scripts;
using UnityEngine;

public class TuringCommand : ICommand
{
    private readonly TM _machine;
    private readonly int[] _parameters;
    private readonly Action<bool> _onComplete;

    private bool _isPaused;
    private bool _isHalted;
    private bool _hasResult;
    private bool _result;
    
    public TuringCommand(TM machine, int[] parameters, Action<bool> onComplete)
    {
        _machine     = machine;
        _parameters  = parameters;
        _onComplete  = onComplete;
    }

    public IEnumerator Execute()
    {
        var prog = _machine.ExecuteProgram(_parameters, result => {
            _hasResult = true;
            _result    = result;
        });
        
        while (prog.MoveNext())
        {
            if (_isHalted) yield break;
            if (_isPaused) {
                yield return null;
                continue;
            }
            yield return prog.Current;
        }
        
        while (!_hasResult) {
            yield return null;
        }
        
        _onComplete?.Invoke(_result);
    }

    public void Pause()   => _isPaused = true;
    public void Resume()  => _isPaused = false;
    public void Halt() => _isHalted = true;

    public void Reset()
    {
        _isPaused  = false;
        _isHalted  = false;
        _hasResult = false;
        _result    = false;
    }
}