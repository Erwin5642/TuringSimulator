using System;
using TuringSimulator.Controller;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Program;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.GameFlow.Events;
using UnityEngine;

namespace TuringSimulator.GameFlow
{
    [Serializable]
    public class ControllerSceneBindings
    {
        public PlayerInputCatcher input;
        public ProgramWorkbench programWorkbench;

        [Header("Event Channels")]
        public RunRequestedEventChannel runRequestedChannel;
        public RunStartedEventChannel runStartedChannel;
        public RunFinishedEventChannel runFinishedChannel;
        public LevelLoadedEventChannel levelLoadedChannel;
        public ProgramChangedEventChannel programChangedChannel;
        public PlaybackStepEventChannel playbackStepChannel;
        public SimulationStepProducedEventChannel simulationStepProducedChannel;
        public HaltReachedEventChannel haltReachedChannel;
        public ValidationCompletedEventChannel validationCompletedChannel;
        public LevelOutcomeEventChannel levelOutcomeChannel;
    }

    [Serializable]
    public class ControllerPrefabs
    {
        public GameObject input;
        public ProgramWorkbench programWorkbench;

        [Header("Optional Event Channels")]
        public RunRequestedEventChannel runRequestedChannel;
        public RunStartedEventChannel runStartedChannel;
        public RunFinishedEventChannel runFinishedChannel;
        public LevelLoadedEventChannel levelLoadedChannel;
        public ProgramChangedEventChannel programChangedChannel;
        public PlaybackStepEventChannel playbackStepChannel;
        public SimulationStepProducedEventChannel simulationStepProducedChannel;
        public HaltReachedEventChannel haltReachedChannel;
        public ValidationCompletedEventChannel validationCompletedChannel;
        public LevelOutcomeEventChannel levelOutcomeChannel;
    }

    public sealed class ControllerInstaller
    {
        private readonly ModelInstaller _model;
        private readonly ViewInstaller _view;

        public PlaybackController Playback { get; private set; }
        public PlayerInputCatcher PlayerInputCatcher { get; private set; }
        public ProgramEditController ProgramEdit { get; private set; }
        public StepViewApplier StepApplier { get; private set; }
        public GameFlowController GameFlowController { get; private set; }

        readonly ControllerPrefabs _prefabs;
        readonly ControllerSceneBindings _scene;
        readonly bool _useSceneBindings;

        private readonly ProgramWorkbench _workbench;
        private readonly RunRequestedEventChannel _runRequestedChannel;
        private readonly RunStartedEventChannel _runStartedChannel;
        private readonly RunFinishedEventChannel _runFinishedChannel;
        private readonly LevelLoadedEventChannel _levelLoadedChannel;
        private readonly ProgramChangedEventChannel _programChangedChannel;
        private readonly PlaybackStepEventChannel _playbackStepChannel;
        private readonly SimulationStepProducedEventChannel _simulationStepProducedChannel;
        private readonly HaltReachedEventChannel _haltReachedChannel;
        private readonly ValidationCompletedEventChannel _validationCompletedChannel;
        private readonly LevelOutcomeEventChannel _levelOutcomeChannel;
        private readonly ILevelLoadedActionHandler[] _levelLoadedHandlers;
        private ProgramChangedEventRuntimeHandler _programChangedRuntimeHandler;
        private HaltReachedEventRuntimeHandler _haltReachedRuntimeHandler;
        private LevelLoadedEventRuntimeHandler _levelLoadedRuntimeHandler;
        private RunRequestedEventRuntimeHandler _runRequestedRuntimeHandler;
        private int _playbackStepIndex = -1;

        public ControllerInstaller(ControllerPrefabs prefabs, ModelInstaller model, ViewInstaller view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
            _workbench = prefabs.programWorkbench;
            _runRequestedChannel = prefabs.runRequestedChannel;
            _runStartedChannel = prefabs.runStartedChannel;
            _runFinishedChannel = prefabs.runFinishedChannel;
            _levelLoadedChannel = prefabs.levelLoadedChannel;
            _programChangedChannel = prefabs.programChangedChannel;
            _playbackStepChannel = prefabs.playbackStepChannel;
            _simulationStepProducedChannel = prefabs.simulationStepProducedChannel;
            _haltReachedChannel = prefabs.haltReachedChannel;
            _validationCompletedChannel = prefabs.validationCompletedChannel;
            _levelOutcomeChannel = prefabs.levelOutcomeChannel;
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
            _runStartedChannel = sceneBindings.runStartedChannel;
            _runFinishedChannel = sceneBindings.runFinishedChannel;
            _levelLoadedChannel = sceneBindings.levelLoadedChannel;
            _programChangedChannel = sceneBindings.programChangedChannel;
            _playbackStepChannel = sceneBindings.playbackStepChannel;
            _simulationStepProducedChannel = sceneBindings.simulationStepProducedChannel;
            _haltReachedChannel = sceneBindings.haltReachedChannel;
            _validationCompletedChannel = sceneBindings.validationCompletedChannel;
            _levelOutcomeChannel = sceneBindings.levelOutcomeChannel;
            _levelLoadedHandlers = BuildLevelLoadedHandlers();
            _useSceneBindings = true;
        }

        public ProgramWorkbench Workbench => _workbench;

        public void Install()
        {
            StepApplier = new StepViewApplier(_view.Machine);
            Playback = new PlaybackController(StepApplier);
            ProgramEdit = new ProgramEditController();
            GameFlowController = new GameFlowController(_model, _view, this);
            GameFlowController.ConfigureEventChannels(
                _runStartedChannel,
                _runFinishedChannel,
                _simulationStepProducedChannel,
                _validationCompletedChannel,
                _levelOutcomeChannel);
            _programChangedRuntimeHandler = new ProgramChangedEventRuntimeHandler(_model);
            _haltReachedRuntimeHandler = new HaltReachedEventRuntimeHandler(GameFlowController);
            _levelLoadedRuntimeHandler = new LevelLoadedEventRuntimeHandler(_model, _view, _levelLoadedHandlers);
            _runRequestedRuntimeHandler = new RunRequestedEventRuntimeHandler(GameFlowController);

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

            ValidateEventChannelWiring();

            _workbench?.Initialize(ProgramEdit);

            PlayerInputCatcher.OnStartRequest += PublishRunRequested;
            PlayerInputCatcher.OnPauseRequest += Playback.Pause;
            PlayerInputCatcher.OnPlayRequest += Playback.Play;
            PlayerInputCatcher.OnForwardRequest += Playback.StepForward;
            PlayerInputCatcher.OnBackwardRequest += Playback.StepBackward;
            PlayerInputCatcher.OnNextRequest += GameFlowController.Next;
            PlayerInputCatcher.OnMenuRequest += HandleMenuRequested;

            ProgramEdit.OnProgramChanged += PublishProgramChanged;
            Playback.OnStep += PublishPlaybackStep;
            _model.Levels.OnLevelChanged += PublishLevelLoaded;

            if (_runRequestedChannel != null)
                _runRequestedChannel.OnRaised += HandleRunRequestedEvent;
            if (_programChangedChannel != null)
                _programChangedChannel.OnRaised += HandleProgramChangedEvent;
            if (_playbackStepChannel != null)
                _playbackStepChannel.OnRaised += HandlePlaybackStepEvent;
            if (_haltReachedChannel != null)
                _haltReachedChannel.OnRaised += HandleHaltReachedEvent;
            if (_levelLoadedChannel != null)
                _levelLoadedChannel.OnRaised += HandleLevelLoadedEvent;
        }

        void HandleMenuRequested()
        {
            GameFlowController.ReturnToMenu();
            SkillTracker.Instance?.ClearSession();
        }

        void PublishProgramChanged(IProgram program)
        {
            var eventData = new ProgramChangedEventData(
                BuildEventContext(nameof(ProgramEditController), "program-changed"),
                program,
                program?.States?.Count ?? 0,
                program?.FinalStates?.Count ?? 0);
            EventTraceLog.Record(nameof(ProgramChangedEventData), eventData.ToString(), _workbench);
            if (_programChangedChannel != null)
            {
                _programChangedChannel.Raise(eventData, _workbench);
                return;
            }

            ApplyProgramChanged(program);
        }

        void HandleProgramChangedEvent(ProgramChangedEventData eventData)
        {
            ApplyProgramChanged(eventData.Program);
        }

        void ApplyProgramChanged(IProgram program)
        {
            _programChangedRuntimeHandler.Handle(program);
        }

        void PublishPlaybackStep(StepResult result)
        {
            _playbackStepIndex++;
            var stepData = new PlaybackStepEventData(
                BuildEventContext(nameof(PlaybackController), _playbackStepIndex.ToString()),
                _playbackStepIndex,
                result.Kind,
                result);
            EventTraceLog.Record(nameof(PlaybackStepEventData), stepData.ToString(), PlayerInputCatcher);
            if (_playbackStepChannel != null)
            {
                _playbackStepChannel.Raise(stepData, PlayerInputCatcher);
                return;
            }

            ApplyPlaybackStep(stepData);
        }

        void HandlePlaybackStepEvent(PlaybackStepEventData eventData)
        {
            ApplyPlaybackStep(eventData);
        }

        void ApplyPlaybackStep(PlaybackStepEventData eventData)
        {
            if (eventData.ResultKind != ResultKind.Halt)
                return;

            var haltData = new HaltReachedEventData(
                BuildEventContext(nameof(PlaybackController), $"halt-{eventData.StepIndex}"),
                eventData.Step.AsHalt(),
                eventData.Step);
            EventTraceLog.Record(nameof(HaltReachedEventData), haltData.ToString(), PlayerInputCatcher);
            if (_haltReachedChannel != null)
            {
                _haltReachedChannel.Raise(haltData, PlayerInputCatcher);
                return;
            }

            HandleHaltReachedEvent(haltData);
        }

        void HandleHaltReachedEvent(HaltReachedEventData _)
        {
            _haltReachedRuntimeHandler.Handle();
        }

        void PublishLevelLoaded(LevelDefinition level)
        {
            _playbackStepIndex = -1;
            if (level == null)
            {
                Debug.LogWarning("[ControllerInstaller] LevelLoader published a null level.");
                return;
            }

            var levelId = LevelLoadedActionHelpers.ResolveLevelId(level);
            var levelData = new LevelLoadedEventData(
                BuildEventContext(nameof(LevelDefinition), levelId),
                level,
                levelId,
                level.title,
                level.ValidationScenarioCount);

            EventTraceLog.Record(nameof(LevelLoadedEventData), levelData.ToString(), level);
            if (_levelLoadedChannel != null)
            {
                _levelLoadedChannel.Raise(levelData, level);
                return;
            }

            ApplyLevelLoaded(level);
        }

        void HandleLevelLoadedEvent(LevelLoadedEventData eventData)
        {
            ApplyLevelLoaded(eventData.Level);
        }

        void ApplyLevelLoaded(LevelDefinition level)
        {
            if (level == null)
                Debug.LogWarning("[ControllerInstaller] Received null level in level-loaded pipeline.");
            _levelLoadedRuntimeHandler.Handle(level);
        }

        static EventContextData BuildEventContext(string sourceName, string correlationId) =>
            EventContextFactory.Create(sourceName, correlationId);

        void PublishRunRequested()
        {
            var gsm = GameStateMachine.Instance;
            if (gsm.CurrentState == GameState.Menu)
            {
                var startFromMenu = new RunRequestedEventData(
                    BuildEventContext(nameof(PlayerInputCatcher), "start-from-menu"),
                    GameState.Menu.ToString());
                EventTraceLog.Record(nameof(RunRequestedEventData), startFromMenu.ToString(), PlayerInputCatcher);
                if (_runRequestedChannel != null)
                {
                    _runRequestedChannel.Raise(startFromMenu, PlayerInputCatcher);
                    return;
                }

                HandleRunRequestedEvent(startFromMenu);
                return;
            }

            var runFromEditing = new RunRequestedEventData(
                BuildEventContext(nameof(PlayerInputCatcher), "run-from-editing"),
                GameState.Editing.ToString());
            EventTraceLog.Record(nameof(RunRequestedEventData), runFromEditing.ToString(), PlayerInputCatcher);
            if (_runRequestedChannel != null)
            {
                _runRequestedChannel.Raise(runFromEditing, PlayerInputCatcher);
                return;
            }

            HandleRunRequestedEvent(runFromEditing);
        }

        void HandleRunRequestedEvent(RunRequestedEventData eventData)
        {
            if (eventData.RequestedState != GameState.Menu.ToString() &&
                eventData.RequestedState != GameState.Editing.ToString())
            {
                Debug.LogWarning($"[ControllerInstaller] Unsupported run request state: {eventData.RequestedState}");
                return;
            }

            _runRequestedRuntimeHandler.Handle(eventData);
        }

        ILevelLoadedActionHandler[] BuildLevelLoadedHandlers()
        {
            return new ILevelLoadedActionHandler[]
            {
                new LevelModelTapeSetupActionHandler(),
                new LevelValidationTestsSetupActionHandler(),
                new LevelViewResetActionHandler(),
                new LevelUiMetadataActionHandler(),
                new LevelSessionContextActionHandler(),
            };
        }

        void ValidateEventChannelWiring()
        {
            if (!_useSceneBindings)
                return;

            WarnIfMissing(_runRequestedChannel, nameof(ControllerSceneBindings.runRequestedChannel));
            WarnIfMissing(_runStartedChannel, nameof(ControllerSceneBindings.runStartedChannel));
            WarnIfMissing(_runFinishedChannel, nameof(ControllerSceneBindings.runFinishedChannel));
            WarnIfMissing(_levelLoadedChannel, nameof(ControllerSceneBindings.levelLoadedChannel));
            WarnIfMissing(_programChangedChannel, nameof(ControllerSceneBindings.programChangedChannel));
            WarnIfMissing(_playbackStepChannel, nameof(ControllerSceneBindings.playbackStepChannel));
            WarnIfMissing(_simulationStepProducedChannel, nameof(ControllerSceneBindings.simulationStepProducedChannel));
            WarnIfMissing(_haltReachedChannel, nameof(ControllerSceneBindings.haltReachedChannel));
            WarnIfMissing(_validationCompletedChannel, nameof(ControllerSceneBindings.validationCompletedChannel));
            WarnIfMissing(_levelOutcomeChannel, nameof(ControllerSceneBindings.levelOutcomeChannel));
        }

        static void WarnIfMissing(UnityEngine.Object value, string fieldName)
        {
            if (value == null)
                Debug.LogWarning($"[ControllerInstaller] Missing event channel wiring: {fieldName}");
        }
    }
}
