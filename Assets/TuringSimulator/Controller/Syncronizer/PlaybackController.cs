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
        private CancellationTokenSource _playCts;

        public PlaybackController(IStepApplier stepApplier)
        {
            _stepApplier = stepApplier;
        }

        public event Action<StepResult> OnStep;

        public void Play()
        {
            if (!_enabled) return;
            if (_busy) return;
            _busy = true;
            Debug.Log("[PlaybackController] Play enter");

            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            _ = RunPlay(token);
        }

        private async Task RunPlay(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var stepped = await _stepApplier.TryStepForward();
                    if (stepped == null)
                        break;

                    OnStep?.Invoke(stepped.Value);
                }
            }
            finally
            {
                Debug.Log("[PlaybackController] Play exit");
                _busy = false;
                _playCts = null;
            }
        }

        public void Pause()
        {
            Debug.Log("[PlaybackController] Pause");
            _playCts?.Cancel();
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
            }
        }

        public void Enable() => _enabled = true;
        public void Disable() => _enabled = false;
    }
}