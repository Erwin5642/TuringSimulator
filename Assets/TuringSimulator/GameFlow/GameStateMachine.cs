using System;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    public class GameStateMachine {
        public static GameStateMachine Instance { get;  } = new GameStateMachine();
        public GameState CurrentState { get; private set; } = GameState.Menu;
        public GameState PreviousState { get; private set; }

        public event Action<GameState, GameState> OnStateChanged;

        public static bool IsAllowed(GameState from, GameState to)
        {
            return from switch
            {
                GameState.Menu => to == GameState.Loading,
                GameState.Loading => to is GameState.Editing or GameState.Menu,
                GameState.Editing => to is GameState.Running or GameState.Menu,
                GameState.Running => to is GameState.Halted or GameState.Editing,
                GameState.Halted => to is GameState.Validating or GameState.Menu,
                GameState.Validating => to is GameState.Victory or GameState.Defeat,
                GameState.Victory => to is GameState.Loading or GameState.Menu,
                GameState.Defeat => to is GameState.Editing or GameState.Debugging or GameState.Loading or GameState.Menu,
                GameState.Debugging => to is GameState.Editing or GameState.Loading or GameState.Menu,
                _ => false
            };
        }

        public bool CanTransitionTo(GameState next)
        {
            var result = IsAllowed(CurrentState, next);
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
