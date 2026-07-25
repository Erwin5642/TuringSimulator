using System;
using ITS;
using TuringSimulator.Core.Level;
using TuringSimulator.Core.Tape;
using TuringSimulator.Core.Validation;

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
            var mainTest = LevelLoadedActionGuard.RequireMainTest(context.Level);
            var tape = new SimulationTape(mainTest.headIndex, mainTest.initialSymbols);
            context.Model.Buffer.Clear();
            context.Model.CurrentTape = tape;
        }
    }

    public sealed class LevelValidationTestsSetupActionHandler : ILevelLoadedActionHandler
    {
        public void Apply(LevelLoadedActionContext context)
        {
            context.Model.Validation.SetTests(context.Level.validationTests);
        }
    }

    public sealed class LevelViewResetActionHandler : ILevelLoadedActionHandler
    {
        public void Apply(LevelLoadedActionContext context)
        {
            var mainTest = LevelLoadedActionGuard.RequireMainTest(context.Level);
            context.View.Tape.SetTape(mainTest.initialSymbols, mainTest.headIndex);
            context.View.Halt.Reset();
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

    static class LevelLoadedActionGuard
    {
        public static ValidationTest RequireMainTest(LevelDefinition level)
        {
            if (level.mainTest == null)
                throw new InvalidOperationException("LevelDefinition.mainTest must be assigned.");
            return level.mainTest;
        }
    }
}
