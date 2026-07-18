using System;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    public class GameStateMachine {
        public static GameStateMachine Instance { get;  } = new GameStateMachine();
        public GameState CurrentState { get; private set; } = GameState.Menu;
        public GameState PreviousState { get; private set; }

        public event Action<GameState, GameState> OnStateChanged;

        public bool CanTransitionTo(GameState next)
        {
            var result = CurrentState switch
            {
                GameState.Menu => next == GameState.Loading,
                GameState.Loading => next is GameState.Editing or GameState.Menu,
                GameState.Editing => next is GameState.Running or GameState.Menu,
                GameState.Running => next is GameState.Halted,
                GameState.Halted => next is GameState.Validating or GameState.Menu,
                GameState.Validating => next is GameState.Victory or GameState.Defeat,
                GameState.Victory => next is GameState.Loading or GameState.Menu,
                GameState.Defeat => next is GameState.Debugging or GameState.Loading or GameState.Menu,
                GameState.Debugging => next is GameState.Loading or GameState.Menu,
                _ => false
            };
            if (!result) Debug.Log($"[GSM] Cannot transition from {CurrentState} to {next} state.");
            
            return result;
        }
    
        public bool TryTransition(GameState next)
        {
            if (!CanTransitionTo(next)) return false;

            var previous = CurrentState;
            PreviousState = previous;
            CurrentState = next;

            OnStateChanged?.Invoke(previous, next);
            
            Debug.Log("Transitioning from " + previous + " to " + next);
            
            return true;
        }
    }
}
