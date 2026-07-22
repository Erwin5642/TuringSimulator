using System.Collections.Generic;
using System.Threading.Tasks;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.Controller.Syncronizer
{
    public interface IStepApplier
    {
        int CurrentStepIndex { get; }
        int TotalSteps { get; }
        Task<StepResult?> TryStepForward();
        Task<StepResult?> TryStepBackward();
        void LoadSteps(IReadOnlyList<StepResult> steps);

        void Reset();
    }
}