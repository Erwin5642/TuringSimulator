using System;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.Controller.Syncronizer
{
    public interface IPlaybackController
    {
        event Action<StepResult> OnStep;
        event Action<bool> OnPlayingChanged;

        bool IsPlaying { get; }
        
        void Play();
        void Pause();
        void StepForward();
        void StepBackward();
        void NotifyStepsAvailable();
        void Enable();
        void Disable();
    }
}
