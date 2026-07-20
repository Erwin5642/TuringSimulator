using System;
using TuringSimulator.Controller;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Program;
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
        public ProgramWorkbench programWorkbench;

        [Header("Event Channels")]
        public RunRequestedEventChannel runRequestedChannel;
        public LevelLoadedEventChannel levelLoadedChannel;
        public ProgramChangedEventChannel programChangedChannel;
        public PlaybackStepEventChannel playbackStepChannel;
        public HaltReachedEventChannel haltReachedChannel;
    }

    [Serializable]
    public class ControllerPrefabs
    {
        public ProgramWorkbench programWorkbench;

        [Header("Optional Event Channels")]
        public RunRequestedEventChannel runRequestedChannel;
        public LevelLoadedEventChannel levelLoadedChannel;
        public ProgramChangedEventChannel programChangedChannel;
        public PlaybackStepEventChannel playbackStepChannel;
        public HaltReachedEventChannel haltReachedChannel;
    }
    
    public sealed class ControllerInstaller 
    {
        private readonly ModelInstaller _model;
        private readonly ViewInstaller _view;
        
        // Controller prefabs
        
        // Core controllers
        public PlaybackController Playback { get; private set; }
        public ProgramEditController ProgramEdit { get; private set; }
        public StepViewApplier StepApplier { get; set; }
        public GameFlowController GameFlowController { get; set; }
        
        readonly ControllerPrefabs _prefabs;
        readonly ControllerSceneBindings _scene;
        readonly bool _useSceneBindings;

        private readonly ProgramWorkbench _workbench;
        private readonly RunRequestedEventChannel _runRequestedChannel;
        private readonly LevelLoadedEventChannel _levelLoadedChannel;
        private readonly ProgramChangedEventChannel _programChangedChannel;
        private readonly PlaybackStepEventChannel _playbackStepChannel;
        private readonly HaltReachedEventChannel _haltReachedChannel;
        private readonly ILevelLoadedActionHandler[] _levelLoadedHandlers;
        private int _playbackStepIndex = -1;

        public ControllerInstaller(ControllerPrefabs prefabs, ModelInstaller model, ViewInstaller view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
            _workbench = prefabs.programWorkbench;
            _runRequestedChannel = prefabs.runRequestedChannel;
            _levelLoadedChannel = prefabs.levelLoadedChannel;
            _programChangedChannel = prefabs.programChangedChannel;
            _playbackStepChannel = prefabs.playbackStepChannel;
            _haltReachedChannel = prefabs.haltReachedChannel;
            _levelLoadedHandlers = BuildLevelLoadedHandlers();
            _useSceneBindings = false;
        }

        public ControllerInstaller(ControllerSceneBindings sceneBindings, ModelInstaller model, ViewInstaller view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _scene = sceneBindings ?? throw new ArgumentNullException(nameof(sceneBindings));
            _workbench = sceneBindings.programWorkbench;
            _runRequestedChannel = sceneBindings.runRequestedChannel;
            _levelLoadedChannel = sceneBindings.levelLoadedChannel;
            _programChangedChannel = sceneBindings.programChangedChannel;
            _playbackStepChannel = sceneBindings.playbackStepChannel;
            _haltReachedChannel = sceneBindings.haltReachedChannel;
            _levelLoadedHandlers = BuildLevelLoadedHandlers();
            _useSceneBindings = true;
        }

        public ProgramWorkbench Workbench => _workbench;
        
        public void Install()
        {
            StepApplier = new StepViewApplier(_model.Buffer, _view.Machine);
            Playback = new PlaybackController(StepApplier);
            ProgramEdit = new ProgramEditController();
            GameFlowController = new GameFlowController(_model, _view, this);
            ValidateEventChannelWiring();

            _workbench?.Initialize(ProgramEdit);

            ProgramEdit.OnProgramChanged += HandleProgramChanged;

            Playback.OnStep += HandlePlaybackStep;

            _model.Levels.OnLevelChanged += HandleLevelChanged;
        }

        void HandleMenuRequested()
        {
            GameFlowController.ReturnToMenu();
            SkillTracker.Instance?.ClearSession();
        }

        public void RequestStartOrRun()
        {
            HandleStartRequested();
        }

        public void RequestPausePlayback()
        {
            Playback.Pause();
        }

        public void RequestPlayPlayback()
        {
            Playback.Play();
        }

        public void RequestStepForward()
        {
            Playback.StepForward();
        }

        public void RequestStepBackward()
        {
            Playback.StepBackward();
        }

        public void RequestNextLevel()
        {
            GameFlowController.Next();
        }

        public void RequestReturnToMenu()
        {
            HandleMenuRequested();
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
            _programChangedChannel?.Raise(eventData, _workbench);
        }

        void HandlePlaybackStep(StepResult result)
        {
            _playbackStepIndex++;
            var stepData = new PlaybackStepEventData(
                BuildEventContext(nameof(PlaybackController), _playbackStepIndex.ToString()),
                _playbackStepIndex,
                result.Kind);
            EventTraceLog.Record(nameof(PlaybackStepEventData), stepData.ToString(), _workbench);
            _playbackStepChannel?.Raise(stepData, _workbench);

            LiveTutorSocket.Instance?.SendPlaybackStep(result);
            if (result.Kind != ResultKind.Halt)
                return;

            var haltData = new HaltReachedEventData(
                BuildEventContext(nameof(PlaybackController), $"halt-{_playbackStepIndex}"),
                result.AsHalt());
            EventTraceLog.Record(nameof(HaltReachedEventData), haltData.ToString(), _workbench);
            _haltReachedChannel?.Raise(haltData, _workbench);
            GameFlowController.Halt();
        }

        void HandleLevelChanged(LevelDefinition level)
        {
            _playbackStepIndex = -1;

            var levelId = LevelLoadedActionHelpers.ResolveLevelId(level);

            var levelData = new LevelLoadedEventData(
                BuildEventContext(nameof(LevelDefinition), levelId),
                levelId,
                level.title,
                level.ValidationScenarioCount);
            EventTraceLog.Record(nameof(LevelLoadedEventData), levelData.ToString(), level);
            _levelLoadedChannel?.Raise(levelData, level);

            var levelContext = new LevelLoadedActionContext(level, _model, _view);
            for (var i = 0; i < _levelLoadedHandlers.Length; i++)
                _levelLoadedHandlers[i].Apply(levelContext);
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
                var startFromMenu = new RunRequestedEventData(
                    BuildEventContext(nameof(ControllerInstaller), "start-from-menu"),
                    GameState.Menu.ToString());
                EventTraceLog.Record(nameof(RunRequestedEventData), startFromMenu.ToString(), _workbench);
                _runRequestedChannel?.Raise(startFromMenu, _workbench);

                if (ITSClient.Instance != null && SkillTracker.Instance != null)
                {
                    var studentId = await ITSClient.Instance.RequestNewSessionAsync();
                    SkillTracker.Instance.BeginSession(studentId);
                }

                GameFlowController.Start();
                return;
            }

            var runFromEditing = new RunRequestedEventData(
                BuildEventContext(nameof(ControllerInstaller), "run-from-editing"),
                GameState.Editing.ToString());
            EventTraceLog.Record(nameof(RunRequestedEventData), runFromEditing.ToString(), _workbench);
            _runRequestedChannel?.Raise(runFromEditing, _workbench);
            GameFlowController.Run();
        }

        ILevelLoadedActionHandler[] BuildLevelLoadedHandlers()
        {
            return new ILevelLoadedActionHandler[]
            {
                new LevelModelTapeSetupActionHandler(),
                new LevelValidationTestsSetupActionHandler(),
                new LevelViewResetActionHandler(),
                new LevelUiMetadataActionHandler(),
                new LevelTutorSnapshotActionHandler()
            };
        }

        void ValidateEventChannelWiring()
        {
            if (!_useSceneBindings)
                return;

            WarnIfMissing(_runRequestedChannel, nameof(ControllerSceneBindings.runRequestedChannel));
            WarnIfMissing(_levelLoadedChannel, nameof(ControllerSceneBindings.levelLoadedChannel));
            WarnIfMissing(_programChangedChannel, nameof(ControllerSceneBindings.programChangedChannel));
            WarnIfMissing(_playbackStepChannel, nameof(ControllerSceneBindings.playbackStepChannel));
            WarnIfMissing(_haltReachedChannel, nameof(ControllerSceneBindings.haltReachedChannel));
        }

        static void WarnIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                Debug.LogWarning($"[ControllerInstaller] Missing event channel wiring: {fieldName}");
        }
    }
}