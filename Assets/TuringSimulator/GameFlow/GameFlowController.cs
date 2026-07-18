using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Types;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    public class GameFlowController
    {
        private readonly ControllerInstaller _controller;
        private readonly ViewInstaller _view;
        private readonly ModelInstaller _model;

        private readonly GameStateMachine _stateMachine = GameStateMachine.Instance;

		private readonly SemaphoreSlim _flowLock = new SemaphoreSlim(1, 1);
        
        private bool _busy;

        public GameFlowController(ModelInstaller model, ViewInstaller view, ControllerInstaller controller)
        {
            _model = model;
            _view = view;
            _controller = controller;
        }

        public void Start()
        {
            if (!_stateMachine.TryTransition(GameState.Loading))
                throw new InvalidOperationException("The game should be in a loading state.");

            _model.Simulation.Clear();
            _view.Machine.Reset();
            _controller.StepApplier.Reset();
            _model.LevelLoader.LoadCurrent();

            if (!_stateMachine.TryTransition(GameState.Editing))
                throw new InvalidOperationException("The game should be in a editing state.");

            _controller.Playback.Disable();
            _controller.ProgramEdit.Enable();

            ApplyInitialProgram();
        }

        void ApplyInitialProgram()
        {
            if (_controller.Workbench != null)
                _controller.Workbench.RebuildProgramFromScene();
            else
                ApplyFallbackSeedProgram();
        }

        void ApplyFallbackSeedProgram()
        {
            _controller.ProgramEdit.Clear();
            _controller.ProgramEdit.AddTransition(0, Symbol.Blank, new Transition(1, Symbol.Gear, MoveDirection.Right));
            _controller.ProgramEdit.AddFinalState(1);
        }

        public void Run()
        {
            if (_busy) { Debug.Log("[GameFlow]: Busy"); return;}
            _ = RunAsync();
        }

        private async Task RunAsync()
        {
            await _flowLock.WaitAsync();
            _busy = true;

            try
            {
                if (!_stateMachine.TryTransition(GameState.Running))
                    return;

                _controller.ProgramEdit.Disable();
                Debug.Log("[GameFlow] Starting simulation");
                LiveTutorSocket.Instance?.SendRunLifecycle("run_start");
                SkillTracker.Instance?.OnProgramRun(true);
                await _model.Simulation.Start();
                Debug.Log($"[GameFlow]: Result of simulation: {_model.Buffer.Status}");
                var i = 0;
                while (_model.Buffer.TryGetStep(i++, out var step))
                {
                    Debug.Log(step);
                }
                LiveTutorSocket.Instance?.SendRunLifecycle("run_finished");
                _controller.Playback.Enable();
            }
            catch (Exception e)
            {
                Debug.Log($"[GameFlow]: Running Async Exception: {e}");
            }
            finally
            {
                _busy = false;
                _flowLock.Release();
            }
        }

        public void Abort()
        {
            if (_busy) { Debug.Log("[GameFlow]: Busy"); return;}
            _ = AbortAsync();
        }

        private async Task AbortAsync()
        {
            await _flowLock.WaitAsync();
            _busy = true;

            try
            {
                _model.Simulation.Cancel();
                LiveTutorSocket.Instance?.SendRunLifecycle("run_abort");

                _controller.Playback.Disable();
                _controller.ProgramEdit.Enable();

                _stateMachine.TryTransition(GameState.Editing);
            }
            catch (Exception e)
            {
                Debug.Log($"[GameFlow]: Abort Async Exception: {e}");
            }
            finally
            {
                _busy = false;
                _flowLock.Release();
            }
        }

        // This will be called by the simulation engine event
        public void Halt()
        {
            _ = HaltAsync();
        }

        private async Task HaltAsync()
        {
            await _flowLock.WaitAsync();
            _busy = true;

            try
            {
                if (!_stateMachine.TryTransition(GameState.Halted))
                    return;

                _controller.Playback.Disable();

                if (!_stateMachine.TryTransition(GameState.Validating)) 
                    throw new InvalidOperationException("The game should be in a valid state.");
                
                await _model.Validation.Start();

                var program = _controller.ProgramEdit.Current;
                var runEvidence = ProgramResultAnalyzer.Analyze(program, _model.Buffer);

                if (_model.Validation.AllPassed)
                {
                    SkillTracker.Instance?.OnProgramSuccess(runEvidence);
                    SkillTracker.Instance?.OnLevelComplete();
                    _view.LevelUI.SetValidationSummary(_model.Validation.Results);
                    Victory();
                }
                else
                {
                    SkillTracker.Instance?.OnProgramFail(runEvidence);
                    _view.LevelUI.SetValidationSummary(_model.Validation.Results);
                    Defeat();
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[GameFlow]: Halt Async Exception: {e}");
            }
            finally
            {
                _busy = false;
                _flowLock.Release();
            }
        }

        public void Next()
        {
            if (!_stateMachine.TryTransition(GameState.Loading))
                throw new InvalidOperationException("The game should be in a loading state.");
            
            _model.Simulation.Clear();
            _view.Machine.Reset();
            _controller.StepApplier.Reset();
            
            if (_stateMachine.PreviousState == GameState.Defeat) 
                _model.LevelLoader.LoadCurrent();
            else if (_stateMachine.PreviousState == GameState.Victory)
                _model.LevelLoader.LoadNext();
            else throw new InvalidOperationException();

            if (!_stateMachine.TryTransition(GameState.Editing))
                throw new InvalidOperationException("The game should be in a editing state.");

            _controller.Playback.Disable();
            _controller.ProgramEdit.Enable();

            ApplyInitialProgram();
        }

        public void Victory()
        {
            _stateMachine.TryTransition(GameState.Victory);
        }

        public void Defeat()
        {
            _stateMachine.TryTransition(GameState.Defeat);
        }

        public void ReturnToMenu()
        {
            _model.Simulation.Clear();
            _model.LevelLoader.ResetProgress();
            _view.Machine.Reset();
            _controller.StepApplier.Reset();
            _controller.Playback.Disable();
            _controller.ProgramEdit.Disable();

            if (!_stateMachine.TryTransition(GameState.Menu))
                Debug.LogWarning("[GameFlow] Could not transition to Menu.");
        }
    }
}
