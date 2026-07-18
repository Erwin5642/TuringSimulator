using System;
using TuringSimulator.Controller;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Tape;
using UnityEngine;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;
using TuringSimulator.GameFlow.Events;
using ITS;

namespace TuringSimulator.GameFlow
{
    [Serializable]
    public class ControllerSceneBindings
    {
        public PlayerInputCatcher input;
        public ProgramWorkbench programWorkbench;
    }

    [Serializable]
    public class ControllerPrefabs
    {
        public GameObject input;
        public ProgramWorkbench programWorkbench;
    }
    
    public sealed class ControllerInstaller 
    {
        private readonly ModelInstaller _model;
        private readonly ViewInstaller _view;
        
        // Controller prefabs
        
        // Core controllers
        public PlaybackController Playback { get; private set; }
        
        public PlayerInputCatcher PlayerInputCatcher { get; set; }
        public ProgramEditController ProgramEdit { get; private set; }
        public StepViewApplier StepApplier { get; set; }
        public GameFlowController GameFlowController { get; set; }
        
        readonly ControllerPrefabs _prefabs;
        readonly ControllerSceneBindings _scene;
        readonly bool _useSceneBindings;

        private readonly ProgramWorkbench _workbench;
        private int _playbackStepIndex = -1;

        public ControllerInstaller(ControllerPrefabs prefabs, ModelInstaller model, ViewInstaller view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
            _workbench = prefabs.programWorkbench;
            _useSceneBindings = false;
        }

        public ControllerInstaller(ControllerSceneBindings sceneBindings, ModelInstaller model, ViewInstaller view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _scene = sceneBindings ?? throw new ArgumentNullException(nameof(sceneBindings));
            _workbench = sceneBindings.programWorkbench;
            _useSceneBindings = true;
        }

        public ProgramWorkbench Workbench => _workbench;
        
        public void Install()
        {
            StepApplier = new StepViewApplier(_model.Buffer, _view.Machine);
            Playback = new PlaybackController(StepApplier);
            ProgramEdit = new ProgramEditController();
            GameFlowController = new GameFlowController(_model, _view, this);
            if (_useSceneBindings)
            {
                PlayerInputCatcher = _scene.input;
                if (PlayerInputCatcher == null)
                    throw new InvalidOperationException("Controller scene binding requires input reference.");
            }
            else
            {
                PlayerInputCatcher = UnityEngine.Object.Instantiate(_prefabs.input).GetComponent<PlayerInputCatcher>();
            }

            _workbench?.Initialize(ProgramEdit);
            
            PlayerInputCatcher.OnStartRequest += HandleStartRequested;
            PlayerInputCatcher.OnPauseRequest += Playback.Pause;
            PlayerInputCatcher.OnPlayRequest += Playback.Play;
            PlayerInputCatcher.OnForwardRequest += Playback.StepForward;
            PlayerInputCatcher.OnBackwardRequest += Playback.StepBackward;
            PlayerInputCatcher.OnNextRequest += GameFlowController.Next;
            PlayerInputCatcher.OnMenuRequest += HandleMenuRequested;
            
            ProgramEdit.OnProgramChanged += HandleProgramChanged;

            Playback.OnStep += HandlePlaybackStep;

            _model.Levels.OnLevelChanged += HandleLevelChanged;
        }

        void HandleMenuRequested()
        {
            GameFlowController.ReturnToMenu();
            SkillTracker.Instance?.ClearSession();
        }

        void HandleProgramChanged(IProgram program)
        {
            _model.Simulation.SetProgram(program);
            _model.Validation.SetProgram(program);

            var eventData = new ProgramChangedEventData(
                BuildEventContext(nameof(ProgramEditController), "program-changed"),
                program?.States?.Count ?? 0,
                program?.FinalStates?.Count ?? 0);
            EventTraceLog.Record(nameof(ProgramChangedEventData), eventData.ToString(), _workbench);
        }

        void HandlePlaybackStep(StepResult result)
        {
            _playbackStepIndex++;
            var stepData = new PlaybackStepEventData(
                BuildEventContext(nameof(PlaybackController), _playbackStepIndex.ToString()),
                _playbackStepIndex,
                result.Kind);
            EventTraceLog.Record(nameof(PlaybackStepEventData), stepData.ToString(), PlayerInputCatcher);

            LiveTutorSocket.Instance?.SendPlaybackStep(result);
            if (result.Kind != ResultKind.Halt)
                return;

            var haltData = new HaltReachedEventData(
                BuildEventContext(nameof(PlaybackController), $"halt-{_playbackStepIndex}"),
                result.AsHalt());
            EventTraceLog.Record(nameof(HaltReachedEventData), haltData.ToString(), PlayerInputCatcher);
            GameFlowController.Halt();
        }

        void HandleLevelChanged(LevelDefinition level)
        {
            _playbackStepIndex = -1;

            var test = level.mainTest;
            var tape = new SimulationTape(test.headIndex, test.initialSymbols);
            
            _model.Buffer.Clear();
            _model.Simulation.SetTape(tape);
            _model.Validation.SetTests(level.validationTests);

            _view.Tape.SetTape(test.initialSymbols, test.headIndex);
            _view.Halt.Reset();
            
            _view.LevelUI.SetLevelTitle(level.title);
            _view.LevelUI.SetLevelDescription(level.description);

            var levelId = string.IsNullOrWhiteSpace(level.levelId)
                ? ITS.LevelID.MoveLeftRight
                : level.levelId;

            var levelData = new LevelLoadedEventData(
                BuildEventContext(nameof(LevelDefinition), levelId),
                levelId,
                level.title,
                level.ValidationScenarioCount);
            EventTraceLog.Record(nameof(LevelLoadedEventData), levelData.ToString(), level);

            SkillTracker.Instance?.OnLevelLoaded(levelId);
            LiveTutorSocket.Instance?.SendLevelSnapshot(
                level.title,
                level.description,
                test.initialSymbols,
                test.headIndex);
        }

        static EventContextData BuildEventContext(string sourceName, string correlationId)
        {
            return new EventContextData(
                sourceName,
                correlationId ?? string.Empty,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        async void HandleStartRequested()
        {
            var gsm = GameStateMachine.Instance;
            if (gsm.CurrentState == GameState.Menu)
            {
                if (ITSClient.Instance != null && SkillTracker.Instance != null)
                {
                    var studentId = await ITSClient.Instance.RequestNewSessionAsync();
                    SkillTracker.Instance.BeginSession(studentId);
                }

                GameFlowController.Start();
                return;
            }

            GameFlowController.Run();
        }
    }
}