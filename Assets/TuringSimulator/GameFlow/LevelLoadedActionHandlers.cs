using System;
using ITS;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Types;
using TuringSimulator.Core.Validation;
using TuringSimulator.View.Machine.Tape;

namespace TuringSimulator.GameFlow.Events
{
    public readonly struct LevelLoadedActionContext
    {
        public LevelLoadedActionContext(LevelDefinition level, ModelInstaller model, ViewInstaller view)
        {
            Level = level;
            Model = model;
            View = view;
        }

        public LevelDefinition Level { get; }
        public ModelInstaller Model { get; }
        public ViewInstaller View { get; }
    }

    public interface ILevelLoadedActionHandler
    {
        void Apply(LevelLoadedActionContext context);
    }

    public static class LevelLoadedActionHelpers
    {
        public static string ResolveLevelId(LevelDefinition level)
        {
            return string.IsNullOrWhiteSpace(level.levelId)
                ? LevelID.MoveLeftRight
                : level.levelId;
        }
    }

    public sealed class LevelModelTapeSetupActionHandler : ILevelLoadedActionHandler
    {
        public void Apply(LevelLoadedActionContext context)
        {
            var pool = LevelTestPool.Require(context.Level);
            var selected = context.Model.PlayTestSelector.Select(pool, context.Model.ActivePlayTest);
            context.Model.ActivePlayTest = selected;
            context.Model.Buffer.Clear();
            context.Model.CurrentTape = new SimulationTape(0);
        }
    }

    public sealed class LevelValidationTestsSetupActionHandler : ILevelLoadedActionHandler
    {
        public void Apply(LevelLoadedActionContext context)
        {
            context.Model.Validation.SetTests(LevelTestPool.Require(context.Level));
        }
    }

    public sealed class LevelViewResetActionHandler : ILevelLoadedActionHandler
    {
        public void Apply(LevelLoadedActionContext context)
        {
            context.View.Tape.SetTape(Array.Empty<Symbol>(), 0);
        }
    }

    public sealed class LevelUiMetadataActionHandler : ILevelLoadedActionHandler
    {
        public void Apply(LevelLoadedActionContext context)
        {
            context.View.LevelUI.SetLevelTitle(context.Level.title);
            context.View.LevelUI.SetLevelDescription(context.Level.description);
        }
    }

    public sealed class LevelSessionContextActionHandler : ILevelLoadedActionHandler
    {
        public void Apply(LevelLoadedActionContext context)
        {
            var levelId = LevelLoadedActionHelpers.ResolveLevelId(context.Level);
            SkillTracker.Instance?.OnLevelLoaded(levelId);
        }
    }

    public sealed class LevelItsBenchDirtyActionHandler : ILevelLoadedActionHandler
    {
        readonly IItsBenchStateCache _benchCache;

        public LevelItsBenchDirtyActionHandler(IItsBenchStateCache benchCache)
        {
            _benchCache = benchCache ?? throw new ArgumentNullException(nameof(benchCache));
        }

        public void Apply(LevelLoadedActionContext context)
        {
            _benchCache.MarkTapeDirty();
            _benchCache.MarkProgramDirty();
        }
    }

    public sealed class PlayTapeMaterializeActionHandler
    {
        readonly IItsBenchStateCache _benchCache;

        public PlayTapeMaterializeActionHandler(IItsBenchStateCache benchCache)
        {
            _benchCache = benchCache ?? throw new ArgumentNullException(nameof(benchCache));
        }

        public void Apply(ModelInstaller model, ITapeVisual tape)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (tape == null)
                throw new ArgumentNullException(nameof(tape));

            var test = model.ActivePlayTest
                ?? throw new InvalidOperationException("ActivePlayTest must be selected before a run.");

            var symbols = test.initialSymbols ?? Array.Empty<Symbol>();
            model.CurrentTape = new SimulationTape(test.headIndex, symbols);
            tape.SetTape(symbols, test.headIndex);
            _benchCache.MarkTapeDirty();
        }
    }
}
