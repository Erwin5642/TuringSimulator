using System;
using System.Threading;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;
using UnityEngine;

namespace TuringSimulator.Controller.Syncronizer
{
    public class PlaybackController : IPlaybackController
    {
        private readonly IStepApplier _stepApplier;
        private bool _enabled;

        private bool _busy;
        private bool _playRequested;
        private CancellationTokenSource _playCts;
        private TaskCompletionSource<bool> _stepsSignal;

        public PlaybackController(IStepApplier stepApplier)
        {
            _stepApplier = stepApplier ?? throw new ArgumentNullException(nameof(stepApplier));
            SetPlayRequested(false);
        }

        public event Action<StepResult> OnStep;
        public event Action<bool> OnPlayingChanged;

        public static event Action<bool> PlayingChanged;
        public static bool PlayRequested { get; private set; }

        public bool IsPlaying => _playRequested;

        public void Play()
        {
            if (!_enabled) return;
            SetPlayRequested(true);
            TryStartPlayLoop();
        }

        public void NotifyStepsAvailable()
        {
            _stepsSignal?.TrySetResult(true);
        }

        private void TryStartPlayLoop()
        {
            if (!_enabled || !_playRequested || _busy)
                return;

            _busy = true;
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;
            Debug.Log("[PlaybackController] Play enter");
            _ = RunPlay(token);
        }

        private async Task RunPlay(CancellationToken token)
        {
            try
            {
                while (_playRequested && !token.IsCancellationRequested)
                {
                    var stepped = await _stepApplier.TryStepForward();
                    if (stepped != null)
                    {
                        OnStep?.Invoke(stepped.Value);
                        continue;
                    }

                    await WaitForStepsOrCancel(token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                Debug.Log("[PlaybackController] Play exit");
                _busy = false;
                _playCts = null;
                _stepsSignal = null;
            }
        }

        async Task WaitForStepsOrCancel(CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return;

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _stepsSignal = tcs;

            if (_stepApplier.CurrentStepIndex < _stepApplier.TotalSteps)
            {
                tcs.TrySetResult(true);
                return;
            }

            using (token.Register(() => tcs.TrySetCanceled()))
            {
                try
                {
                    await tcs.Task;
                }
                catch (TaskCanceledException)
                {
                }
            }
        }

        public void Pause()
        {
            Debug.Log("[PlaybackController] Pause");
            _playCts?.Cancel();
            _stepsSignal?.TrySetCanceled();
            SetPlayRequested(false);
        }

        public void StepForward()
        {
            if (!_enabled) return;
            if (_busy) return;
            _busy = true;
            Debug.Log("[PlaybackController] Forward enter");

            _ = RunStep(_stepApplier.TryStepForward);
        }

        public void StepBackward()
        {
            if (!_enabled) return;
            if (_busy) return;
            _busy = true;
            Debug.Log("[PlaybackController] Backward enter");

            _ = RunStep(_stepApplier.TryStepBackward);
        }

        private async Task RunStep(Func<Task<StepResult?>> stepFunc)
        {
            try
            {
                var stepped = await stepFunc();
                if (stepped != null)
                    OnStep?.Invoke(stepped.Value);
            }
            finally
            {
                Debug.Log("[PlaybackController] Step exit");
                _busy = false;
                if (_playRequested)
                    TryStartPlayLoop();
            }
        }

        public void Enable() => _enabled = true;

        public void Disable()
        {
            Pause();
            _enabled = false;
        }

        void SetPlayRequested(bool value)
        {
            if (_playRequested == value && PlayRequested == value)
                return;

            _playRequested = value;
            PlayRequested = value;
            OnPlayingChanged?.Invoke(value);
            PlayingChanged?.Invoke(value);
        }
    }
}
