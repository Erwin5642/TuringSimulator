using System;
using TuringSimulator.Controller;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Tape;
using UnityEngine;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;
using ITS;

namespace TuringSimulator.GameFlow
{
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
        
        public ControllerPrefabs Prefabs { get; set; }

        private readonly ProgramWorkbench _workbench;

        public ControllerInstaller(ControllerPrefabs prefabs, ModelInstaller model, ViewInstaller view)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            Prefabs = prefabs ?? throw new ArgumentNullException(nameof(prefabs));
            _workbench = prefabs.programWorkbench;
        }

        public ProgramWorkbench Workbench => _workbench;
        
        public void Install()
        {
            StepApplier = new StepViewApplier(_model.Buffer, _view.Machine);
            Playback = new PlaybackController(StepApplier);
            ProgramEdit = new ProgramEditController();
            GameFlowController = new GameFlowController(_model, _view, this);
            PlayerInputCatcher = UnityEngine.Object.Instantiate(Prefabs.input).GetComponent<PlayerInputCatcher>();

            _workbench?.Initialize(ProgramEdit);
            
            PlayerInputCatcher.OnStartRequest += GameFlowController.Run;
            PlayerInputCatcher.OnPauseRequest += Playback.Pause;
            PlayerInputCatcher.OnPlayRequest += Playback.Play;
            PlayerInputCatcher.OnForwardRequest += Playback.StepForward;
            PlayerInputCatcher.OnBackwardRequest += Playback.StepBackward;
            PlayerInputCatcher.OnNextRequest += GameFlowController.Next;
            
            ProgramEdit.OnProgramChanged += p =>
            {
                _model.Simulation.SetProgram(p);
                _model.Validation.SetProgram(p);
            };

            Playback.OnStep += result =>
            {
                LiveTutorSocket.Instance?.SendPlaybackStep(result);
                Debug.Log("[Event] OnStep triggered");
                if (result.Kind == ResultKind.Halt) GameFlowController.Halt();
            };

            _model.Levels.OnLevelChanged += level =>
            {
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
                SkillTracker.Instance?.OnLevelLoaded(levelId);

                LiveTutorSocket.Instance?.SendLevelSnapshot(
                    level.title,
                    level.description,
                    test.initialSymbols,
                    test.headIndex);
            };
        }
    }
}