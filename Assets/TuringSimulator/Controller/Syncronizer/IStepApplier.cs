using System.Threading.Tasks;
using TuringSimulator.Core.Simulation;
using TuringSimulator.Core.Simulation.Step;

namespace TuringSimulator.Controller.Syncronizer
{
    public interface IStepApplier
    {
        int CurrentStepIndex { get; }
        Task<StepResult?> TryStepForward();
        Task<StepResult?> TryStepBackward();

        void Reset();
    }
}