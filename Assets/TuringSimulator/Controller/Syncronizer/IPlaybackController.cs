using System;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.Controller.Syncronizer
{
    public interface IPlaybackController
    {
        event Action<StepResult> OnStep;
        
        void Play();
        void Pause();
        void StepForward();
        void StepBackward();
        void Enable();
        void Disable();
    }
}