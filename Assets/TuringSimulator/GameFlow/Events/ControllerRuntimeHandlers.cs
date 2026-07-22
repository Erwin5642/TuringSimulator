using System;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Program;

namespace TuringSimulator.GameFlow.Events
{
    public sealed class RunRequestedEventRuntimeHandler
    {
        readonly GameFlowController _gameFlow;

        public RunRequestedEventRuntimeHandler(GameFlowController gameFlow)
        {
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
        }

        public async void Handle(RunRequestedEventData eventData)
        {
            if (eventData.RequestedState == GameState.Menu.ToString())
            {
                if (ITSClient.Instance != null && SkillTracker.Instance != null)
                {
                    var studentId = await ITSClient.Instance.RequestNewSessionAsync();
                    SkillTracker.Instance.BeginSession(studentId);
                }

                _gameFlow.Start();
                return;
            }

            if (eventData.RequestedState == GameState.Editing.ToString())
            {
                _gameFlow.Run();
                return;
            }
        }
    }

    public sealed class ProgramChangedEventRuntimeHandler
    {
        readonly ModelInstaller _model;

        public ProgramChangedEventRuntimeHandler(ModelInstaller model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void Handle(IProgram program)
        {
            _model.CurrentProgram = program;
            _model.Validation.SetProgram(program);
        }
    }

    public sealed class HaltReachedEventRuntimeHandler
    {
        readonly GameFlowController _gameFlow;

        public HaltReachedEventRuntimeHandler(GameFlowController gameFlow)
        {
            _gameFlow = gameFlow ?? throw new ArgumentNullException(nameof(gameFlow));
        }

        public void Handle()
        {
            _gameFlow.Halt();
        }
    }

    public sealed class LevelLoadedEventRuntimeHandler
    {
        readonly ModelInstaller _model;
        readonly ViewInstaller _view;
        readonly ILevelLoadedActionHandler[] _handlers;

        public LevelLoadedEventRuntimeHandler(
            ModelInstaller model,
            ViewInstaller view,
            ILevelLoadedActionHandler[] handlers)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        }

        public void Handle(LevelDefinition level)
        {
            if (level == null)
                return;

            var levelContext = new LevelLoadedActionContext(level, _model, _view);
            for (var i = 0; i < _handlers.Length; i++)
                _handlers[i].Apply(levelContext);
        }
    }
}
