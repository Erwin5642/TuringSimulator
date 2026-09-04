using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using TuringSimulator.Controller.Syncronizer;
using TuringSimulator.Core.Simulation.Step;
using TuringSimulator.Core.Types;

namespace EditModeTests
{
    public class PlaybackControllerTests
    {
        PlaybackController _playback;

        [TearDown]
        public void TearDown()
        {
            _playback?.Disable();
            _playback = null;
        }

        [Test]
        public void Play_WhenEnabledWithNoSteps_StaysPlayRequested()
        {
            _playback = new PlaybackController(new ImmediateStepApplier());
            _playback.Enable();

            _playback.Play();

            Assert.That(_playback.IsPlaying, Is.True);
            Assert.That(PlaybackController.PlayRequested, Is.True);
        }

        [Test]
        public void Pause_ClearsPlayRequested()
        {
            _playback = new PlaybackController(new ImmediateStepApplier());
            _playback.Enable();
            _playback.Play();

            _playback.Pause();

            Assert.That(_playback.IsPlaying, Is.False);
            Assert.That(PlaybackController.PlayRequested, Is.False);
        }

        [Test]
        public void Disable_PausesAndBlocksPlay()
        {
            _playback = new PlaybackController(new ImmediateStepApplier());
            _playback.Enable();
            _playback.Play();

            _playback.Disable();
            _playback.Play();

            Assert.That(_playback.IsPlaying, Is.False);
        }

        [Test]
        public async Task NotifyStepsAvailable_ResumesWaitingPlayLoop()
        {
            var applier = new ImmediateStepApplier();
            _playback = new PlaybackController(applier);
            var applied = new TaskCompletionSource<StepResult>();
            _playback.OnStep += step => applied.TrySetResult(step);
            _playback.Enable();
            _playback.Play();

            var step = new StepResult(HaltStatus.Accept);
            applier.AppendStep(step);
            _playback.NotifyStepsAvailable();

            var completed = await Task.WhenAny(applied.Task, Task.Delay(1000));
            Assert.That(completed, Is.SameAs(applied.Task), "Play loop did not consume the appended step.");
            Assert.That(applied.Task.Result.AsHalt(), Is.EqualTo(HaltStatus.Accept));
        }

        sealed class ImmediateStepApplier : IStepApplier
        {
            readonly List<StepResult> _steps = new();

            public int CurrentStepIndex { get; private set; }
            public int TotalSteps => _steps.Count;

            public Task<StepResult?> TryStepForward()
            {
                if (CurrentStepIndex < 0 || CurrentStepIndex >= _steps.Count)
                    return Task.FromResult<StepResult?>(null);

                var step = _steps[CurrentStepIndex];
                CurrentStepIndex++;
                return Task.FromResult<StepResult?>(step);
            }

            public Task<StepResult?> TryStepBackward()
            {
                return Task.FromResult<StepResult?>(null);
            }

            public void LoadSteps(IReadOnlyList<StepResult> steps)
            {
                _steps.Clear();
                if (steps != null)
                    _steps.AddRange(steps);
                CurrentStepIndex = 0;
            }

            public void AppendStep(StepResult step)
            {
                _steps.Add(step);
            }

            public bool TryGetLastAppliedStep(out StepResult step)
            {
                var i = CurrentStepIndex - 1;
                if (i < 0 || i >= _steps.Count)
                {
                    step = default;
                    return false;
                }

                step = _steps[i];
                return true;
            }

            public void Reset()
            {
                CurrentStepIndex = 0;
                _steps.Clear();
            }
        }
    }
}
