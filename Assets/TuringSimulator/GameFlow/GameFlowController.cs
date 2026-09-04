using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using ITS;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Types;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    public class GameFlowController
    {
        private readonly ControllerInstaller _controller;
        private readonly ViewInstaller _view;
        private readonly ModelInstaller _model;
        private RunStartedEventChannel _runStartedChannel;
        private RunFinishedEventChannel _runFinishedChannel;
        private SimulationStepProducedEventChannel _simulationStepProducedChannel;
        private ValidationCompletedEventChannel _validationCompletedChannel;
        private LevelOutcomeEventChannel _levelOutcomeChannel;

        private readonly GameStateMachine _stateMachine = GameStateMachine.Instance;

		private readonly SemaphoreSlim _flowLock = new SemaphoreSlim(1, 1);
        
        private bool _busy;
        private bool _abortRequested;
        private int _simulationStepIndex;
        private Coroutine _runCoroutine;
        private Action<StepResult> _onSimulationStepProduced;

        public GameFlowController(ModelInstaller model, ViewInstaller view, ControllerInstaller controller)
        {
            _model = model;
            _view = view;
            _controller = controller;
        }

        public void ConfigureEventChannels(
            RunStartedEventChannel runStartedChannel,
            RunFinishedEventChannel runFinishedChannel,
            SimulationStepProducedEventChannel simulationStepProducedChannel,
            ValidationCompletedEventChannel validationCompletedChannel,
            LevelOutcomeEventChannel levelOutcomeChannel)
        {
            _runStartedChannel = runStartedChannel;
            _runFinishedChannel = runFinishedChannel;
            _simulationStepProducedChannel = simulationStepProducedChannel;
            _validationCompletedChannel = validationCompletedChannel;
            _levelOutcomeChannel = levelOutcomeChannel;
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
            _controller.Workbench?.ClearExecutionHighlight();

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
            if (_busy)
            {
                Debug.Log("[GameFlow]: Busy");
                return;
            }

            _runCoroutine = SimulationCoroutineHost.Instance.StartCoroutine(RunRoutine());
        }

        IEnumerator RunRoutine()
        {
            _busy = true;
            _abortRequested = false;
            _simulationStepIndex = 0;

            try
            {
                if (!_stateMachine.TryTransition(GameState.Running))
                    yield break;

                _controller.ProgramEdit.Disable();
                PublishRunStarted();
                _controller.Workbench?.HighlightStartWire();

                if (_model.CurrentProgram == null || _model.CurrentTape == null)
                {
                    Debug.Log("[GameFlow]: Running Exception: missing program or tape.");
                    ResetSimulationToEditing();
                    yield break;
                }

                _controller.StepApplier.Reset();
                _controller.Playback.Enable();
                _controller.Playback.Play();

                _onSimulationStepProduced = HandleSimulationStepProduced;
                _model.Simulation.OnStepProduced += _onSimulationStepProduced;

                var runRequest = new SimulationRunRequest(_model.CurrentProgram, _model.CurrentTape);
                SimulationRunResult runResult = default;
                var completed = false;
                Exception error = null;
                Debug.Log("[GameFlow] Starting simulation");

                // C# forbids yield inside try/catch; keep MoveNext in try and yield outside.
                var run = _model.Simulation.Run(
                    runRequest,
                    result =>
                    {
                        runResult = result;
                        completed = true;
                    });

                while (true)
                {
                    if (_abortRequested)
                        yield break;

                    object current;
                    try
                    {
                        if (!run.MoveNext())
                            break;
                        current = run.Current;
                    }
                    catch (Exception e)
                    {
                        error = e;
                        break;
                    }

                    yield return current;
                }

                if (_abortRequested)
                    yield break;

                if (error != null)
                {
                    Debug.Log($"[GameFlow]: Running Exception: {error}");
                    ResetSimulationToEditing();
                }
                else if (!completed)
                {
                    Debug.LogWarning("[GameFlow]: Simulation ended without a result.");
                    ResetSimulationToEditing();
                }
                else if (runResult.HaltStatus == HaltStatus.Aborted)
                {
                    Debug.Log("[GameFlow]: Simulation aborted");
                    ResetSimulationToEditing();
                }
                else
                {
                    Debug.Log($"[GameFlow]: Result of simulation: {runResult.HaltStatus}");
                    PublishRunFinished(runResult.HaltStatus, runResult.StepCount);
                }
            }
            finally
            {
                UnsubscribeSimulationSteps();
                _runCoroutine = null;
                _busy = false;
            }
        }

        void HandleSimulationStepProduced(StepResult step)
        {
            if (_abortRequested)
                return;
            if (step.IsHalt && step.AsHalt() == HaltStatus.Aborted)
                return;

            PublishSimulationStepProduced(step, _simulationStepIndex++);
            _controller.StepApplier.AppendStep(step);
            _controller.Playback.NotifyStepsAvailable();
        }

        public void Abort()
        {
            _abortRequested = true;
            _controller.Playback.Disable();
            _model.Simulation.Cancel();
            StopRunCoroutine();
            UnsubscribeSimulationSteps();
            ResetSimulationToEditing();
            _busy = false;
        }

        void StopRunCoroutine()
        {
            if (_runCoroutine == null)
                return;

            var host = SimulationCoroutineHost.InstanceOrNull;
            if (host != null)
                host.StopCoroutine(_runCoroutine);

            _runCoroutine = null;
        }

        void UnsubscribeSimulationSteps()
        {
            if (_onSimulationStepProduced == null)
                return;

            _model.Simulation.OnStepProduced -= _onSimulationStepProduced;
            _onSimulationStepProduced = null;
        }

        void ResetSimulationToEditing()
        {
            _controller.Playback.Disable();
            _model.Simulation.Clear();
            _view.Machine.Reset();
            _controller.StepApplier.Reset();
            _model.LevelLoader.LoadCurrent();
            _controller.ProgramEdit.Enable();
            _controller.Workbench?.ClearExecutionHighlight();
            ApplyInitialProgram();
            if (!_stateMachine.TryTransition(GameState.Editing))
                Debug.LogWarning("[GameFlow] Could not return to Editing after reset.");
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

                _view.LevelUI.SetValidationSummary(_model.Validation.Results);
                PublishValidationCompleted();
                if (_model.Validation.AllPassed)
                    Victory();
                else
                    Defeat();
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
            _controller.Workbench?.ClearExecutionHighlight();

            ApplyInitialProgram();
        }

        public void Victory()
        {
            _stateMachine.TryTransition(GameState.Victory);
            PublishLevelOutcome(LevelOutcomeKind.Victory);
        }

        public void Defeat()
        {
            _stateMachine.TryTransition(GameState.Defeat);
            PublishLevelOutcome(LevelOutcomeKind.Defeat);
            ReturnToEditingAfterDefeat();
        }

        void ReturnToEditingAfterDefeat()
        {
            _model.Simulation.Clear();
            _view.Machine.Reset();
            _controller.StepApplier.Reset();
            _model.LevelLoader.LoadCurrent();
            _controller.Playback.Disable();
            _controller.ProgramEdit.Enable();
            _controller.Workbench?.ClearExecutionHighlight();
            ApplyInitialProgram();
            if (!_stateMachine.TryTransition(GameState.Editing))
                Debug.LogWarning("[GameFlow] Could not return to Editing after Defeat.");
        }

        public void ReturnToMenu()
        {
            _abortRequested = true;
            StopRunCoroutine();
            UnsubscribeSimulationSteps();
            _model.Simulation.Clear();
            _model.LevelLoader.ResetProgress();
            _view.Machine.Reset();
            _controller.StepApplier.Reset();
            _controller.Playback.Disable();
            _controller.ProgramEdit.Disable();
            _controller.Workbench?.ClearExecutionHighlight();
            _busy = false;

            if (!_stateMachine.TryTransition(GameState.Menu))
                Debug.LogWarning("[GameFlow] Could not transition to Menu.");
        }

        private void PublishRunStarted()
        {
            if (_runStartedChannel == null)
                return;

            var context = EventContextFactory.Create(nameof(GameFlowController), "run-start");
            var payload = new RunStartedEventData(context, _stateMachine.PreviousState.ToString());
            EventTraceLog.Record(nameof(RunStartedEventData), payload.ToString());
            _runStartedChannel.Raise(payload);
        }

        private void PublishRunFinished(HaltStatus haltStatus, int stepCount)
        {
            if (_runFinishedChannel == null)
                return;

            var context = EventContextFactory.Create(nameof(GameFlowController), "run-finished");
            var payload = new RunFinishedEventData(context, haltStatus, stepCount);
            EventTraceLog.Record(nameof(RunFinishedEventData), payload.ToString());
            _runFinishedChannel.Raise(payload);
        }

        private void PublishSimulationStepProduced(StepResult step, int stepIndex)
        {
            if (_simulationStepProducedChannel == null)
                return;

            var context = EventContextFactory.Create(nameof(GameFlowController), $"sim-step-{stepIndex}");
            var payload = new SimulationStepProducedEventData(context, stepIndex, step.Kind, step);
            EventTraceLog.Record(nameof(SimulationStepProducedEventData), payload.ToString());
            _simulationStepProducedChannel.Raise(payload);
        }

        private void PublishValidationCompleted()
        {
            if (_validationCompletedChannel == null)
                return;

            var results = _model.Validation.Results;
            var passedCount = 0;
            for (var i = 0; i < results.Count; i++)
            {
                if (results[i].Passed)
                    passedCount++;
            }

            var levelId = SkillTracker.Instance?.GetCurrentLevelId() ?? LevelID.MoveLeftRight;
            var context = EventContextFactory.Create(nameof(GameFlowController), $"validation-{levelId}");
            var payload = new ValidationCompletedEventData(
                context,
                levelId,
                _model.Validation.AllPassed,
                passedCount,
                results.Count);
            EventTraceLog.Record(nameof(ValidationCompletedEventData), payload.ToString());
            _validationCompletedChannel.Raise(payload);
        }

        private void PublishLevelOutcome(LevelOutcomeKind outcome)
        {
            if (_levelOutcomeChannel == null)
                return;

            var levelId = SkillTracker.Instance?.GetCurrentLevelId() ?? LevelID.MoveLeftRight;
            var context = EventContextFactory.Create(nameof(GameFlowController), $"outcome-{levelId}");
            var payload = new LevelOutcomeEventData(context, levelId, outcome);
            EventTraceLog.Record(nameof(LevelOutcomeEventData), payload.ToString());
            _levelOutcomeChannel.Raise(payload);
        }
    }
}
