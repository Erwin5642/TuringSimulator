using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;
using TuringSimulator.View.Machine;
using TuringSimulator.View.Machine.Tape;

namespace EditModeTests
{
    public class StepViewApplierAppendTests
    {
        [Test]
        public void AppendStep_DoesNotResetCurrentIndex()
        {
            var applier = new StepViewApplier(new ImmediateMachineView());
            applier.LoadSteps(new[] { new StepResult(HaltStatus.Accept) });

            applier.AppendStep(new StepResult(HaltStatus.Reject));

            Assert.That(applier.CurrentStepIndex, Is.EqualTo(0));
            Assert.That(applier.TotalSteps, Is.EqualTo(2));
        }

        [Test]
        public async Task TryGetLastAppliedStep_AfterForward_ReturnsAppliedDiff()
        {
            var applier = new StepViewApplier(new ImmediateMachineView());
            var diff = new StepResult(new StepDiff(Symbol.Blank, Symbol.Gear, 0, 0, 0, 1, 0));
            applier.AppendStep(diff);

            Assert.That(applier.TryGetLastAppliedStep(out _), Is.False);

            await applier.TryStepForward();

            Assert.That(applier.TryGetLastAppliedStep(out var applied), Is.True);
            Assert.That(applied.AsDiff().PreviousState, Is.EqualTo(0));
            Assert.That(applied.AsDiff().NextState, Is.EqualTo(1));
        }

        [Test]
        public async Task OnStepApplying_FiresBeforeViewCompletes()
        {
            var view = new GateMachineView();
            var applier = new StepViewApplier(view);
            var diff = new StepResult(new StepDiff(Symbol.Blank, Symbol.Gear, 0, 0, 0, 1, 0));
            applier.AppendStep(diff);

            StepResult? applying = null;
            applier.OnStepApplying += step => applying = step;

            var forward = applier.TryStepForward();
            Assert.That(applying.HasValue, Is.True);
            Assert.That(applying.Value.AsDiff().NextState, Is.EqualTo(1));

            view.Release();
            await forward;
        }

        [Test]
        public async Task Reset_DuringForward_DropsInFlightStep()
        {
            var view = new GateMachineView();
            var applier = new StepViewApplier(view);
            applier.AppendStep(new StepResult(HaltStatus.Accept));

            var forward = applier.TryStepForward();
            applier.Reset();
            view.Release();
            var result = await forward;

            Assert.That(result, Is.Null);
            Assert.That(applier.TotalSteps, Is.EqualTo(0));
            Assert.That(applier.CurrentStepIndex, Is.EqualTo(0));
        }

        sealed class ImmediateMachineView : IMachineView
        {
            public ITapeVisual Tape => null;
            public void Initialize(ITapeVisual tape) { }
            public Task UpdateStepForward(StepResult step) => Task.CompletedTask;
            public Task UpdateStepBackward(StepResult step) => Task.CompletedTask;
            public void Reset() { }
        }

        sealed class GateMachineView : IMachineView
        {
            readonly TaskCompletionSource<bool> _gate = new();

            public ITapeVisual Tape => null;
            public void Initialize(ITapeVisual tape) { }
            public Task UpdateStepForward(StepResult step) => _gate.Task;
            public Task UpdateStepBackward(StepResult step) => Task.CompletedTask;
            public void Reset() { }
            public void Release() => _gate.TrySetResult(true);
        }
    }
}
